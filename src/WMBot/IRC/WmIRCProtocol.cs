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

        private bool Authenticate()
        {
            if (Configuration.IRC.LoginPw != "")
            {
                this.Send("PRIVMSG nickserv :identify " + Configuration.IRC.LoginNick + " " + Configuration.IRC.LoginPw);
                Thread.Sleep(4000);
            }
            return true;
        }

        private void _Connect()
        {
            Syslog.Log("Connecting instance " + this.IRCNetwork.Nickname + " to irc server " + Server + "...");
            if (!Configuration.IRC.UsingBouncer)
            {
                networkStream = new TcpClient(Server, 6667).GetStream();
            } else
            {
                Syslog.Log(this.IRCNetwork.Nickname + " is using personal bouncer port " + BouncerPort);
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
                bool done = true;
                while (done)
                {
                    string response = streamReader.ReadLine();
                    if (response == "CONTROL: TRUE")
                    {
                        Syslog.DebugLog("Resumming previous session on " + this.IRCNetwork.Nickname);
                        done = false;
                        Auth = false;
                        ChannelsJoined = true;
                        IsWorking = true;
                    } else if (response.StartsWith(":"))
                    {
                        Backlog.Add(response);
                    } else if (response == "CONTROL: FALSE")
                    {
                        Syslog.DebugLog("Bouncer is not connected, starting new session on " + IRCNetwork.Nickname);
                        done = false;
                        ChannelsJoined = false;
                        this.Send("CONTROL: CREATE " + Server);
                        streamWriter.Flush();
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
        }

        public override Result Transfer(string text, libirc.Defs.Priority priority, libirc.Network network)
        {
            return base.Transfer(text, priority, network);
        }

        public override void TrafficLog(string text, bool incoming)
        {
            if (incoming)
            {
                Syslog.DebugLog(IRCNetwork.Nickname + ">>" + text);
                Core.TrafficLog(IRCNetwork.Nickname + ">>" + text);
            }
            else
            {
                Syslog.DebugLog(IRCNetwork.Nickname + "<<" + text);
                Core.TrafficLog(IRCNetwork.Nickname + "<<" + text);
            }
        }

        private void ThreadExec()
        {
            try
            {
                this._Connect();
                try
                {
                    while (!streamReader.EndOfStream && IsConnected)
                    {
                        if (!IRCNetwork.IsConnected)
                        {
                            IRCNetwork.IsConnected = true;
                        }
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
                        text = this.RawTraffic(text);
                        this.TrafficLog(text, true);
                        libirc.ProcessorIRC processor = new libirc.ProcessorIRC(IRCNetwork, text, ref LastPing);
                        processor.ProfiledResult();
                        LastPing = processor.pong;
                    }
                }catch (ThreadAbortException)
                {
                    this.SafeDc();
                    this.DisconnectExec("Thread aborted");
                }catch (System.Net.Sockets.SocketException ex)
                {
                    this.SafeDc();
                    this.DisconnectExec(ex.Message);
                }catch (System.IO.IOException ex)
                {
                    this.SafeDc();
                    this.DisconnectExec(ex.Message);
                }
                Core.ThreadManager.UnregisterThread(System.Threading.Thread.CurrentThread);
            } catch (Exception fail)
            {
                Core.HandleException(fail);
                this.SafeDc();
                this.DisconnectExec(fail.Message);
                Core.ThreadManager.UnregisterThread(this.main);
            }
        }

        protected override void SafeDc()
        {
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
                    this.Send("QUIT :" + IRCNetwork.Quit);
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
                    this.DebugLog(er.Message);
                    Connected = false;
                }
                Core.ThreadManager.KillThread(this.main);
            }
            return Result.Done;
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

