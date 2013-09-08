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
                if (message.StartsWith(config.CommandPrefix + "add"))
                {
                    if (chan.Users.IsApproved(user, host, "admin"))
                    {
                        while (!core.FinishedJoining)
                        {
                            core.Log("Postponing request to join because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        if (message.Contains(" "))
                        {
                            string channel = message.Substring(message.IndexOf(" ") + 1);
                            if (!validFile(channel) || (channel.Contains("#") == false))
                            {
                                irc._SlowQueue.DeliverMessage(messages.get("InvalidName", chan.Language), chan);
                                return;
                            }
                            lock (config.channels)
                            {
                                foreach (config.channel cu in config.channels)
                                {
                                    if (channel == cu.Name)
                                    {
                                        irc._SlowQueue.DeliverMessage(messages.get("ChannelIn", chan.Language), chan);
                                        return;
                                    }
                                }
                            }
                            bool existing = config.channel.channelExist(channel);
                            config.channel xx = new config.channel(channel);
                            lock (config.channels)
                            {
                                config.channels.Add(xx);
                            }
                            config.Save();
                            xx.instance.irc.SendData("JOIN " + channel);
                            Thread.Sleep(100);
                            config.channel Chan = getChannel(channel);
                            if (!existing)
                            {
                                Chan.Users.addUser("admin", IRCTrust.normalize(user) + "!.*@" + IRCTrust.normalize(host));
                            }
                            return;
                        }
                        chan.instance.irc.Message(messages.get("InvalidName", chan.Language), chan.Name);
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan);
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
        /// <param name="origin"></param>
        public static void partChannel(config.channel chan, string user, string host, string message, string origin = "NULL")
        {
            try
            {
                if (origin == "NULL")
                {
                    origin = chan.Name;
                }
                if (chan.Name == config.DebugChannel)
                {
                    chan.instance.irc._SlowQueue.DeliverMessage("Covardly refusing to part this channel, because I love it :3", chan);
                    return;
                }
                if (message == config.CommandPrefix + "drop")
                {
                    if (chan.Users.IsApproved(user, host, "admin"))
                    {
                        while (!core.FinishedJoining)
                        {
                            core.Log("Postponing request to part " + chan.Name + " because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        chan.instance.irc.SendData("PART " + chan.Name + " :" + "dropped by " + user + " from " + origin);
                        Program.Log("Dropped " + chan.Name + " dropped by " + user + " from " + origin);
                        Thread.Sleep(100);
                        try
                        {
                            // let's try to remove channel logs
                            string logdir = Module.GetConfig(chan, "Logs.Path", "null");
                            if (logdir == "null")
                            {
                                logdir = chan.LogDir;
                            }
                            if (Directory.Exists(logdir))
                            {
                                Directory.Delete(logdir, true);
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
                            chan.Remove();
                            config.channels.Remove(chan);
                        }
                        config.Save();
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), origin);
                    return;
                }
                if (message == config.CommandPrefix + "part")
                {
                    if (chan.Users.IsApproved(user, host, "admin"))
                    {
                        while (!core.FinishedJoining)
                        {
                            core.Log("Postponing request to part " + chan.Name + " because bot is still loading", true);
                            Thread.Sleep(2000);
                        }
                        chan.instance.irc.SendData("PART " + chan.Name + " :" + "removed by " + user + " from " + origin);
                        Program.Log("Removed " + chan.Name + " removed by " + user + " from " + origin);
                        Thread.Sleep(100);
                        config.channels.Remove(chan);
                        config.Save();
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), origin);
                }
            }
            catch (Exception x)
            {
                handleException(x);
            }
        }
    }
}
