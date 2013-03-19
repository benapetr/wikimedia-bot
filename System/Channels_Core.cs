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
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace wmib
{
    public partial class core
    {
        /// <summary>
        /// Join channel
        /// </summary>
        /// <param name="chan">Channel</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        public static void addChannel(config.channel chan, string user, string host, string message)
        {
            try
            {
                if (message.StartsWith("@add"))
                {
                    if (chan.Users.isApproved(user, host, "admin"))
                    {
                        if (message.Contains(" "))
                        {
                            string channel = message.Substring(message.IndexOf(" ") + 1);
                            if (!validFile(channel) || (channel.Contains("#") == false))
                            {
                                irc._SlowQueue.DeliverMessage(messages.get("InvalidName", chan.Language), chan.Name);
                                return;
                            }
                            lock (config.channels)
                            {
                                foreach (config.channel cu in config.channels)
                                {
                                    if (channel == cu.Name)
                                    {
                                        irc._SlowQueue.DeliverMessage(messages.get("ChannelIn", chan.Language), chan.Name);
                                        return;
                                    }
                                }
                            }
                            bool existing = config.channel.channelExist(channel);
                            lock (config.channels)
                            {
                                config.channels.Add(new config.channel(channel));
                            }
                            config.Save();
                            irc.SendData("JOIN " + channel);
                            irc.wd.Flush();
                            Thread.Sleep(100);
                            config.channel Chan = getChannel(channel);
                            if (!existing)
                            {
                                Chan.Users.addUser("admin", IRCTrust.normalize(user) + "!.*@" + IRCTrust.normalize(host));
                            }
                            return;
                        }
                        irc.Message(messages.get("InvalidName", chan.Language), chan.Name);
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name);
                    return;
                }
            }
            catch (Exception b)
            {
                handleException(b);
            }
        }

        /// <summary>
        /// Part a channel
        /// </summary>
        /// <param name="chan">Channel object</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        public static void partChannel(config.channel chan, string user, string host, string message, string origin = "NULL")
        {
            try
            {
                if (origin == "NULL")
                {
                    origin = chan.Name;
                }
                if (message == "@drop")
                {
                    if (chan.Users.isApproved(user, host, "admin"))
                    {
                        irc.SendData("PART " + chan.Name + " :" + "dropped by " + user + " from " + origin);
                        Program.Log("Dropped " + chan.Name + " dropped by " + user + " from " + origin);
                        Thread.Sleep(100);
                        irc.wd.Flush();
                        try
                        {
                            if (Directory.Exists(chan.LogDir))
                            {
                                Directory.Delete(chan.LogDir, true);
                            }
                        }
                        catch (Exception fail)
                        {
                            Log(fail.ToString(), true);
                        }
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
                                        core.Log("MODULE: exception at Hook_ChannelDrop in " + curr.Name, true);
                                        core.handleException(fail);
                                    }
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            Log(error.ToString(), true);
                        }
                        lock (config.channels)
                        {
                            config.channels.Remove(chan);
                        }
                        config.Save();
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), origin);
                    return;
                }
                if (message == "@part")
                {
                    if (chan.Users.isApproved(user, host, "admin"))
                    {
                        irc.SendData("PART " + chan.Name + " :" + "removed by " + user + " from " + origin);
                        Program.Log("Removed " + chan.Name + " removed by " + user + " from " + origin);
                        Thread.Sleep(100);
                        irc.wd.Flush();
                        config.channels.Remove(chan);
                        config.Save();
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), origin);
                    return;
                }
            }
            catch (Exception x)
            {
                handleException(x);
            }
        }
    }
}
