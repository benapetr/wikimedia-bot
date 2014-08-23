//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Web;

namespace wmib
{
    /// <summary>
    /// This is a custom protocol for handling irc requests that is capable of parsing input from
    /// multiple sessions (connections) so that we can use only 1 network instance for all wm-bot
    /// sessions that are connected to target server
    /// </summary>
    public class WmIrcProtocol : libirc.Protocols.ProtocolIrc
    {
        public string BouncerHost = "127.0.0.1";
        public int BouncerPort = 6667;
        private Thread main;
        /// <summary>
        /// During outage the bouncer is collecting the IRC traffic, so when we reconnect it get copied to
        /// this array which we need to later process
        /// </summary>
        private List<string> Backlog = new List<string>();
        public bool ChannelsJoined = false;
        public bool IsWorking = false;
        public bool IsDisconnected = false;
        // we have to return false here because the wm-bot bouncer doesn't support ssl yet
        public override bool SupportSSL
        {
            get
            {
                return false;
            }
        }

        public WmIrcProtocol(string ServerHost, string bouncerHost, int bouncerPort) : base()
        {
            this.Server = ServerHost;
            this.BouncerPort = bouncerPort;
            this.BouncerHost = bouncerHost;
        }

        private void NetworkInit()
        {
            this.Send("USER " + IRCNetwork.UserName + " 8 * :" + IRCNetwork.Ident);
            this.Send("NICK " + IRCNetwork.Nickname);

            Authenticate();
        }

        public bool Authenticate(bool wait = true)
        {
            if (!String.IsNullOrEmpty(Configuration.IRC.LoginPw))
            {
                this.Send("PRIVMSG nickserv :identify " + Configuration.IRC.LoginNick + " " + Configuration.IRC.LoginPw);
                if (wait)
                    Thread.Sleep(4000);
            }
            return true;
        }

        private bool _Connect()
        {
            Syslog.Log("Connecting instance " + this.IRCNetwork.Nickname + " to irc server " + Server + "...");
            if (!Configuration.IRC.UsingBouncer)
            {
                networkStream = new TcpClient(Server, 6667).GetStream();
            } else
            {
                Syslog.Log(this.IRCNetwork.Nickname + " is using personal bouncer port " + BouncerPort.ToString());
                networkStream = new TcpClient(BouncerHost, BouncerPort).GetStream();
            }
            Connected = true;
            streamReader = new StreamReader(networkStream, System.Text.Encoding.UTF8);
            streamWriter = new StreamWriter(networkStream);
            bool Auth = true;
            if (Configuration.IRC.UsingBouncer)
            {
                this.Send("CONTROL: STATUS");
                Syslog.Log("CACHE: Waiting for buffer (network bouncer) of instance " + this.IRCNetwork.Nickname);
                while (true)
                {
                    string response = streamReader.ReadLine();
                    this.TrafficLog(response, true);
                    if (response == "CONTROL: TRUE")
                    {
                        Syslog.DebugLog("Resumming previous session on " + this.IRCNetwork.Nickname);
                        Auth = false;
                        IRCNetwork.IsConnected = true;
                        ChannelsJoined = true;
                        IsWorking = true;
                        // this here is a nasty hack to make libirc think that we just joined all the channels
                        // we should be already on
                        foreach (Instance xx in Instance.Instances.Values)
                        {
                            foreach (Channel channel in xx.ChannelList)
                            {
                                channel.PrimaryInstance.Network.MakeChannel(channel.Name);
                            }
                        }
                        break;
                    } else if (response.StartsWith(":"))
                    {
                        Backlog.Add(response);
                    } else if (response == "CONTROL: FALSE")
                    {
                        Syslog.DebugLog("Bouncer is not connected, starting new session on " + IRCNetwork.Nickname);
                        if (!this.connectBnc())
                        {
                            return false;
                        }
                        break;
                    }
                }
            }
            Messages.protocol = this;
            if (Auth)
                NetworkInit();
            if (!this.ManualThreads)
            {
                this.TKeep = new Thread(_Ping);
                this.TKeep.Name = "Instance:" + this.Server + "/pinger";
                Core.ThreadManager.RegisterThread(TKeep);
                TKeep.Start();
            }
            if (!this.ManualThreads)
            {
                TDeliveryQueue = new System.Threading.Thread(Messages.Run);
                TDeliveryQueue.Name = "Instance:" + this.Server + "/queue";
                Core.ThreadManager.RegisterThread(TDeliveryQueue);
                TDeliveryQueue.Start();
            }
            return true;
        }

        public override Result Transfer(string text, libirc.Defs.Priority priority, libirc.Network network)
        {
            return base.Transfer(text, priority, network);
        }

        public override void TrafficLog(string text, bool incoming)
        {
            if (incoming)
            {
                Core.TrafficLog(IRCNetwork.Nickname + " << " + text);
            }
            else
            {
                Core.TrafficLog(IRCNetwork.Nickname + " >> " + text);
            }
        }

        private bool connectBnc()
        {
            this.Send("CONTROL: CREATE " + this.Server);
            this.ChannelsJoined = false;
            int retries = 0;
            bool Connected_ = false;
            while (!Connected_)
            {
                Thread.Sleep(2000);
                this.Send("CONTROL: STATUS");
                string response = streamReader.ReadLine();
                this.TrafficLog(response, true);
                if (response.StartsWith(":"))
                {
                    // we received network data here
                    lock(Backlog)
                        Backlog.Add(response);
                    continue;
                }
                if (response == "CONTROL: TRUE")
                {
                    Syslog.Log("Bouncer connected to " + Server + " on: " + this.IRCNetwork.Nickname);
                    return true;
                } else
                {
                    retries++;
                    if (retries > 6)
                    {
                        Syslog.WarningLog("Bouncer failed to connect to the network within 10 seconds, disconnecting it: "
                                          + this.IRCNetwork.Nickname);
                        this.Send("CONTROL: DISCONNECT");
                        return false;
                    }
                    Syslog.Log("Still waiting for bouncer (trying " + retries.ToString() + "/6) on " + this.IRCNetwork.Nickname + " " + response);
                }
            }
            return true;
        }

        private void KillSelf(string reason)
        {
            this.SafeDc();
            this.DisconnectExec(reason);
            Core.ThreadManager.UnregisterThread(System.Threading.Thread.CurrentThread);
        }

        private void ThreadExec()
        {
            try
            {
                if (!this._Connect())
                {
                    this.KillSelf("Unable to connect to remote");
                    return;
                }
                // why is this??
                if (!IRCNetwork.IsConnected)
                    IRCNetwork.IsConnected = true;
                while (!streamReader.EndOfStream && IsConnected)
                {
                    string text;
                    if (Backlog.Count > 0)
                    {
                        lock (Backlog)
                        {
                            text = Backlog[0];
                            Backlog.RemoveAt(0);
                        }
                    } else
                    {
                        text = streamReader.ReadLine();
                    }
                    if (Configuration.IRC.UsingBouncer && text[0] == 'C' && text.StartsWith("CONTROL: "))
                    {
                        if (text == "CONTROL: DC")
                        {
                            Syslog.Log("CACHE: Lost connection to remote on " + this.IRCNetwork.Nickname);
                            this.ChannelsJoined = false;
                            this.IsWorking = false;
                            this.KillSelf("Lost connection to remote");
                            return;
                        }
                    }
                    text = this.RawTraffic(text);
                    this.TrafficLog(text, true);
                    libirc.ProcessorIRC processor = new libirc.ProcessorIRC(IRCNetwork, text, ref LastPing);
                    processor.ProfiledResult();
                    LastPing = processor.pong;
                }
            }catch (ThreadAbortException)
            {
                KillSelf("Thread aborted");
            }catch (System.Net.Sockets.SocketException ex)
            {
                this.KillSelf(ex.Message);
            }catch (System.IO.IOException ex)
            {
               this.KillSelf(ex.Message);
            } catch (Exception fail)
            {
                Core.HandleException(fail);
                this.KillSelf(fail.Message);
            }
        }

        protected override void SafeDc()
        {
            this.IsDisconnected = true;
            base.SafeDc();
            Core.ThreadManager.KillThread(this.TKeep);
            Core.ThreadManager.KillThread(this.TDeliveryQueue);
        }

        public override Result Disconnect()
        {
            // we lock the function so that it can't be called in same time in different thread
            lock(this)
            {
                if (!IsConnected || IRCNetwork == null)
                {
                    return Result.Failure;
                }
                try
                {
                    IRCNetwork.IsConnected = false;
                    if (SSL)
                    {
                        networkSsl.Close();
                    } else
                    {
                        networkStream.Close();
                    }
                    streamWriter.Close();
                    streamReader.Close();
                    Connected = false;
                } catch (System.IO.IOException er)
                {
                    this.DebugLog(er.Message, 1);
                    Connected = false;
                }
                Core.ThreadManager.KillThread(this.main);
            }
            return Result.Done;
        }

        public override void DebugLog(string Text, int Verbosity)
        {
            Syslog.DebugLog(Text, Verbosity);
        }

        public override Thread Open()
        {
            this.main = new Thread(ThreadExec);
            this.main.Name = "Instance:" + this.IRCNetwork.Nickname + "/IRC";
            Core.ThreadManager.RegisterThread(main);
            this.main.Start();
            return this.main;
        }
    }
}

