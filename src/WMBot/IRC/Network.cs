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

namespace wmib
{
    public class Network : libirc.Network
    {
        private Instance instance;
        public Network(string server, Instance Instance, WmIrcProtocol protocol)
            : base(server, (libirc.Protocols.ProtocolIrc)protocol)
        {
            this.instance = Instance;
            this.Config.TrafficInterval = Configuration.IRC.Interval;
            this.Config.Nick = Instance.Nick;
            if (Configuration.IRC.UsingBouncer)
                this.IsLoaded = true;
        }

        protected override void __evt_CTCP(NetworkCTCPEventArgs args)
        {
            switch (args.CTCP)
            {
                case "FINGER":
                    Transfer("NOTICE " + args.SourceInfo.Nick + " :" + _Protocol.Separator + "FINGER" +
                             " I am a bot don't finger me");
                    return;
                case "TIME":
                    Transfer("NOTICE " + args.SourceInfo.Nick + " :" + _Protocol.Separator + "TIME " + DateTime.Now.ToString());
                    return;
                case "PING":
                    Transfer("NOTICE " + args.SourceInfo.Nick + " :" + _Protocol.Separator + "PING" + args.Message.Substring(
                        args.Message.IndexOf(_Protocol.Separator + "PING") + 5));
                    return;
                case "VERSION":
                    Transfer("NOTICE " + args.SourceInfo.Nick + " :" + _Protocol.Separator + "VERSION "
                             + Configuration.System.Version + " http://meta.wikimedia.org/wiki/WM-Bot");
                    return;
            }
            Syslog.DebugLog("Ignoring uknown CTCP from " + args.Source + ": " + args.CTCP + args.Message);
        }

        protected override void __evt_KICK(NetworkKickEventArgs args)
        {
            if (args.ChannelName == Configuration.System.DebugChan && this.instance != Instance.PrimaryInstance)
                return;
            Channel channel = Core.GetChannel(args.ChannelName);
            if (channel == null) return;
            SystemHooks.IrcKick(channel, args.SourceInfo, args.Target);
            if (this.Nickname.ToLower() == args.Target.ToLower())
            {
                Syslog.Log("I was kicked from " + args.ChannelName + " by " + args.SourceInfo.Nick + " because of: " + args.Message);
                lock (Configuration.Channels)
                {
                    if (Configuration.Channels.Contains(channel))
                    {
                        Configuration.Channels.Remove(channel);
                    }
                }
                Configuration.Save();
            }
        }

        protected override bool __evt__IncomingData(IncomingDataEventArgs args)
        {
            switch (args.Command)
            {
                case "001":
                case "002":
                    this.instance.IsWorking = true;
                    break;
            }
            return base.__evt__IncomingData(args);
        }

        protected override void __evt_JOIN(NetworkChannelEventArgs args)
        {
            if (args.ChannelName == Configuration.System.DebugChan && this.instance != Instance.PrimaryInstance)
                return;
            Channel channel = Core.GetChannel(args.ChannelName);
            if (channel != null)
            {
                foreach (Module module in ExtensionHandler.ExtensionList)
                {
                    try
                    {
                        if (module.IsWorking)
                        {
                            module.Hook_Join(channel, args.SourceInfo);
                        }
                    }
                    catch (Exception fail)
                    {
                        Syslog.Log("MODULE: exception at Hook_Join in " + module.Name, true);
                        Core.HandleException(fail);
                    }
                }
            }
        }

        protected override void __evt_PART(NetworkChannelDataEventArgs args)
        {
            if (args.ChannelName == Configuration.System.DebugChan && this.instance != Instance.PrimaryInstance)
                return;
            Channel channel = Core.GetChannel(args.ChannelName);
            if (channel != null)
            {
                foreach (Module module in ExtensionHandler.ExtensionList)
                {
                    if (!module.IsWorking)
                        continue;

                    try
                    {
                        module.Hook_Part(channel, args.SourceInfo);
                    }
                    catch (Exception fail)
                    {
                        Syslog.Log("MODULE: exception at Hook_Part in " + module.Name, true);
                        Core.HandleException(fail);
                    }
                }
            }
        }

        protected override void __evt_QUIT(NetworkGenericDataEventArgs args)
        {
            foreach (Module module in ExtensionHandler.ExtensionList)
            {
                if (!module.IsWorking)
                    continue;

                try
                {
                    module.Hook_Quit(args.SourceInfo, args.Message);
                }
                catch (Exception fail)
                {
                    Syslog.Log("MODULE: exception at Hook_Quit in " + module.Name, true);
                    Core.HandleException(fail);
                }
            }
            foreach (Channel channel in instance.ChannelList)
            {
                if (channel.ContainsUser(args.SourceInfo.Nick))
                {
                    foreach (Module module in ExtensionHandler.ExtensionList)
                    {
                        if (!module.IsWorking)
                            continue;

                        try
                        {
                            module.Hook_ChannelQuit(channel, args.SourceInfo, args.Message);
                        }
                        catch (Exception fail)
                        {
                            Syslog.Log("MODULE: exception at Hook_ChannelQuit in " + module.Name, true);
                            Core.HandleException(fail);
                        }
                    }
                }
            }
        }

        protected override void __evt_NICK(NetworkNICKEventArgs args)
        {
            foreach (Channel channel in instance.ChannelList)
            {
                if (channel.ContainsUser(args.OldNick))
                {
                    foreach (Module extension_ in ExtensionHandler.ExtensionList)
                    {
                        try
                        {
                            if (extension_.IsWorking)
                                extension_.Hook_Nick(channel, args.SourceInfo, args.OldNick, args.NewNick);

                        }
                        catch (Exception fail)
                        {
                            Syslog.Log("MODULE: exception in Hook_Nick in " + extension_.Name, true);
                            Core.HandleException(fail);
                        }
                    }
                }
            }
        }

        protected override void __evt_JOINERROR(libirc.Network.NetworkJoinErrorEventArgs args)
        {
            if (!string.IsNullOrEmpty(Configuration.System.DebugChan))
            {
                IRC.DeliverMessage("Join error: " + args.Message + " channel: " + args.ParameterLine + " reason: " + args.Error.ToString(),
                                   Configuration.System.DebugChan);
            }
            base.__evt_JOINERROR(args);
        }

        protected override void __evt_FinishChannelParseUser(NetworkChannelDataEventArgs args)
        {
            Channel channel = Core.GetChannel(args.ChannelName);
            Syslog.DebugLog("Finished parsing of user list for channel: " + args.ChannelName);
            if (channel != null)
                channel.HasFreshUserList = true;
        }

        protected override void __evt_PRIVMSG(NetworkPRIVMSGEventArgs args)
        {
            if (args.ChannelName == null)
            {
                // private message
                // store which instance this message was from so that we can send it using same instance
                lock (Instance.TargetBuffer)
                {
                    if (!Instance.TargetBuffer.ContainsKey(args.SourceInfo.Nick))
                        Instance.TargetBuffer.Add(args.SourceInfo.Nick, this.instance);
                    else
                        Instance.TargetBuffer[args.SourceInfo.Nick] = this.instance;
                }
                string modules = "";
                bool respond = !Commands.Trusted(args.Message, args.SourceInfo.Nick, args.SourceInfo.Host);
                if (!respond)
                    modules += "@trusted ";
                foreach (Module module in ExtensionHandler.ExtensionList)
                {
                    if (module.IsWorking)
                    {
                        try
                        {
                            if (module.Hook_OnPrivateFromUser(args.Message, args.SourceInfo))
                            {
                                respond = false;
                                modules += module.Name + " ";
                            }
                        }
                        catch (Exception fail)
                        {
                            Core.HandleException(fail);
                        }
                    }
                }
                if (respond)
                {
                    IRC.DeliverMessage("Hi, I am robot, this command was not understood." +
                                         " Please bear in mind that every message you send" +
                                         " to me will be logged for debuging purposes. See" +
                                         " documentation at http://meta.wikimedia.org/wiki" +
                                         "/WM-Bot for explanation of commands", args.SourceInfo,
                                         libirc.Defs.Priority.Low);
                    Syslog.Log("Ignoring private message: (" + args.SourceInfo.Nick + ") " + args.Message, false);
                }
                else
                {
                    modules = Core.Trim(modules);
                    Syslog.Log("Private message: (handled by " + modules + " from " + args.SourceInfo.Nick + ") " +
                               args.Message, false);
                }
            }
            else
            {
                if (args.ChannelName == Configuration.System.DebugChan && this.instance != Instance.PrimaryInstance)
                    return;
                if (args.IsAct)
                {
                    Core.GetAction(args.Message, args.ChannelName, args.SourceInfo.Host, args.SourceInfo.Nick);
                    return;
                }
                Core.GetMessage(args.ChannelName, args.SourceInfo.Nick, args.SourceInfo.Host, args.Message);
            }
        }
    }
}

