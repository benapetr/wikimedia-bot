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
using System.Threading;
using System.IO;

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
        public static void AddChannel(Channel channel, string user, string host, string message)
        {
            try
            {
                if (message.StartsWith(Configuration.System.CommandPrefix + "add ") ||
                    message.StartsWith(Configuration.System.CommandPrefix + "join "))
                {
                    if (channel.SystemUsers.IsApproved(user, host, "admin"))
                    {
                        while (!Core.FinishedJoining)
                        {
                            Syslog.Log("Postponing request to join because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        if (message.Contains(" "))
                        {
                            string _channel = message.Substring(message.IndexOf(" ") + 1).Trim();
                            if (!Core.ValidFile(_channel) || !_channel.StartsWith("#"))
                            {
                                Core.irc.Queue.DeliverMessage(messages.Localize("InvalidName", channel.Language), channel);
                                return;
                            }
                            lock (Configuration.Channels)
                            {
                                foreach (Channel cu in Configuration.Channels)
                                {
                                    if (_channel == cu.Name)
                                    {
                                        Core.irc.Queue.DeliverMessage(messages.Localize("ChannelIn", channel.Language), channel);
                                        return;
                                    }
                                }
                            }
                            bool existing = Channel.ConfigExists(_channel);
                            Channel xx = new Channel(_channel);
                            lock (Configuration.Channels)
                            {
                                Configuration.Channels.Add(xx);
                            }
                            Configuration.Save();
                            xx.PrimaryInstance.irc.SendData("JOIN " + _channel);
                            Thread.Sleep(100);
                            Channel Chan = Core.GetChannel(_channel);
                            if (!existing)
                            {
                                Chan.SystemUsers.AddUser("admin", Security.EscapeUser(user) + "!.*@" + Security.EscapeUser(host));
                            }
                            return;
                        }
                        channel.PrimaryInstance.irc.Message(messages.Localize("InvalidName", channel.Language), channel);
                        return;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                }
            }
            catch (Exception b)
            {
                Core.HandleException(b);
            }
        }

        /// <summary>
        /// Part a channel
        /// </summary>
        /// <param name="channel">Channel object</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        /// <param name="origin"></param>
        public static void PartChannel(Channel channel, string user, string host, string message, string origin = "NULL")
        {
            try
            {
                if (origin == "NULL")
                {
                    origin = channel.Name;
                }
                if (channel.Name == Configuration.System.DebugChan && (message == Configuration.System.CommandPrefix + "part" 
                                                          || message == Configuration.System.CommandPrefix + "drop"))
                {
                    channel.PrimaryInstance.irc.Queue.DeliverMessage("Cowardly refusing to part this channel, because I love it :3", channel);
                    return;
                }
                if (message == Configuration.System.CommandPrefix + "drop")
                {
                    if (channel.SystemUsers.IsApproved(user, host, "admin"))
                    {
                        while (!Core.FinishedJoining)
                        {
                            Syslog.Log("Postponing request to part " + channel.Name + " because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        channel.PrimaryInstance.irc.SendData("PART " + channel.Name + " :" + "dropped by " + user + " from " + origin);
                        Syslog.Log("Dropped " + channel.Name + " dropped by " + user + " from " + origin);
                        Thread.Sleep(100);
                        try
                        {
                            File.Delete(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".xml");
                            lock (ExtensionHandler.Extensions)
                            {
                                foreach (Module curr in ExtensionHandler.Extensions)
                                {
                                    try
                                    {
                                        if (curr.IsWorking)
                                        {
                                            curr.Hook_ChannelDrop(channel);
                                        }
                                    }
                                    catch (Exception fail)
                                    {
                                        Syslog.Log("MODULE: exception at Hook_ChannelDrop in " + curr.Name, true);
                                        Core.HandleException(fail, curr.Name);
                                    }
                                }
                            }
                        }
                        catch (Exception fail)
                        {
                            Core.HandleException(fail);
                        }
                        lock (Configuration.Channels)
                        {
                            channel.Remove();
                            Configuration.Channels.Remove(channel);
                        }
                        Configuration.Save();
                        return;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), origin);
                    return;
                }

                if (message == Configuration.System.CommandPrefix + "part")
                {
                    if (channel.SystemUsers.IsApproved(user, host, "admin"))
                    {
                        while (!Core.FinishedJoining)
                        {
                            Syslog.Log("Postponing request to part " + channel.Name + " because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        channel.PrimaryInstance.irc.SendData("PART " + channel.Name + " :" + "removed by " + user + " from " + origin);
                        Syslog.Log("Removed " + channel.Name + " removed by " + user + " from " + origin);
                        Thread.Sleep(100);
                        lock (Configuration.Channels)
                        {
                            channel.Remove();
                            Configuration.Channels.Remove(channel);
                        }
                        Configuration.Save();
                        return;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), origin);
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
        }
    }
}
