//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;


namespace wmib
{
    /// <summary>
    /// IRC
    /// </summary>
    public class IRC
    {
        /// <summary>
        /// Server addr
        /// </summary>
        public string Server;
        /// <summary>
        /// Nick
        /// </summary>
        public string NickName;
        /// <summary>
        /// ID
        /// </summary>
        public string Ident;
        /// <summary>
        /// User
        /// </summary>
        public string UserName;
        /// <summary>
        /// Pw
        /// </summary>
        public string Password;
        /// <summary>
        /// Socket
        /// </summary>
        private static System.Net.Sockets.NetworkStream networkStream;
        /// <summary>
        /// Socket Reader
        /// </summary>
        private StreamReader streamReader;
        /// <summary>
        /// Writer
        /// </summary>
        private StreamWriter streamWriter;
        /// <summary>
        /// Pinger
        /// </summary>
        public static Thread check_thread = null;
        /// <summary>
        /// Queue thread
        /// </summary>
        private Thread _Queue;
        /// <summary>
        /// This is a thread for channel list
        /// </summary>
        private Thread ChannelThread;
        /// <summary>
        /// Queue of all messages that should be delivered to network
        /// </summary>
        public SlowQueue _SlowQueue = null;
        private bool connected;
        /// <summary>
        /// If network is connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return connected;
            }
        }

        /// <summary>
        /// Queue of all messages that should be delivered to some network
        /// </summary>
        [Serializable()]
        public class SlowQueue
        {
            /// <summary>
            /// Creates new queue
            /// </summary>
            /// <param name="_parent">Parent object</param>
            public SlowQueue(IRC _parent)
            {
                Parent = _parent;
            }

            /// <summary>
            /// Message
            /// </summary>
            public class Message
            {
                /// <summary>
                /// Priority
                /// </summary>
                public priority _Priority;
                /// <summary>
                /// Message itself
                /// </summary>
                public string message;
                /// <summary>
                /// Channel which the message should be delivered to
                /// </summary>
                public string channel;
                /// <summary>
                /// If this is true the message will be sent as raw command
                /// </summary>
                public bool command = false;
            }

            private bool running = true;
            /// <summary>
            /// List of messages in queue which needs to be processed
            /// </summary>
            public List<Message> messages = new List<Message>();
            /// <summary>
            /// List of new messages
            /// </summary>
            public List<Message> newmessages = new List<Message>();
            private IRC Parent;

            /// <summary>
            /// Deliver a message
            /// </summary>
            /// <param name="Message">Text</param>
            /// <param name="Channel">Channel</param>
            /// <param name="Pr">Priority</param>
            public void DeliverMessage(string Message, string Channel, priority Pr = priority.normal)
            {
                Message text = new Message {_Priority = Pr, message = Message, channel = Channel};
                lock (messages)
                {
                    messages.Add(text);
                }
            }

            /// <summary>
            /// Deliver me
            /// </summary>
            /// <param name="Message">Text</param>
            /// <param name="Channel">Channel</param>
            /// <param name="Pr">Priority</param>
            public void DeliverAct(string Message, string Channel, priority Pr = priority.normal)
            {
                Message text = new Message {_Priority = Pr, message = Message, channel = Channel};
                lock (messages)
                {
                    messages.Add(text);
                }
            }

            /// <summary>
            /// Send a command to server
            /// </summary>
            /// <param name="Data"></param>
            /// <param name="Priority"></param>
            public void Send(string Data, priority Priority = priority.high)
            {
                Message text = new Message { _Priority = Priority, channel = null, message = Data, command = true };
                lock (messages)
                {
                    messages.Add(text);
                }
            }

            /// <summary>
            /// Deliver a message
            /// </summary>
            /// <param name="Message">Text</param>
            /// <param name="User">User</param>
            /// <param name="Pr">Priority</param>
            public void DeliverMessage(string Message, User User, priority Pr = priority.low)
            {
                Message text = new Message {_Priority = Pr, message = Message, channel = User.Nick};
                lock (messages)
                {
                    messages.Add(text);
                }
            }

            /// <summary>
            /// Deliver a message
            /// </summary>
            /// <param name="Message">Text</param>
            /// <param name="Channel">Channel</param>
            /// <param name="Pr">Priority</param>
            public void DeliverMessage(string Message, config.channel Channel, priority Pr = priority.normal)
            {
                Message text = new Message {_Priority = Pr, message = Message, channel = Channel.Name};
                lock (messages)
                {
                    messages.Add(text);
                }
            }

            /// <summary>
            /// Disable queue
            /// </summary>
            public void Exit()
            {
                running = false;
                core.Log("Turning off the message queue with " + (newmessages.Count + messages.Count).ToString() + " untransfered data");
                lock (messages)
                {
                    messages.Clear();
                }
                lock (newmessages)
                {
                    newmessages.Clear();
                }
            }

            private void Transfer(Message text)
            {
                if (text.command)
                {
                    Parent.SendData(text.message);
                    return;
                }
                Parent.Message(text.message, text.channel);
            }

            /// <summary>
            /// Internal function
            /// </summary>
            public void Run()
            {
                while (true)
                {
                    try
                    {
                        if (!running)
                        {
                            return;
                        }
                        if (messages.Count > 0)
                        {
                            lock (messages)
                            {
                                newmessages.AddRange(messages);
                                messages.Clear();
                            }
                        }
                        if (newmessages.Count > 0)
                        {
                            List<Message> Processed = new List<Message>();
                            priority highest = priority.low;
                            lock (newmessages)
                            {
                                while (newmessages.Count > 0)
                                {
                                    // we need to get all messages that have been scheduled to be send
                                    lock (messages)
                                    {
                                        if (messages.Count > 0)
                                        {
                                            newmessages.AddRange(messages);
                                            messages.Clear();
                                        }
                                    }
                                    highest = priority.low;
                                    // we need to check the priority we need to handle first
                                    foreach (Message message in newmessages)
                                    {
                                        if (message._Priority > highest)
                                        {
                                            highest = message._Priority;
                                            if (message._Priority == priority.high)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    // send highest priority first
                                    foreach (Message message in newmessages)
                                    {
                                        if (message._Priority >= highest)
                                        {
                                            Processed.Add(message);
                                            Transfer(message);
                                            System.Threading.Thread.Sleep(1000);
                                            if (highest != priority.high)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    foreach (Message message in Processed)
                                    {
                                        if (newmessages.Contains(message))
                                        {
                                            newmessages.Remove(message);
                                        }
                                    }
                                }
                            }
                        }
                        lock (newmessages)
                        {
                            newmessages.Clear();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        /// <summary>
        /// Creates a new instance of IRC
        /// </summary>
        /// <param name="_server">Server to connect to</param>
        /// <param name="_nick">Nickname to use</param>
        /// <param name="_ident">Ident</param>
        /// <param name="_username">Username</param>
        public IRC(string _server, string _nick, string _ident, string _username)
        {
            Server = _server;
            Password = "";
            _SlowQueue = new SlowQueue(this);
            UserName = _username;
            NickName = _nick;
            Ident = _ident;
        }

        /// <summary>
        /// Send a message to channel
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="channel">Channel</param>
        /// <returns></returns>
        public bool Message(string message, string channel)
        {
            try
            {
                config.channel curr = core.getChannel(channel);
                if (curr == null && channel.StartsWith("#"))
                {
                    Program.Log("Attempt to send a message to non existing channel: " + channel + " " + message, true);
                    return true;
                }
                if (curr != null && curr.suppress)
                {
                    return true;
                }
                SendData("PRIVMSG " + channel + " :" + message.Replace("\n", " "));
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        try
                        {
                            if (module.working)
                            {
                                module.Hook_OnSelf(curr, new User(config.username, "wikimedia/bot/wm-bot", "wmib"), message);
                            }
                        }
                        catch (Exception fail)
                        {
                            core.handleException(fail);
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return true;
        }

        /// <summary>
        /// Join a channel
        /// </summary>
        /// <param name="Channel">Channel</param>
        /// <returns></returns>
        public bool Join(config.channel Channel)
        {
            if (Channel != null)
            {
                SendData("JOIN " + Channel.Name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Restart a message delivery
        /// </summary>
        public void RestartIRCMessageDelivery()
        {
            this._Queue.Abort();
            this._SlowQueue.newmessages.Clear();
            this._Queue = new System.Threading.Thread(new System.Threading.ThreadStart(_SlowQueue.Run));
            this._SlowQueue.messages.Clear();
            this._Queue.Start();
        }

        /// <summary>
        /// This function will retrieve a list of users in a channel for every channel that doesn't have it so far
        /// </summary>
        public void ChannelList()
        {
            try
            {
                while (IsConnected)
                {
                    List<string> channels = new List<string>();
                    // check if there is some channel which needs an update of user list
                    lock (config.channels)
                    {
                        foreach (config.channel dd in config.channels)
                        {
                            if (!dd.FreshList)
                            {
                                channels.Add(dd.Name);
                            }
                        }
                    }

                    if (channels.Count >= 1)
                    {
                        foreach (string xx in channels)
                        {
                            Program.Log("requesting user list for" + xx);
                            // Send the request with low priority
                            _SlowQueue.Send("WHO " + xx, priority.low);
                        }
                        // we give 10 seconds for each channel to send us a list
                        Thread.Sleep(10000 * channels.Count);
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }

        /// <summary>
        /// Ping
        /// </summary>
        public void Ping()
        {
            while (IsConnected)
            {
                try
                {
                    System.Threading.Thread.Sleep(20000);
                    if (!config.UsingNetworkIOLayer)
                    {
                        SendData("PING :" + config.network);
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception fail)
                {
                    core.handleException(fail);
                }
            }
        }

        /// <summary>
        /// Connection
        /// </summary>
        /// <returns></returns>
        public bool Reconnect()
        {
            _Queue.Abort();
            networkStream = config.UsingNetworkIOLayer ? new System.Net.Sockets.TcpClient("127.0.0.1", 6667).GetStream() : new System.Net.Sockets.TcpClient(Server, 6667).GetStream();
            connected = true;
            streamReader = new StreamReader(networkStream, System.Text.Encoding.UTF8);
            streamWriter = new StreamWriter(networkStream);
            SendData("USER " + UserName + " 8 * :" + Ident);
            SendData("NICK " + NickName);
            Authenticate();
            _Queue = new Thread(_SlowQueue.Run);
            foreach (config.channel ch in config.channels)
            {
                Thread.Sleep(2000);
                this.Join(ch);
            }
            _SlowQueue.newmessages.Clear();
            _SlowQueue.messages.Clear();
            ChannelThread.Abort();
            ChannelThread = new Thread(ChannelList);
            ChannelThread.Start();
            _Queue.Start();
            return false;
        }

        /// <summary>
        /// Send data to network
        /// </summary>
        /// <param name="text"></param>
        public void SendData(string text)
        {
            if (IsConnected)
            {
                lock (this)
                {
                    streamWriter.WriteLine(text);
                    streamWriter.Flush();
                    core.TrafficLog("MAIN>>>>>>" + text);
                }
            }
            else
            {
                core.Log("DEBUG: didn't send data to network, because it's not connected");
            }
        }

        /// <summary>
        /// Identify
        /// </summary>
        /// <returns></returns>
        public bool Authenticate()
        {
            if (config.password != "")
            {
                SendData("PRIVMSG nickserv :identify " + config.login + " " + config.password);
                System.Threading.Thread.Sleep(4000);
            }
            return true;
        }

        /// <summary>
        /// Connection
        /// </summary>
        /// <returns></returns>
        public void Connect()
        {
            try
            {
                if (!config.UsingNetworkIOLayer)
                {
                    networkStream = new System.Net.Sockets.TcpClient(Server, 6667).GetStream();
                }
                else
                {
                    networkStream = new System.Net.Sockets.TcpClient("127.0.0.1", 6667).GetStream();
                    Program.Log("System is using external bouncer");
                }
                connected = true;
                streamReader = new System.IO.StreamReader(networkStream, System.Text.Encoding.UTF8);
                streamWriter = new System.IO.StreamWriter(networkStream);

                bool Auth = true;

                if (config.UsingNetworkIOLayer)
                {
                    SendData("CONTROL: STATUS");
                    Console.WriteLine("CACHE: Waiting for buffer");
                    bool done = true;
                    while (done)
                    {
                        string response = streamReader.ReadLine();
                        if (response == "CONTROL: TRUE")
                        {
                            done = false;
                            Auth = false;
                        }
                        else if (response == "CONTROL: FALSE")
                        {
                            done = false;
                            SendData("CONTROL: CREATE");
                            streamWriter.Flush();
                        }
                    }
                }

                _Queue = new System.Threading.Thread(_SlowQueue.Run);

                check_thread = new System.Threading.Thread(Ping);
                check_thread.Start();
                ChannelThread = new Thread(ChannelList);
                ChannelThread.Start();

                if (Auth)
                {
                    SendData("USER " + UserName + " 8 * :" + Ident);
                    SendData("NICK " + NickName);
                }

                _Queue.Start();
                System.Threading.Thread.Sleep(2000);

                if (Auth)
                {
                    Authenticate();

                    foreach (config.channel ch in config.channels)
                    {
                        if (ch.Name != "")
                        {
                            this.Join(ch);
                            Thread.Sleep(2000);
                        }
                    }
                }
                string nick = "";
                string host = "";
                string channel = "";
                const char delimiter = (char)001;

                while (IsConnected)
                {
                    try
                    {
                        while (!streamReader.EndOfStream && core._Status == core.Status.OK)
                        {
                            string text = streamReader.ReadLine();
                            core.TrafficLog("MAIN<<<<<<" + text);
                            if (config.UsingNetworkIOLayer)
                            {
                                if (text.StartsWith("CONTROL: "))
                                {
                                    if (text == "CONTROL: DC")
                                    {
                                        SendData("CONTROL: CREATE");
                                        streamWriter.Flush();
                                        Console.WriteLine("CACHE: Lost connection to remote, reconnecting");
                                        bool Connected = false;
                                        while (!Connected)
                                        {
                                            System.Threading.Thread.Sleep(800);
                                            SendData("CONTROL: STATUS");
                                            string response = streamReader.ReadLine();
                                            core.TrafficLog("MAIN<<<<<<" + response);
                                            if (response == "CONTROL: OK")
                                            {
                                                Reconnect();
                                                Connected = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (text.StartsWith(":"))
                            {
                                ProcessorIRC processor = new ProcessorIRC(text);
                                processor.Result();
                                string check = text.Substring(text.IndexOf(" "));
                                if (check.StartsWith(" 005"))
                                {

                                }
                                else
                                {
                                    string command = "";
                                    if (text.Contains(" :"))
                                    {
                                        command = text.Substring(1);
                                        command = command.Substring(0, command.IndexOf(" :"));
                                    }
                                    if (command.Contains("PRIVMSG"))
                                    {
                                        string info = text.Substring(1, text.IndexOf(" :", 1) - 1);
                                        // we got a message here :)
                                        if (text.Contains("!") && text.Contains("@"))
                                        {
                                            nick = info.Substring(0, info.IndexOf("!"));
                                            host = info.Substring(info.IndexOf("@") + 1, info.IndexOf(" ", info.IndexOf("@")) - 1 - info.IndexOf("@"));
                                        }
                                        string info_host = info.Substring(info.IndexOf("PRIVMSG "));

                                        string message;
                                        if (info_host.Contains("#"))
                                        {
                                            channel = info_host.Substring(info_host.IndexOf("#"));
                                            message = text.Replace(info, "");
                                            message = message.Substring(message.IndexOf(" :") + 2);
                                            if (message.Contains(delimiter.ToString() + "ACTION"))
                                            {
                                                core.getAction(message.Replace(delimiter.ToString() + "ACTION", ""), channel, host, nick);
                                                continue;
                                            }
                                            core.getMessage(channel, nick, host, message);
                                            continue;
                                        }
                                        message = text.Substring(text.IndexOf("PRIVMSG"));
                                        message = message.Substring(message.IndexOf(" :"));
                                        // private message
                                        if (message.StartsWith(" :" + delimiter.ToString() + "FINGER"))
                                        {
                                            SendData("NOTICE " + nick + " :" + delimiter.ToString() + "FINGER" + " I am a bot don't finger me");
                                            continue;
                                        }
                                        if (message.StartsWith(" :" + delimiter.ToString() + "TIME"))
                                        {
                                            SendData("NOTICE " + nick + " :" + delimiter.ToString() + "TIME " + System.DateTime.Now.ToString());
                                            continue;
                                        }
                                        if (message.StartsWith(" :" + delimiter.ToString() + "PING"))
                                        {
                                            SendData("NOTICE " + nick + " :" + delimiter.ToString() + "PING" + message.Substring(message.IndexOf(delimiter.ToString() + "PING") + 5));
                                            continue;
                                        }
                                        if (message.StartsWith(" :" + delimiter.ToString() + "VERSION"))
                                        {
                                            SendData("NOTICE " + nick + " :" + delimiter.ToString() + "VERSION " + config.version);
                                            continue;
                                        }
                                        bool respond = true;
                                        string modules = "";
                                        lock (Module.module)
                                        {
                                            foreach (Module module in Module.module)
                                            {
                                                if (module.working)
                                                {
                                                    try
                                                    {

                                                        if (module.Hook_OnPrivateFromUser(message.Substring(2), new User(nick, host, Ident)))
                                                        {
                                                            respond = false;
                                                            modules += module.Name + " ";
                                                        }
                                                    }
                                                    catch (Exception fail)
                                                    {
                                                        core.handleException(fail);
                                                    }
                                                }
                                            }
                                        }
                                        if (respond)
                                        {
                                            _SlowQueue.DeliverMessage("Hi, I am robot, this command was not understood. Please bear in mind that every message you send to me will be logged for debuging purposes. See documentation at http://meta.wikimedia.org/wiki/WM-Bot for explanation of commands", nick, priority.low);
                                            Program.Log("Ignoring private message: (" + nick + ") " + message.Substring(2), false);
                                        }
                                        else
                                        {
                                            Program.Log("Private message: (handled by " + modules + " from "  + nick + ") " + message.Substring(2), false);
                                        }
                                        continue;
                                    }
                                    if (command.Contains("PING "))
                                    {
                                        SendData("PONG " + text.Substring(text.IndexOf("PING ") + 5));
                                        Console.WriteLine(command);
                                    }
                                    if (command.Contains("KICK"))
                                    {
                                        string temp = command.Substring(command.IndexOf("KICK"));
                                        string[] parts = temp.Split(' ');
                                        if (parts.Length > 1)
                                        {
                                            string _channel = parts[1];
                                            string user = parts[2];
                                            if (user == NickName)
                                            {
                                                config.channel chan = core.getChannel(_channel);
                                                if (chan != null)
                                                {
                                                    if (config.channels.Contains(chan))
                                                    {
                                                        config.channels.Remove(chan);
                                                        Program.Log("I was kicked from " + parts[1]);
                                                        config.Save();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            System.Threading.Thread.Sleep(50);
                        }
                        Program.Log("Reconnecting, end of data stream");
                        connected = false;
                        Reconnect();
                    }
                    catch (System.IO.IOException xx)
                    {
                        Program.Log("Reconnecting, connection failed " + xx.Message + xx.StackTrace);
                        connected = false;
                        Reconnect();
                    }
                    catch (Exception xx)
                    {
                        core.handleException(xx, channel);
                        core.Log("IRC: Connection error!! Terminating system");
                        connected = false;
                        core.Kill();
                    }
                }
            }
            catch (Exception)
            {
                core.Log("IRC: Connection error!! Terminating system");
                connected = false;
                // there is no point for being up when connection is dead and can't be reconnected
                core.Kill();
            }
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        /// <returns></returns>
        public int Disconnect()
        {
            if (IsConnected)
            {
                connected = false;
                try
                {
                    core.DebugLog("Closing");
                    streamWriter.Close();
                    streamReader.Close();
                    networkStream.Close();
                }
                catch (Exception fail)
                {
                    core.handleException(fail);
                }
            }
            return 0;
        }

        /// <summary>
        /// Priority of message
        /// </summary>
        public enum priority
        {
            /// <summary>
            /// Low
            /// </summary>
            low = 1,
            /// <summary>
            /// Normal
            /// </summary>
            normal = 2,
            /// <summary>
            /// High
            /// </summary>
            high = 3,
        }
    }
}
