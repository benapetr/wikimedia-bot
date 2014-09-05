//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Copyright 2013 - 2014 Petr Bena (benapetr@gmail.com)

using System;
using System.IO;
using System.Threading;

namespace wmib
{
    public partial class Commands
    {
        /// <summary>
        /// Join channel
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        public static void AddChannel(CommandParams parameters)
        {
            if (!String.IsNullOrEmpty(parameters.Parameters))
            {
                while (!IRC.FinishedJoining)
                {
                    Syslog.Log("Postponing request to join " + parameters.Parameters + " because bot is still loading", true);
                    Thread.Sleep(2000);
                }
                string channel_name = parameters.Parameters.Trim();
                if (!Core.ValidFile(channel_name) || !channel_name.StartsWith("#"))
                {
                    IRC.DeliverMessage(messages.Localize("InvalidName", parameters.SourceChannel.Language), parameters.SourceChannel);
                    return;
                }
                lock (Configuration.Channels)
                {
                    foreach (Channel cu in Configuration.Channels)
                    {
                        if (channel_name == cu.Name)
                        {
                            IRC.DeliverMessage(messages.Localize("ChannelIn", parameters.SourceChannel.Language), parameters.SourceChannel);
                            return;
                        }
                    }
                }
                bool existing = Channel.ConfigExists(channel_name);
                Channel channel = new Channel(channel_name);
                lock (Configuration.Channels)
                {
                    Configuration.Channels.Add(channel);
                }
                Configuration.Save();
                channel.PrimaryInstance.Network.Join(channel_name);
                Thread.Sleep(100);
                Channel Chan = Core.GetChannel(channel_name);
                if (!existing)
                    Chan.SystemUsers.AddUser("admin", Security.EscapeUser(parameters.User.Nick) + "!.*@" + Security.EscapeUser(parameters.User.Host));

                return;
            }
            IRC.DeliverMessage(messages.Localize("InvalidName", parameters.SourceChannel.Language), parameters.SourceChannel);
            return;
        }

        /// <summary>
        /// Part a channel
        /// </summary>
        /// <param name="channel">Channel object</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        /// <param name="origin">The channel from which this request was sent</param>
        public static void PartChannel(Channel channel, string user, string host, string message, string origin = "NULL")
        {
            try
            {
                if (origin == "NULL")
                    origin = channel.Name;
                if (channel.Name == Configuration.System.DebugChan && (message == Configuration.System.CommandPrefix + "part"
                                                          || message == Configuration.System.CommandPrefix + "drop"))
                {
                    IRC.DeliverMessage("Cowardly refusing to part this channel, because I love it :3", channel);
                    return;
                }
                if (message == Configuration.System.CommandPrefix + "drop")
                {
                    if (channel.SystemUsers.IsApproved(user, host, "drop"))
                    {
                        while (!IRC.FinishedJoining)
                        {
                            Syslog.Log("Postponing request to part " + channel.Name + " because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        channel.PrimaryInstance.Network.Transfer("PART " + channel.Name + " :" + "dropped by " + user + " from " + origin);
                        Syslog.Log("Dropped " + channel.Name + " dropped by " + user + " from " + origin);
                        Thread.Sleep(100);
                        try
                        {
                            File.Delete(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".xml");
                        }
                        catch (Exception fail)
                        {
                            Syslog.ErrorLog("Failed to delete configuration file of " + channel.Name);
                            Core.HandleException(fail);
                        }
                        foreach (Module curr in ExtensionHandler.ExtensionList)
                        {
                            try
                            {
                                if (curr.IsWorking)
                                    curr.Hook_ChannelDrop(channel);
                            }
                            catch (Exception fail)
                            {
                                Syslog.Log("MODULE: exception at Hook_ChannelDrop in " + curr.Name, true);
                                Core.HandleException(fail, curr.Name);
                            }
                        }
                        lock (Configuration.Channels)
                        {
                            channel.Remove();
                        }
                        Configuration.Save();
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), origin);
                    return;
                }

                if (message == Configuration.System.CommandPrefix + "part")
                {
                    if (channel.SystemUsers.IsApproved(user, host, "part"))
                    {
                        while (!IRC.FinishedJoining)
                        {
                            Syslog.Log("Postponing request to part " + channel.Name + " because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        channel.PrimaryInstance.Network.Transfer("PART " + channel.Name + " :" + "removed by " + user + " from " + origin);
                        Syslog.Log("Removed " + channel.Name + " removed by " + user + " from " + origin);
                        Thread.Sleep(100);
                        lock (Configuration.Channels)
                        {
                            channel.Remove();
                        }
                        Configuration.Save();
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), origin);
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
        }
    }
}
