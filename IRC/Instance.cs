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
using System.Text;

namespace wmib
{
    /// <summary>
    /// Represent one instance of this bot
    /// </summary>
    public class Instance
    {
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
        /// If this instance is connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (irc != null && irc.IsConnected)
                {
                    return true;
                }
                return false;
            }
        }

        private Thread JoinThread = null;
        /// <summary>
        /// Each instance is running in its own thread, this is pointer to that thread
        /// </summary>
        private Thread thread = null;

        /// <summary>
        /// List of channels this instance is in
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
        /// Whether this instance is working
        /// </summary>
        public bool IsWorking
        {
            get
            {
                return irc.IsWorking;
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
        /// Pointer to IRC handler for this instance
        /// </summary>
        public IRC irc = null;

        /// <summary>
        /// Creates a new bot instance but not connect it to IRC
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="port">Port</param>
        public Instance(string name, int port = 0)
        {
            Nick = name;
            Port = port;
            irc = new IRC(Configuration.IRC.NetworkHost, Nick, Configuration.IRC.Username, Configuration.IRC.Username, this);
            irc.Bouncer = Hostname;
            irc.BouncerPort = Port;
        }

        /// <summary>
        /// Join all channels
        /// </summary>
        public void Join()
        {
            JoinThread = new Thread(JoinAll);
            JoinThread.Name = "Jointhread:" + Nick;
			Core.ThreadManager.RegisterThread(JoinThread);
            JoinThread.Start();
        }

        /// <summary>
        /// This is a private handler for channel joining, never call it directly, use Join() for that
        /// </summary>
        private void JoinAll()
        {
            if (irc.ChannelsJoined == false)
            {
                if (Configuration.System.DebugChan != null)
                {
                    irc.SendData("JOIN " + Configuration.System.DebugChan);
                }
                foreach (Channel channel in ChannelList)
                {
                    if (channel.Name != "" && channel.Name != Configuration.System.DebugChan)
                    {
                        Syslog.DebugLog("Joining " + channel.Name + " on " + Nick);
                        irc.Join(channel);
                        Thread.Sleep(2000);
                    }
                }
                irc.ChannelsJoined = true;
            }

            irc.ChannelThread = new Thread(irc.ChannelList);
			irc.ChannelThread.Name = "ChannelList:" + Nick;
			Core.ThreadManager.RegisterThread(irc.ChannelThread);
            irc.ChannelThread.Start();
			Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
        }

        /// <summary>
        /// Create this instance
        /// </summary>
        public void Init()
        {
            thread = new Thread(Connect);
            thread.Name = "Instance:" + Nick;
			Core.ThreadManager.RegisterThread(thread);
            thread.Start();
        }

        /// <summary>
        /// Shut down
        /// </summary>
        public void Shut()
        {
            if (thread != null)
            {
                Core.ThreadManager.KillThread(thread);
            }
            if (irc != null)
            {
                irc.Disconnect();
            }
        }

        /// <summary>
        /// Connect the instance
        /// </summary>
        private void Connect()
        {
            irc.Connect();
			Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
        }
    }
}
