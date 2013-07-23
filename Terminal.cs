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
        public static Thread thread;
        /// <summary>
        /// Whether the console is running or not
        /// </summary>
        public static bool Running = true;
        /// <summary>
        /// Number of current connections to this console
        /// </summary>
        public static int Connections = 0;

        /// <summary>
        /// This will start the console
        /// </summary>
        public static void Init()
        {
            thread = new System.Threading.Thread(thrd);
            thread.Start();
        }

        private static void HandleClient(object data)
        {
            try
            {
                System.Net.Sockets.TcpClient connection = (System.Net.Sockets.TcpClient)data;
                core.DebugLog("Incoming connection from: " + connection.Client.RemoteEndPoint.ToString());
                Connections++;
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

                int permissions = (IRCTrust.Auth(username, password));

                if (permissions == 0)
                {
                    Writer.WriteLine("Invalid user or password, bye");
                    Writer.Flush();
                    connection.Close();
                    Connections--;
                    return;
                }

                Writer.WriteLine("Successfuly logged in to wm-bot, I have " + Connections.ToString() + " users logged in");
                Writer.Flush();

                while (connection.Connected && !Reader.EndOfStream)
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
                            Connections--;
                            return;
                        case "info":
                            string result = "Uptime: " + core.getUptime() + Environment.NewLine + "Instances:" + Environment.NewLine;
                            lock (core.Instances)
                            {
                                foreach (Instance instance in core.Instances.Values)
                                {
                                    result += instance.Nick + " channels: " + instance.ChannelCount.ToString() +
                                        " connected: " + instance.IsConnected.ToString() + " working: " +
                                        instance.IsWorking.ToString() + "\n";
                                }
                            }
                            Writer.WriteLine(result);
                            Writer.Flush();
                            break;
                        case "help":
                            Writer.WriteLine("Commands:\n"
                            +"exit - shutdown connection\n"
                            +"info - print information about system\n"
                            +"halt - shutdown bot\n"
                            +"traffic-on - turn on traffic logs\n"
                            +"traffic-off - turn off traffic logs\n"
                            +"kill [instance] - disconnect selected instance\n"
                            +"conn [instance] - connect instance\n");
                            Writer.Flush();
                            break;
                        case "halt":
                            Writer.WriteLine("Shutting down");
                            Writer.Flush();
                            core.Kill();
                            return;
                        case "traffic-on":
                            config.Logging = true;
                            Writer.WriteLine("Dumping traffic");
                            Writer.Flush();
                            break;
                        case "traffic-off":
                            config.Logging = false;
                            Writer.WriteLine("Disabled traf");
                            Writer.Flush();
                            break;
                        case "kill":
                            if (core.Instances.ContainsKey(parameters))
                            {
                                core.Instances[parameters].irc.Disconnect();
                                Writer.WriteLine("Offline: " + parameters);
                                Writer.Flush();
                                break;
                            }
                            Writer.WriteLine("Unknown instance: " + parameters);
                            Writer.Flush();
                            break;
                        case "conn":
                            if (core.Instances.ContainsKey(parameters))
                            {
                                if (core.Instances[parameters].irc.IsConnected)
                                {
                                    Writer.WriteLine("Refusing to connect instance which is already connected: " + parameters);
                                    Writer.Flush();
                                    break;
                                }
                                core.Instances[parameters].Init();
                                Writer.WriteLine("Initializing: " + parameters);
                                Writer.Flush();
                                int curr = 0;
                                while (curr < 10 && !core.Instances[parameters].IsWorking)
                                {
                                    curr++;
                                    Thread.Sleep(1000);
                                }
                                if (!core.Instances[parameters].IsWorking)
                                {
                                    Writer.WriteLine("Failed to initialize instance");
                                    Writer.Flush();
                                    break;
                                }
                                Writer.WriteLine("Joining channels");
                                Writer.Flush();
                                core.Instances[parameters].Join();
                                curr = 0;
                                while (curr < core.Instances[parameters].ChannelCount && !core.Instances[parameters].irc.ChannelsJoined)
                                {
                                    curr++;
                                    Thread.Sleep(6000);
                                }
                                if (!core.Instances[parameters].irc.ChannelsJoined)
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
                        default:
                            Writer.WriteLine("Unknown command, try help");
                            Writer.Flush();
                            break;
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            Connections--;
        }

        private static void thrd()
        {
            try
            {
                System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, config.SystemPort);
                server.Start();
                Program.Log("Network console is online on port: " + config.SystemPort.ToString());
                while (Running)
                {
                    System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                    Thread client = new Thread(HandleClient);
                    client.Start(connection);
                    Thread.Sleep(100);
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }
    }
}