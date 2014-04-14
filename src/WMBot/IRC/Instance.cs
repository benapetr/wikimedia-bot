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

        private Thread JoinThread;
        /// <summary>
        /// Each instance is running in its own thread, this is pointer to that thread
        /// </summary>
        private Thread thread;

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
            irc = new IRC(Configuration.IRC.NetworkHost, Nick, Configuration.IRC.Username, Configuration.IRC.Username, this)
            {
                Bouncer = Hostname,
                BouncerPort = Port
            };
        }

        /// <summary>
        /// Join all channels
        /// </summary>
        public void Join()
        {
            JoinThread = new Thread(JoinAll) {Name = "Jointhread:" + Nick};
            Core.ThreadManager.RegisterThread(JoinThread);
            JoinThread.Start();
        }

        public int QueueSize()
        {
            if (irc == null || irc.Queue == null)
            {
                return 0;
            }
            return irc.Queue.Size();
        }

        /// <summary>
        /// This is a private handler for channel joining, never call it directly, use Join() for that
        /// </summary>
        private void JoinAll()
        {
            if (irc.ChannelsJoined == false)
            {
                while (!irc.IsWorking)
                {
                    Syslog.DebugLog("JOIN THREAD: Waiting for " + Nick + " to finish connection to IRC server", 6);
                    Thread.Sleep(1000);
                }
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

            irc.ChannelThread = new Thread(irc.ChannelList) {Name = "ChannelList:" + Nick};
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
            this.IsActive = true;
            thread.Name = "Instance:" + Nick;
            Core.ThreadManager.RegisterThread(thread);
            thread.Start();
        }

        /// <summary>
        /// Shut down
        /// </summary>
        public void ShutDown()
        {
            this.IsActive = false;
            if (thread != null)
            {
                Core.ThreadManager.KillThread(thread);
            }
            Thread.Sleep(200);
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
            while (this.IsActive && Core.IsRunning)
            {
                try
                {
                    // we first attempt to disconnect this instance, if this is first loop
                    // it will just skip it
                    irc.Disconnect();
                    irc.Connect();
                    Join();
                    irc.ParserExec();

                } catch (ThreadAbortException)
                {
                    Syslog.DebugLog("Terminated primary thread for instance " + Nick);
                    return;
                }catch (IOException fail) 
                {
                    if (this.IsActive)
                    {
                        Syslog.ErrorLog("Failure of primary thread of instance " + Nick + " attempting to recover");
                        Core.HandleException(fail);
                    } else
                    {
                        return;
                    }
                }catch (Exception fail)
                {
                    Core.HandleException(fail);
                    if (this.IsActive)
                    {
                        Syslog.ErrorLog("Failure of primary thread of instance " + Nick + " attempting to recover");
                    } else
                    {
                        return;
                    }
                    Thread.Sleep(20000);
                }
            }
            Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
        }
    }
}
