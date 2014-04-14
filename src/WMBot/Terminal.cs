//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System;
using System.Collections.Generic;
using System.Threading;

namespace wmib
{
    /// <summary>
    /// This class open a network console which sysadmins can login to and control the bot
    /// </summary>
    public class Terminal
    {
        public class Session
        {
            private System.Net.Sockets.TcpClient connection;
            private System.Net.Sockets.NetworkStream networkStream;
            private System.IO.StreamReader streamReader;
            private System.IO.StreamWriter streamWriter;
            
            private void Write(string text)
            {
                streamWriter.WriteLine();
                streamWriter.Flush();
            }
            
            private void Disconnect()
            {
                lock (Terminal.SessionList)
                {
                    if (Terminal.SessionList.Contains(this))
                        Terminal.SessionList.Remove(this);
                }
                Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
            }
            
            private void Disconnect(string text)
            {
                Write (text);
                connection.Close();
                lock (Terminal.SessionList)
                {
                    if (Terminal.SessionList.Contains(this))
                        Terminal.SessionList.Remove(this);
                }
                Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
            }
            
            public void ThreadExec(object data)
            {
                try
                {
                    this.connection = (System.Net.Sockets.TcpClient)data;
                    Syslog.DebugLog("Incoming connection from: " + connection.Client.RemoteEndPoint.ToString());
                    this.connection.NoDelay = true;
                    this.networkStream = connection.GetStream();
                    this.streamReader = new System.IO.StreamReader(networkStream);
                    this.streamWriter = new System.IO.StreamWriter(networkStream);
                    // login
                    Write("Enter username:");
                    string username = streamReader.ReadLine();
                    Write("Enter password:");
                    string password = streamReader.ReadLine();
                    SystemUser user = Security.Auth(username, password);
                    if (user == null)
                    {
                        Disconnect("Invalid user or password, bye");
                        return;
                    }
                    if (!Security.IsGloballyApproved(user, "terminal"))
                    {
                        Disconnect("No permissions, bye");
                        return;
                    }
                    Write("Successfuly logged in to wm-bot, I have " + Terminal.SessionList.Count.ToString() + " users logged in");
                    while (connection.Connected && !streamReader.EndOfStream && Core.IsRunning)
                    {
                        string text = streamReader.ReadLine();
                        string command = text;
                        List<string> list = new List<string>();
                        string parameters = "";
                        if (command.Contains(" "))
                        {
                            parameters = command.Substring(command.IndexOf(" ") + 1);
                            command = command.Substring(0, command.IndexOf(" "));
                            if (parameters.Contains(" "))
                            {
                                list.AddRange(parameters.Split(' '));
                            }
                        }
    
                        switch (command.ToLower())
                        {
                            case "exit":
                            case "quit":
                                Disconnect("Good bye");
                                return;
                            case "info":
                                string result = "Uptime: " + Core.getUptime() + " Version: " + Configuration.System.Version 
                                    + "\n\nBuffer information:\nUnwritten lines (file storage): " + StorageWriter.Count.ToString() + "\n";
                                if (Core.DB != null)
                                {
                                    result += "Unwritten rows (MySQL): " + Core.DB.CacheSize().ToString() + "\n";
                                }
                                result += "\nThreads:\n";
                                foreach (Thread thread in Core.ThreadManager.ThreadList)
                                {
                                    result += "Thread: " + FormatToSpecSize(thread.Name, 20) + " status: " + 
                                              FormatToSpecSize(thread.ThreadState.ToString(), 20) +
                                              " id: " + FormatToSpecSize(thread.ManagedThreadId.ToString(), 8) + "\n";
                                }
                                result += "\nInstances:";
                                Write(result);
                                result = "";
                                Syslog.DebugLog("Retrieving information for user " + username + " in system");
                                lock (Core.Instances)
                                {
                                    foreach (Instance instance in Core.Instances.Values)
                                    {
                                        Syslog.DebugLog("Retrieving information for user " + username + " of instance " +  instance.Nick, 2);
                                        result += instance.Nick + " channels: " + instance.ChannelCount.ToString() +
                                            " connected: " + instance.IsConnected.ToString() + " working: " +
                                            instance.IsWorking.ToString() + " queue: " + instance.QueueSize().ToString() + "\n";
                                    }
                                }
                                Write(result);
                                break;
                            case "help":
                                Write("Commands:\n"
                                + "exit - shutdown connection\n"
                                + "verbosity++\nverbosity--\n"
                                + "info - print information about system\n"
                                + "halt - shutdown bot\n"
                                + "traffic-on - turn on traffic logs\n"
                                + "traffic-off - turn off traffic logs\n"
                                + "kill [instance] - disconnect selected instance\n"
                                + "conn [instance] - connect instance\n");
                                break;
                            case "halt":
                                Disconnect("Shutting down");
                                Core.Kill();
                                return;
                            case "traffic-on":
                                Configuration.Network.Logging = true;
                                Write("Dumping traffic into datafile");
                                break;
                            case "verbosity++":
                                Configuration.System.SelectedVerbosity++;
                                break;
                            case "verbosity--":
                                Configuration.System.SelectedVerbosity--;
                                break;
                            case "traffic-off":
                                Configuration.Network.Logging = false;
                                Write("Disabled traffic");
                                break;
                            case "kill":
                                if (Core.Instances.ContainsKey(parameters))
                                {
                                    Core.Instances[parameters].IsActive = false;
                                    Core.Instances[parameters].ShutDown();
                                    Write("Killed: " + parameters);
                                    break;
                                }
                                Write("Unknown instance: " + parameters);
                                break;
                            case "conn":
                                if (Core.Instances.ContainsKey(parameters))
                                {
                                    if (Core.Instances[parameters].irc.IsConnected)
                                    {
                                        Write("Refusing to connect instance which is already connected: " + parameters);
                                        break;
                                    }
                                    Core.Instances[parameters].Init();
                                    Write("Initializing: " + parameters);
                                    int curr = 0;
                                    while (curr < 10 && !Core.Instances[parameters].IsWorking)
                                    {
                                        curr++;
                                        Thread.Sleep(1000);
                                    }
                                    if (!Core.Instances[parameters].IsWorking)
                                    {
                                        Write("Failed to initialize instance");
                                        break;
                                    }
                                    Write("Joining channels");
                                    Core.Instances[parameters].irc.ChannelsJoined = false;
                                    Core.Instances[parameters].Join();
                                    curr = 0;
                                    while (curr < Core.Instances[parameters].ChannelCount && !Core.Instances[parameters].irc.ChannelsJoined)
                                    {
                                        curr++;
                                        Thread.Sleep(6000);
                                    }
                                    if (!Core.Instances[parameters].irc.ChannelsJoined)
                                    {
                                        Write("Failed to rejoin all channels in time");
                                        break;
                                    }
                                    Write("Instance is online: " + parameters);
                                    break;
                                }
                                Write("Unknown instance: " + parameters);
                                break;
                            case "send":
                                if (!parameters.Contains(" "))
                                {
                                    Write("This command requires 2 parameters");
                                    break;
                                }
                                string to = parameters.Substring(0, parameters.IndexOf(" "));
                                if (Core.Instances.ContainsKey(to))
                                {
                                    if (!Core.Instances[to].irc.IsConnected)
                                    {
                                        Write("Refusing to send data using instance which is not connected: " + to);
                                        break;
                                    }
                                    Core.Instances[to].irc.SendData(parameters.Substring(parameters.IndexOf(" ") + 1));
                                    break;
                                }
                                Write("I have no such instance dude");
                                break;
                            default:
                                Write("Unknown command, try help");
                                break;
                        }
                    }
                }
                catch (Exception fail)
                {
                    Core.HandleException(fail);
                }
                Disconnect();
            }
        }

        public static List<Session> SessionList = new List<Session>();
        /// <summary>
        /// Thread this console run in
        /// </summary>
        private static Thread listenerThread;
        /// <summary>
        /// Gets a value indicating whether this listener for terminal is online.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is online; otherwise, <c>false</c>.
        /// </value>
        public static bool IsOnline
        {
            get
            {
                return Online;
            }
        }
        /// <summary>
        /// Whether the console is online or not
        /// </summary>
        private static bool Online = false;

        /// <summary>
        /// This will start the console
        /// </summary>
        public static void Init()
        {
            listenerThread = new System.Threading.Thread(ExecuteThread);
            listenerThread.Name = "Telnet";
            Core.ThreadManager.RegisterThread(listenerThread);
            listenerThread.Start();
        }
        
        public static string FormatToSpecSize(string st, int size)
        {
            if (st.Length > size)
            {
                st = st.Substring(0, st.Length - ((st.Length - size) + 3));
                st += "...";
            } else
            {
                while (st.Length < size)
                {
                    st += " ";
                }
            }
            return st;
        }

        private static void ExecuteThread()
        {
            try
            {
                System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any,
                                                                         Configuration.Network.SystemPort);
                server.Start();
                Online = true;
                Syslog.WriteNow("Network console is online on port: " + Configuration.Network.SystemPort.ToString());
                while (Core.IsRunning)
                {
                    System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                    Session session = new Session();
                    lock (SessionList)
                    {
                        SessionList.Add(session);
                    }
                    Thread client = new Thread(session.ThreadExec);
                    client.Name = "Telnet:" + connection.Client.RemoteEndPoint.ToString();
                    Core.ThreadManager.RegisterThread(client);
                    client.Start(connection);
                    Thread.Sleep(100);
                }
            }
            catch (Exception fail)
            {
                Online = false;
                Syslog.WriteNow("Network console is down", true);
                Core.HandleException(fail);
            }
            Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
        }
    }
}