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
using System.Text;
using System.Threading;

namespace wmib
{
    public partial class IRC
    {
        public static void DeliverMessage(string text, Channel target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal)
        {
            if (!target.Suppress)
            {            
                target.PrimaryInstance.Network.Message(text, target.Name, priority);
            }
        }

        public static void DeliverMessage(string text, libirc.UserInfo target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal)
        {
            // this is a private message
            lock (Instance.TargetBuffer)
            {
                if (Instance.TargetBuffer.ContainsKey(target.Nick))
                {
                    Instance.TargetBuffer[target.Nick].Network.Message(text, target.Nick, priority);
                    return;
                }
            }
            Instance.PrimaryInstance.Network.Message(text, target.Nick, priority);
        }

        public static void DeliverMessage(string text, string target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal)
        {
            // get a target instance
            if (target.StartsWith("#"))
            {
                // it's a channel
                Channel ch = Core.GetChannel(target);
                if (ch == null)
                {
                    Syslog.Log("Not sending message to unknown channel: " + target);
                    return;
                }
                if (!ch.PrimaryInstance.IsConnected)
                {
                    Syslog.Log("Not sending message using disconnected instance: " + ch.PrimaryInstance.Nick + " target: " + target + " message: " + text);
                    return;
                }
                if (!ch.Suppress)
                {
                    ch.PrimaryInstance.Network.Message(text, target, priority);
                }
            } else
            {
                lock (Instance.TargetBuffer)
                {
                    if (Instance.TargetBuffer.ContainsKey(target))
                    {
                        Instance.TargetBuffer[target].Network.Message(text, target, priority);
                        return;
                    }
                }
                Instance.PrimaryInstance.Network.Message(text, target, priority);
            }
        }

        /*
        /// <summary>
        /// This function will retrieve a list of users in a channel for every channel that doesn't have it so far
        /// </summary>
        public void ChannelList()
        {
            try
            {
                while (IsConnected && Core.IsRunning)
                {
                    List<string> channels = new List<string>();
                    // check if there is some channel which needs an update of user list
                    foreach (Channel dd in ParentInstance.ChannelList)
                    {
                        if (!dd.HasFreshUserList)
                        {
                            channels.Add(dd.Name);
                        }
                    }

                    if (channels.Count >= 1)
                    {
                        foreach (string xx in channels)
                        {
                            Syslog.Log("requesting user list on " + ParentInstance.Nick + " for channel: " + xx);
                            // Send the request with low priority
                            Queue.Send("WHO " + xx, libirc.Defs.Priority.Low);
                        }
                        // we give 10 seconds for each channel to send us a list
                        Thread.Sleep(10000 * channels.Count);
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (ThreadAbortException)
            {
                Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
                return;
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
        }

        /// <summary>
        /// Connect this instance
        /// </summary>
        public void Connect()
        {
            Syslog.Log("Connecting instance " + NickName + " to irc server " + Server + "...");
            if (!Configuration.IRC.UsingBouncer)
            {
                networkStream = new TcpClient(Server, 6667).GetStream();
            } else
            {
                Syslog.Log(ParentInstance.Nick + " is using personal bouncer port " + BouncerPort);
                networkStream = new TcpClient(Bouncer, BouncerPort).GetStream();
            }
            connected = true;
            streamReader = new StreamReader(networkStream, Encoding.UTF8);
            streamWriter = new StreamWriter(networkStream);

            bool Auth = true;

            if (Configuration.IRC.UsingBouncer)
            {
                SendData("CONTROL: STATUS");
                Syslog.Log("CACHE: Waiting for buffer (network bouncer) of instance " + this.ParentInstance.Nick);
                bool done = true;
                while (done)
                {
                    string response = streamReader.ReadLine();
                    if (response == "CONTROL: TRUE")
                    {
                        Syslog.DebugLog("Resumming previous session on " + this.ParentInstance.Nick);
                        done = false;
                        Auth = false;
                        ChannelsJoined = true;
                        IsWorking = true;
                    } else if (response.StartsWith(":"))
                    {
                        Backlog.Add(response);
                    } else if (response == "CONTROL: FALSE")
                    {
                        Syslog.DebugLog("Bouncer is not connected, starting new session on " + this.ParentInstance.Nick);
                        done = false;
                        ChannelsJoined = false;
                        SendData("CONTROL: CREATE " + Server);
                        streamWriter.Flush();
                    }
                }
            }

            if (Auth)
            {
                NetworkInit();
            }

            _Queue = new Thread(Queue.Run) {Name = "MessageQueue:" + NickName};
            Core.ThreadManager.RegisterThread(_Queue);
            PingerThread = new Thread(Ping);
            Core.ThreadManager.RegisterThread(PingerThread);
            PingerThread.Name = "Ping:" + NickName;
            PingerThread.Start();
            _Queue.Start();
        }

        private void NetworkInit()
        {
            SendData("USER " + UserName + " 8 * :" + Ident);
            SendData("NICK " + ParentInstance.Nick);

            Authenticate();
        }

        /// <summary>
        /// Connection
        /// </summary>
        /// <returns></returns>
        public void ParserExec()
        {
            string nick = "";
            string host = "";
            const char delimiter = (char)001;

            while ((!streamReader.EndOfStream || Backlog.Count > 0) && Core.IsRunning)
            {
                string text;
                lock (Backlog)
                {
                    if (Backlog.Count == 0)
                    {
                        text = streamReader.ReadLine();
                    } else
                    {
                        text = Backlog[0];
                        Backlog.RemoveAt(0);
                    }
                }
                Core.TrafficLog(ParentInstance.Nick + "<<<<<<" + text);
                if (Configuration.IRC.UsingBouncer)
                {
                    if (text.StartsWith("CONTROL: "))
                    {
                        if (text == "CONTROL: DC")
                        {
                            SendData("CONTROL: CREATE " + Server);
                            streamWriter.Flush();
                            Syslog.Log("CACHE: Lost connection to remote on " + this.ParentInstance.Nick + 
                                ", creating new session on remote"
                            );
                            ChannelsJoined = false;
                            IsWorking = false;
                            int xx = 0;
                            bool Connected_ = false;
                            while (!Connected_)
                            {
                                Thread.Sleep(2000);
                                SendData("CONTROL: STATUS");
                                string response = streamReader.ReadLine();
                                Core.TrafficLog(ParentInstance.Nick + "<<<<<<" + response);
                                if (response.StartsWith(":"))
                                {
                                    // we received network data here
                                    lock(Backlog)
                                    {
                                        Backlog.Add(response);
                                    }
                                    continue;
                                }
                                if (response == "CONTROL: TRUE")
                                {
                                    Syslog.Log("Bouncer reconnected to network on: " + NickName);
                                    NetworkInit();
                                    ParentInstance.Join();
                                    Connected_ = true;
                                } else
                                {
                                    xx++;
                                    if (xx > 6)
                                    {
                                        Syslog.WarningLog("Bouncer failed to connect to the network within 10 seconds, disconnecting it: " + NickName);
                                        SendData("CONTROL: DISCONNECT");
                                        return;
                                    }
                                    Syslog.Log("Still waiting for bouncer (trying " + xx 
                                               + "/6) on " + NickName + " " + response);
                                }
                            }
                        }
                    }
                }
                if (text.StartsWith(":"))
                {
					DateTime pong;
                    libirc.ProcessorIRC processor = new libirc.ProcessorIRC(WmIrcProtocol.Network, text, ref pong);
                    processor.ProfiledResult();
                    string check = text.Substring(text.IndexOf(" "));
                    if (!check.StartsWith(" 005"))
                    {
                        string command = "";
                        if (text.Contains(" :"))
                        {
                            command = text.Substring(1);
                            command = command.Substring(0, command.IndexOf(" :"));
                        }
                        
                        if (command.Contains("KICK"))
                        {
                            string temp = command.Substring(command.IndexOf("KICK"));
                            string[] parts = temp.Split(' ');
                            if (parts.Length > 1)
                            {
                                string _channel = parts[1];
                                if (_channel == Configuration.System.DebugChan && ParentInstance.Nick != Core.irc.NickName)
                                {
                                    continue;
                                }
                                string user = parts[2];
                                if (user == NickName)
                                {
                                    Channel chan = Core.GetChannel(_channel);
                                    if (chan != null)
                                    {
                                        lock(Configuration.Channels)
                                        {
                                            if (Configuration.Channels.Contains(chan))
                                            {
                                                Configuration.Channels.Remove(chan);
                                                Syslog.Log("I was kicked from " + parts[1]);
                                                Configuration.Save();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(50);
            }
            Syslog.Log("Lost connection to IRC on " + NickName);
            Disconnect();
            IsWorking = false;
        }
        */
    }
}
