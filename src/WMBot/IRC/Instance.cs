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
using System.IO;
using System.Threading;

namespace wmib
{
    /// <summary>
    /// Represent one instance of this bot
    /// </summary>
    public class Instance
    {
        public static Instance PrimaryInstance = null;
        /// <summary>
        /// List of instances
        /// </summary>
        public static Dictionary<string, Instance> Instances = new Dictionary<string, Instance>();
        /// <summary>
        /// Targets of each instance
        /// 
        /// This is used to remember the last instance that user was talking to in a private message
        /// so that we respond to user using the same instance and not primary one
        /// </summary>
        public static Dictionary<string, Instance> TargetBuffer = new Dictionary<string, Instance>();
        public WmIrcProtocol Protocol = null;
        public Network Network = null;
        /// <summary>
        /// Nickname of this instance
        /// </summary>
        public string Nick;
        /// <summary>
        /// Port for bouncer
        /// </summary>
        public int Port = 0;
        /// <summary>
        /// Host for bouncer
        /// </summary>
        public string Hostname = "127.0.0.1";
        /// <summary>
        /// If you need to permanently disconnect this instance, change this to false
        /// </summary>
        public bool IsActive = true;
        public bool ChannelsJoined
        {
            set
            {
                Protocol.ChannelsJoined = value;
            }
            get
            {
                return Protocol.ChannelsJoined;
            }
        }
        /// <summary>
        /// Whether this instance has finished connection to IRC server, this is used because of several
        /// freenode issues, where some random commands sent during irc server connection get ignored
        /// when connection didn't finish (basically after we receive MOTD it's all OK, so we flag this
        /// to true after we receive motd).
        /// </summary>
        public bool IsWorking
        {
            get
            {
                return Protocol.IsWorking;
            }
            set
            {
                Protocol.IsWorking = value;
            }
        }
        /// <summary>
        /// If this instance is connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.Network != null && this.Network.IsConnected;
            }
        }
        private Thread JoinThread;
        /// <summary>
        /// Each instance is running in its own thread, this is pointer to that thread
        /// </summary>
        private Thread thread;
        /// <summary>
        /// List of channels this instance is now used in
        /// </summary>
        public List<Channel> ChannelList
        {
            get
            {
                List<Channel> list = new List<Channel>();
                foreach (Channel ch in Configuration.ChannelList)
                {
                    if (ch.PrimaryInstance == this)
                    {
                        list.Add(ch);
                    }
                }
                return list;
            }
        }

        /// <summary>
        /// Number of channels that are being used by this instance
        /// </summary>
        public int ChannelCount
        {
            get
            {
                int Channels = 0;
                foreach (Channel channel in Configuration.ChannelList)
                {
                    if (channel.PrimaryInstance == this)
                    {
                        Channels++;
                    }
                }
                return Channels;
            }
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="name"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Instance CreateInstance(string name, int port = 0)
        {
            Syslog.DebugLog("Creating instance " + name + " with port " + port);
            Instance instance = new Instance(name, port);
            if (Instances.ContainsKey(name))
            {
                throw new Exception("Can't load instance " + name + " because this instance is already running");
            }
            Instances.Add(name, instance);
            return instance;
        }

        /// <summary>
        /// Return instance with lowest number of channels
        /// </summary>
        /// <returns></returns>
        public static Instance GetInstance()
        {
            int lowest = 99999999;
            Instance instance = null;
            // first try to get instance which is online
            foreach (Instance xx in Instances.Values)
            {
                if (xx.IsConnected && xx.IsWorking && xx.ChannelCount < lowest)
                {
                    lowest = xx.ChannelCount;
                    instance = xx;
                }
            }
            // if there is no such return any instance with low channels
            if (instance == null)
            {
                foreach (Instance xx in Instances.Values)
                {
                    if (xx.ChannelCount < lowest)
                    {
                        lowest = xx.ChannelCount;
                        instance = xx;
                    }
                }
            }
            return instance;
        }

        public static void Kill()
        {

        }

        public static void ConnectAllIrcInstances()
        {
            foreach (Instance instance in Instances.Values)
            {
                // connect it to irc
                instance.Init();
            }
            // now we need to wait for all instances to connect
            Syslog.Log("Waiting for all instances to connect to irc");
            bool IsOk = false;
            while (!IsOk)
            {
                foreach (Instance instance in Instances.Values)
                {
                    if (!instance.IsWorking)
                    {
                        Syslog.DebugLog("Waiting for " + instance.Nick, 2);
                        Thread.Sleep(1000);
                        IsOk = false;
                        break;
                    }
                    Syslog.DebugLog("Connected to " + instance.Nick, 6);
                    IsOk = true;
                }
            }

            // wait for all instances to join their channels
            Syslog.Log("Waiting for all instances to join channels");
            IsOk = false;
            while (!IsOk)
            {
                foreach (Instance instance in Instances.Values)
                {
                    if (!instance.ChannelsJoined)
                    {
                        Thread.Sleep(100);
                        IsOk = false;
                        break;
                    }
                    IsOk = true;
                }
            }
            Syslog.Log("All instances joined their channels");
        }

        /// <summary>
        /// Creates a new bot instance but not connect it to IRC
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="port">Port</param>
        public Instance(string name, int port = 0)
        {
            Nick = name;
            Port = port;
            this.Protocol = new WmIrcProtocol(Configuration.IRC.NetworkHost, Hostname, Port);
            this.Network = new Network(Configuration.IRC.NetworkHost, this, this.Protocol);
            this.Network.Nickname = Nick;
            this.Network.UserName = Configuration.IRC.Username;
            this.Network.Ident = Configuration.IRC.Ident;
            this.Protocol.IRCNetwork = this.Network;
        }

        /// <summary>
        /// Join all channels
        /// </summary>
        public void Join()
        {
            JoinThread = new Thread(JoinAll) { Name = "Jointhread:" + Nick };
            Core.ThreadManager.RegisterThread(JoinThread);
            JoinThread.Start();
        }

        public void ShutDown()
        {
            this.IsActive = false;
            this.Disconnect();
            Core.ThreadManager.KillThread(thread);
        }

        public int QueueSize()
        {
            if (this.Protocol == null)
                return 0;

            return this.Protocol.QueueSize;
        }

        /// <summary>
        /// This is a private handler for channel joining, never call it directly, use Join() for that
        /// </summary>
        private void JoinAll()
        {
            try
            {
                if (this.ChannelsJoined == false)
                {
                    while (!this.IsWorking)
                    {
                        if (!this.IsActive || !Core.IsRunning)
                        {
                            Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
                            return;
                        }
                        Syslog.DebugLog("JOIN THREAD: Waiting for " + Nick + " to finish connection to IRC server", 6);
                        Thread.Sleep(1000);
                    }
                    if (Configuration.System.DebugChan != null)
                    {
                        this.Network.Join(Configuration.System.DebugChan);
                    }
                    foreach (Channel channel in ChannelList)
                    {
                        if (channel.Name.Length > 0 && channel.Name != Configuration.System.DebugChan)
                        {
                            Syslog.DebugLog("Joining " + channel.Name + " on " + Nick);
                            this.Network.Join(channel.Name);
                            Thread.Sleep(1000);
                        }
                    }
                    this.ChannelsJoined = true;
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
        }

        /// <summary>
        /// Create this instance
        /// </summary>
        public void Init()
        {
            thread = new Thread(Exec);
            this.IsActive = true;
            thread.Name = "Instance:" + Nick;
            Core.ThreadManager.RegisterThread(thread);
            thread.Start();
        }

        public void Connect()
        {
            this.Protocol.IsDisconnected = false;
            this.Protocol.Open();
        }

        public void Disconnect()
        {
            this.IsWorking = false;
            this.Protocol.Disconnect();
        }

        private void Exec()
        {
            while (this.IsActive && Core.IsRunning)
            {
                try
                {
                    this.Disconnect();
                    this.Connect();
                    while (!this.IsWorking && !this.Protocol.IsDisconnected)
                    {
                        // we need to wait for the irc handler to connect to irc
                        Thread.Sleep(100);
                    }
                    // now we can finally join all channels
                    Join();
                    // then we just sleep
                    while (this.Network.IsConnected)
                    {
                        Thread.Sleep(2000);
                    }
                    // in case we got disconnected, we log it and restart the procedure
                    Syslog.WarningLog("Disconnected from irc network on " + Nick);
                    Thread.Sleep(20000);
                }
                catch (ThreadAbortException)
                {
                    Syslog.DebugLog("Terminated primary thread for instance " + Nick);
                    return;
                }
                catch (IOException fail)
                {
                    if (this.IsActive)
                    {
                        Syslog.ErrorLog("Failure of primary thread of instance " + Nick + " attempting to recover");
                        Core.HandleException(fail);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception fail)
                {
                    Core.HandleException(fail);
                    if (this.IsActive)
                        Syslog.ErrorLog("Failure of primary thread of instance " + Nick + " attempting to recover");
                    else
                        return;
                    Thread.Sleep(20000);
                }
            }
            Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
        }
    }
}
