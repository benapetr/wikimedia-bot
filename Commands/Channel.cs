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
        /// <param name="chan">Channel</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        public static void AddChannel(Channel chan, string user, string host, string message)
        {
            try
            {
                if (message.StartsWith(Configuration.System.CommandPrefix + "add"))
                {
                    if (chan.Users.IsApproved(user, host, "admin"))
                    {
                        while (!Core.FinishedJoining)
                        {
                            Syslog.Log("Postponing request to join because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        if (message.Contains(" "))
                        {
                            string channel = message.Substring(message.IndexOf(" ") + 1);
                            if (!Core.validFile(channel) || (channel.Contains("#") == false))
                            {
                                Core.irc._SlowQueue.DeliverMessage(messages.get("InvalidName", chan.Language), chan);
                                return;
                            }
                            lock (Configuration.Channels)
                            {
                                foreach (Channel cu in Configuration.Channels)
                                {
                                    if (channel == cu.Name)
                                    {
                                        Core.irc._SlowQueue.DeliverMessage(messages.get("ChannelIn", chan.Language), chan);
                                        return;
                                    }
                                }
                            }
                            bool existing = Channel.Exists(channel);
                            Channel xx = new Channel(channel);
                            lock (Configuration.Channels)
                            {
                                Configuration.Channels.Add(xx);
                            }
                            Configuration.Save();
                            xx.instance.irc.SendData("JOIN " + channel);
                            Thread.Sleep(100);
                            Channel Chan = Core.GetChannel(channel);
                            if (!existing)
                            {
                                Chan.Users.AddUser("admin", IRCTrust.normalize(user) + "!.*@" + IRCTrust.normalize(host));
                            }
                            return;
                        }
                        chan.instance.irc.Message(messages.get("InvalidName", chan.Language), chan.Name);
                        return;
                    }
                    Core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan);
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
        /// <param name="chan">Channel object</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        /// <param name="origin"></param>
        public static void PartChannel(Channel chan, string user, string host, string message, string origin = "NULL")
        {
            try
            {
                if (origin == "NULL")
                {
                    origin = chan.Name;
                }
                if (chan.Name == Configuration.System.DebugChan && (message == Configuration.System.CommandPrefix + "part" 
				                                          || message == Configuration.System.CommandPrefix + "drop"))
                {
                    chan.instance.irc._SlowQueue.DeliverMessage("Cowardly refusing to part this channel, because I love it :3", chan);
                    return;
                }
                if (message == Configuration.System.CommandPrefix + "drop")
                {
                    if (chan.Users.IsApproved(user, host, "admin"))
                    {
                        while (!Core.FinishedJoining)
                        {
                            Syslog.Log("Postponing request to part " + chan.Name + " because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        chan.instance.irc.SendData("PART " + chan.Name + " :" + "dropped by " + user + " from " + origin);
                        Syslog.Log("Dropped " + chan.Name + " dropped by " + user + " from " + origin);
                        Thread.Sleep(100);
                        try
                        {
                            File.Delete(variables.config + Path.DirectorySeparatorChar + chan.Name + ".setting");
                            File.Delete(chan.Users.File);
                            if (File.Exists(variables.config + Path.DirectorySeparatorChar + chan.Name + ".list"))
                            {
                                File.Delete(variables.config + Path.DirectorySeparatorChar + chan.Name + ".list");
                            }
                            if (File.Exists(variables.config + Path.DirectorySeparatorChar + chan.Name + ".statistics"))
                            {
                                File.Delete(variables.config + Path.DirectorySeparatorChar + chan.Name + ".statistics");
                            }
                            lock (Module.module)
                            {
                                foreach (Module curr in Module.module)
                                {
                                    try
                                    {
                                        if (curr.working)
                                        {
                                            curr.Hook_ChannelDrop(chan);
                                        }
                                    }
                                    catch (Exception fail)
                                    {
                                        Syslog.Log("MODULE: exception at Hook_ChannelDrop in " + curr.Name, true);
                                        Core.HandleException(fail);
                                    }
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            Syslog.Log(error.ToString(), true);
                        }
                        lock (Configuration.Channels)
                        {
                            chan.Remove();
                            Configuration.Channels.Remove(chan);
                        }
                        Configuration.Save();
                        return;
                    }
                    Core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), origin);
                    return;
                }

                if (message == Configuration.System.CommandPrefix + "part")
                {
                    if (chan.Users.IsApproved(user, host, "admin"))
                    {
                        while (!Core.FinishedJoining)
                        {
                            Syslog.Log("Postponing request to part " + chan.Name + " because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        chan.instance.irc.SendData("PART " + chan.Name + " :" + "removed by " + user + " from " + origin);
                        Syslog.Log("Removed " + chan.Name + " removed by " + user + " from " + origin);
                        Thread.Sleep(100);
                        Configuration.Channels.Remove(chan);
                        Configuration.Save();
                        return;
                    }
                    Core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), origin);
                }
            }
            catch (Exception x)
            {
                Core.HandleException(x);
            }
        }
    }
}
