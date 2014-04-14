//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;
using System.Text;

namespace wmib
{
    /// <summary>
    /// This class open a network console which sysadmins can login to and control the bot
    /// </summary>
    public class Terminal
    {
        /// <summary>
        /// Thread this console run in
        /// </summary>
        private static Thread thread;
        /// <summary>
        /// Gets a value indicating whether this instance is online.
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
        /// Whether the console is running or not
        /// </summary>
        public static bool Running = true;
        /// <summary>
        /// Number of current connections to this console
        /// </summary>
        public static int Connections = 0;
        private static object lConnections = new object();

        /// <summary>
        /// Decreases the connections.
        /// </summary>
        private static void DecreaseConnections()
        {
            lock(lConnections)
            {
                Connections--;
            }
        }

        /// <summary>
        /// Increases the connections.
        /// </summary>
        private static void IncreaseConnections()
        {
            lock(lConnections)
            {
                Connections++;
            }
        }

        /// <summary>
        /// This will start the console
        /// </summary>
        public static void Init()
        {
            thread = new System.Threading.Thread(ExecuteThread);
            thread.Name = "Telnet";
            Core.ThreadManager.RegisterThread(thread);
            thread.Start();
        }

        private static void HandleClient(object data)
        {
            try
            {
                System.Net.Sockets.TcpClient connection = (System.Net.Sockets.TcpClient)data;
                Syslog.DebugLog("Incoming connection from: " + connection.Client.RemoteEndPoint.ToString());
                IncreaseConnections();
                connection.NoDelay = true;
                System.Net.Sockets.NetworkStream ns = connection.GetStream();
                System.IO.StreamReader Reader = new System.IO.StreamReader(ns);
                string text;
                // give the user access to global cache
                // save the reference to global cache because we might need it in future
                System.IO.StreamWriter Writer = new System.IO.StreamWriter(ns);

                // login

                Writer.WriteLine("Enter username:");
                Writer.Flush();
                string username = Reader.ReadLine();
                Writer.WriteLine("Enter password:");
                Writer.Flush();
                string password = Reader.ReadLine();
                SystemUser user = Security.Auth(username, password);
				if (user == null)
				{
                    Writer.WriteLine("Invalid user or password, bye");
                    Writer.Flush();
                    connection.Close();
                    DecreaseConnections();
                    return;
                }
				if (!Security.IsGloballyApproved(user, "terminal"))
				{
					Writer.WriteLine("No permissions, bye");
                    Writer.Flush();
                    connection.Close();
                    DecreaseConnections();
                    return;
				}
                Writer.WriteLine("Successfuly logged in to wm-bot, I have " + Connections.ToString() + " users logged in");
                Writer.Flush();
                while (connection.Connected && !Reader.EndOfStream && Core.IsRunning)
                {
                    text = Reader.ReadLine();
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
                            Writer.WriteLine("Good bye");
                            Writer.Flush();
                            connection.Close();
                            DecreaseConnections();
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
                            Writer.WriteLine(result);
                            Writer.Flush();
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
                            Writer.WriteLine(result);
                            Writer.Flush();
                            break;
                        case "help":
                            Writer.WriteLine("Commands:\n"
                            + "exit - shutdown connection\n"
                            + "verbosity++\nverbosity--\n"
                            + "info - print information about system\n"
                            + "halt - shutdown bot\n"
                            + "traffic-on - turn on traffic logs\n"
                            + "traffic-off - turn off traffic logs\n"
                            + "kill [instance] - disconnect selected instance\n"
                            + "conn [instance] - connect instance\n");
                            Writer.Flush();
                            break;
                        case "halt":
                            Writer.WriteLine("Shutting down");
                            Writer.Flush();
                            Core.Kill();
                            return;
                        case "traffic-on":
                            Configuration.Network.Logging = true;
                            Writer.WriteLine("Dumping traffic into datafile");
                            Writer.Flush();
                            break;
                        case "verbosity++":
                            Configuration.System.SelectedVerbosity++;
                            break;
                        case "verbosity--":
                            Configuration.System.SelectedVerbosity--;
                            break;
                        case "traffic-off":
                            Configuration.Network.Logging = false;
                            Writer.WriteLine("Disabled traffic");
                            Writer.Flush();
                            break;
                        case "kill":
                            if (Core.Instances.ContainsKey(parameters))
                            {
                                Core.Instances[parameters].IsActive = false;
                                Core.Instances[parameters].ShutDown();
                                Writer.WriteLine("Killed: " + parameters);
                                Writer.Flush();
                                break;
                            }
                            Writer.WriteLine("Unknown instance: " + parameters);
                            Writer.Flush();
                            break;
                        case "conn":
                            if (Core.Instances.ContainsKey(parameters))
                            {
                                if (Core.Instances[parameters].irc.IsConnected)
                                {
                                    Writer.WriteLine("Refusing to connect instance which is already connected: " + parameters);
                                    Writer.Flush();
                                    break;
                                }
                                Core.Instances[parameters].Init();
                                Writer.WriteLine("Initializing: " + parameters);
                                Writer.Flush();
                                int curr = 0;
                                while (curr < 10 && !Core.Instances[parameters].IsWorking)
                                {
                                    curr++;
                                    Thread.Sleep(1000);
                                }
                                if (!Core.Instances[parameters].IsWorking)
                                {
                                    Writer.WriteLine("Failed to initialize instance");
                                    Writer.Flush();
                                    break;
                                }
                                Writer.WriteLine("Joining channels");
                                Writer.Flush();
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
                                    Writer.WriteLine("Failed to rejoin all channels in time");
                                    Writer.Flush();
                                    break;
                                }
                                Writer.WriteLine("Instance is online: " + parameters);
                                Writer.Flush();
                                break;
                            }
                            Writer.WriteLine("Unknown instance: " + parameters);
                            Writer.Flush();
                            break;
                        case "send":
                            if (!parameters.Contains(" "))
                            {
                                Writer.WriteLine("This command requires 2 parameters");
                                Writer.Flush();
                                break;
                            }
                            string to = parameters.Substring(0, parameters.IndexOf(" "));
                            if (Core.Instances.ContainsKey(to))
                            {
                                if (!Core.Instances[to].irc.IsConnected)
                                {
                                    Writer.WriteLine("Refusing to send data using instance which is not connected: " + to);
                                    Writer.Flush();
                                    break;
                                }
                                Core.Instances[to].irc.SendData(parameters.Substring(parameters.IndexOf(" ") + 1));
                                break;
                            }
                            Writer.WriteLine("I have no such instance dude");
                            Writer.Flush();
                            break;
                        default:
                            Writer.WriteLine("Unknown command, try help");
                            Writer.Flush();
                            break;
                    }
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            DecreaseConnections();
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
                while (Running && Core.IsRunning)
                {
                    System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                    Thread client = new Thread(HandleClient);
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