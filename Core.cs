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
    public class variables
    {
        /// <summary>
        /// Configuration directory
        /// </summary>
        public static readonly string config = "configuration";
        public static readonly string prefix_logdir = "log";
        public static string bold = ((char)002).ToString();
    }

    public class misc
    {
        /// <summary>
        /// Check if a regex is valid
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool IsValidRegex(string pattern)
        {
            if (pattern == null) return false;

            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }

    [Serializable()]
    public partial class core : MarshalByRefObject
    {
        public static AppDomain domain = null;
        public static string LastText = null;
        public static bool disabled = false;
        public static bool exit = false;
        public static IRC irc;
        public static Thread WriterThread = null;
        public static Dictionary<Module, AppDomain> Domains = new Dictionary<Module, AppDomain>();
        private static List<SystemUser> User = new List<SystemUser>();
        private static Dictionary<string, string> HelpData = new Dictionary<string, string>();

        [Serializable()]
        public class SystemUser
        {
            /// <summary>
            /// Regex
            /// </summary>
            public string name;
            /// <summary>
            /// Level
            /// </summary>
            public string level;
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="level"></param>
            /// <param name="name"></param>
            public SystemUser(string level, string name)
            {
                this.level = level;
                this.name = name;
            }
        }

        public static void TrafficLog(string text)
        {
            if (config.Logging)
            { 
                StorageWriter.InsertLine("trafficlog.dat", DateTime.Now.ToString() + ": " + text, false);
            }
        }

        /// <summary>
        /// Encode a data before saving it to a file
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns></returns>
        public static string encode2(string text)
        {
            return text.Replace("|", "<separator>");
        }

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="text">String</param>
        /// <returns></returns>
        public static string decode2(string text)
        {
            return text.Replace("<separator>", "|");
        }

        public static void Log(string text, bool error = false)
        {
            Program.Log(text, error);
        }

        /// <summary>
        /// Exceptions :o
        /// </summary>
        /// <param name="ex">Exception pointer</param>
        /// <param name="chan">Channel name</param>
        public static void handleException(Exception ex, string chan = "")
        {
            try
            {
                if (config.debugchan != null && config.debugchan != "")
                {
                    irc._SlowQueue.DeliverMessage("DEBUG Exception: " + ex.Message + " last input was " + LastText + " I feel crushed, uh :|", config.debugchan);
                }
                Program.Log("DEBUG Exception: " + ex.Message + ex.Source + ex.StackTrace, true);
            }
            catch (Exception) // exception happened while we tried to handle another one, ignore that (probably issue with logging)
            { }
        }

        /// <summary>
        /// Get a channel object
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns></returns>
        public static config.channel getChannel(string name)
        {
            lock (config.channels)
            {
                foreach (config.channel current in config.channels)
                {
                    if (current.Name.ToLower() == name.ToLower())
                    {
                        return current;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Called on action
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="Channel">Channel</param>
        /// <param name="host">Host</param>
        /// <param name="nick">Nick</param>
        /// <returns></returns>
        public static bool getAction(string message, string Channel, string host, string nick)
        {
            config.channel channel = getChannel(Channel);
            if (channel != null)
            {
                lock (Module.module)
                {
                    foreach (Module curr in Module.module)
                    {
                        if (!curr.working)
                        {
                            continue;
                        }
                        try
                        {
                            curr.Hook_ACTN(channel, new User(nick, host, ""), message);
                        }
                        catch (Exception fail)
                        {
                            Program.Log("Exception on Hook_ACTN in module: " + curr.Name);
                            core.handleException(fail);
                        }
                    }
                }
            }
            return false;
        }

        public static bool validFile(string name)
        {
            return !(name.Contains(" ") || name.Contains("?") || name.Contains("|") || name.Contains("/")
                || name.Contains("\\") || name.Contains(">") || name.Contains("<") || name.Contains("*"));
        }

        public static bool backupRecovery(string name, string ch = "unknown object")
        {
            if (File.Exists(config.tempName(name)))
            {
                string temp = System.IO.Path.GetTempFileName();
                File.Copy(config.tempName(name), temp, true);
                Program.Log("Unfinished transaction from " + name + "~ was stored as " + temp);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create a backup file
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool backupData(string name)
        {
            try
            {
                if (!File.Exists(name))
                {
                    return false;
                }
                if (File.Exists(config.tempName(name)))
                {
                    backupRecovery(name);
                }
                File.Copy(name, config.tempName(name), true);
            }
            catch (Exception b)
            {
                core.handleException(b);
            }
            return true;
        }

        public static bool recoverFile(string name, string ch = "unknown object")
        {
            try
            {
                if (File.Exists(config.tempName(name)))
                {
                    if (!Program.Temp(name))
                    {
                        Program.Log("Unfinished transaction could not be restored! DB of " + name + " is probably broken", true);
                        return false;
                    }
                    else
                    {
                        Program.Log("Restoring unfinished transaction of " + ch + " for db_" + name);
                        File.Copy(config.tempName(name), name, true);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception b)
            {
                core.handleException(b);
                Program.Log("Unfinished transaction could not be restored! DB of " + name + " is now broken");
                return false;
            }
        }

        /// <summary>
        /// 
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

        public static void Connect()
        {
            irc = new IRC(config.network, config.username, config.name, config.name);
            irc.Connect();
        }

        private static void Terminate()
        {
            try
            {
                List<Module> list = new List<Module>();
                lock (Module.module)
                {
                    list.AddRange(Module.module);
                }
                foreach (Module d in list)
                {
                    try
                    {
                        d.Exit();
                    }
                    catch (Exception fail)
                    {
                        core.handleException(fail);
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }

        public static void Kill()
        {
            try
            {
                irc.disabled = true;
                exit = true;
                irc.wd.Close();
                irc.rd.Close();
                irc._SlowQueue.Exit();
                StorageWriter.running = false;
                Thread modules = new Thread(Terminate);
                modules.Name = "KERNEL: Core helper shutdown thread";
                modules.Start();
                Program.Log("Giving grace time for all modules to finish ok");
                int kill = 0;
                while (kill < 20)
                {
                    kill++;
                    if (Module.module.Count == 0)
                    {
                        Program.Log("KERNEL: Modules are all down");
                        if (WriterThread.ThreadState == ThreadState.Running || WriterThread.ThreadState == ThreadState.WaitSleepJoin)
                        {
                            Log("KERNEL: Writer thread didn't shut down gracefully, waiting 2 seconds", true);
                            Thread.Sleep(2000);
                            if (WriterThread.ThreadState == ThreadState.Running || WriterThread.ThreadState == ThreadState.WaitSleepJoin)
                            {
                                Log("KERNEL: Writer thread didn't shut down gracefully, killing", true);
                                WriterThread.Abort();
                            }
                            else
                            {
                                Log("KERNEL: Writer thread is shut down", true);
                            }
                        }
                        else
                        {
                            Log("KERNEL: Writer thread is down ok");
                        }
                        Program.Log("KERNEL: Terminated");
                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);

            }
            Program.Log("There was problem shutting down " + Module.module.Count.ToString() + " modules, terminating process");
            Program.Log("Terminated");
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Called when someone post a message to server
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="nick">Nick</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        /// <returns></returns>
        public static bool getMessage(string channel, string nick, string host, string message)
        {
            LastText = nick + " chan: " + channel + " " + message;
            config.channel curr = getChannel(channel);
            if (curr != null)
            {
                if (curr.ignore_unknown)
                {
                    if (!curr.Users.isApproved(nick, host, "trust"))
                    {
                        return false;
                    }
                }
                if (message.StartsWith("@"))
                {
                    modifyRights(message, curr, nick, host);
                    addChannel(curr, nick, host, message);
                    partChannel(curr, nick, host, message);
                }
                admin(curr, nick, host, message);
                if (curr.respond_message)
                {
                    if (message.StartsWith(config.username + ":"))
                    {
                        System.DateTime time = curr.last_msg;
                        if (System.DateTime.Now >= time.AddSeconds(curr.respond_wait))
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("hi", curr.Language, new List<string> { nick }), curr.Name);
                            curr.last_msg = System.DateTime.Now;
                        }
                    }
                }
            }

            return false;
        }

        public static class Help
        {
            public static bool Register(string name, string text)
            {
                lock (HelpData)
                {
                    if (!HelpData.ContainsKey(name))
                    {
                        HelpData.Add(name, text);
                        return true;
                    }
                }
                return false;
            }

            public static void CreateHelp()
            {
                Register("infobot-ignore-", null);
                Register("infobot-ignore-", null);
                Register("infobot-ignore+", null);
                Register("trustdel", null);
                Register("refresh", null);
                Register("infobot-on", null);
                Register("seen-on", null);
                Register("seen", null);
                Register("infobot-off", null);
                Register("seen-off", null);
                Register("channellist", null);
                Register("trusted", null);
                Register("trustadd", null);
                Register("drop", null);
                Register("part", null);
                Register("language", null);
                Register("whoami", null);
                Register("suppress-on", null);
                Register("infobot-detail", null);
                Register("configure", null);
                Register("add", null);
                Register("reload", null);
                Register("logon", null);
                Register("logoff", null);
                Register("recentchanges-on", null);
                Register("recentchanges-off", null);
                Register("statistics-reset", null);
                Register("statistics-off", null);
                Register("statistics-on", null);
                Register("recentchanges-", null);
                Register("recentchanges+", null);
                Register("infobot-share-on", null);
                Register("infobot-share-trust+", null);
                Register("infobot-share-trust-", null);
                Register("infobot-link", null);
                Register("info", null);
                Register("rc-", null);
                Register("search", null);
                Register("commands", null);
                Register("regsearch", null);
                Register("infobot-share-off", null);
                Register("rc+", null);
                Register("suppress-off", null);
            }

            public static bool Unregister(string name)
            {
                lock (HelpData)
                {
                    if (HelpData.ContainsKey(name))
                    {
                        HelpData.Remove(name);
                        return true;

                    }
                }
                return false;
            }
        }

        public static class Host
        {
            public static string Host2Name(string host)
            {
                host = host.Replace("-", "_");
                host = host.Replace("pdpc.professional.", "");
                host = host.Replace("pdpc.active.", "");
                if (host.StartsWith("wikimedia/"))
                {
                    host = host.Substring("wikipedia/".Length);
                    return "https://meta.wikimedia.org/wiki/User:" + host;
                }
                else if (host.StartsWith("wikipedia/"))
                {
                    host = host.Substring("wikipedia/".Length);
                    return "https://en.wikipedia.org/wiki/User:" + host;
                }
                else if (host.StartsWith("mediawiki/"))
                {
                    host = host.Substring("wikipedia/".Length);
                    return "https://mediawiki.org/wiki/User:" + host;
                }
                return "";
            }
        }

        private static void showInfo(string name, string info, string channel)
        {
            irc._SlowQueue.DeliverMessage("Info for " + name + ": " + info, channel);
        }

        private static bool ShowHelp(string parameter, config.channel channel)
        {
            if (parameter.StartsWith("@"))
            {
                parameter = parameter.Substring(1);
            }
            lock (HelpData)
            {
                if (HelpData.ContainsKey(parameter.ToLower()))
                {
                    if (HelpData[parameter] == null)
                    {
                        showInfo(parameter, messages.get(parameter.ToLower(), channel.Language), channel.Name);
                        return true;
                    }
                    showInfo(parameter, HelpData[parameter], channel.Name);
                    return true;
                }
            }
            irc._SlowQueue.DeliverMessage("Unknown command type @commands for a list of all commands I know", channel.Name);
            return false;
        }

        public static string getUptime()
        {
            System.TimeSpan uptime = System.DateTime.Now - config.UpTime;
            return uptime.Days.ToString() + " days  " + uptime.Hours.ToString() + " hours since " + config.UpTime.ToString();
        }

        public static class HTML
        {
            /// <summary>
            /// Insert another table row
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public static string AddLink(string name, string value)
            {
                return "<tr><td>" + System.Web.HttpUtility.HtmlEncode(name) + "</td><td><a href=\"#" + System.Web.HttpUtility.HtmlEncode(value) + "\">" + System.Web.HttpUtility.HtmlEncode(value) + "</a></td></tr>\n";
            }

            /// <summary>
            /// Insert another table row
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public static string AddKey(string name, string value)
            {
                return "<tr id=\"" + System.Web.HttpUtility.HtmlEncode(name) + "\"><td>" + System.Web.HttpUtility.HtmlEncode(name) + "</td><td>" + System.Web.HttpUtility.HtmlEncode(value) + "</td></tr>\n";
            }
        }
    }
}
