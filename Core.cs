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
    }
    public class misc
    {
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

    public class core
    {
        public static Thread dumphtmt;
        public static Thread rc;
        public static string LastText;
        public static Thread ib;
        public static bool disabled;
        public static IRC irc;
        private static List<user> User = new List<user>();

        public class user
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
            public user(string level, string name)
            {
                this.level = level;
                this.name = name;
            }
        }

        public class RegexCheck
        {
            public string value;
            public string regex;
            public bool searching;
            public bool result = false;
            public RegexCheck(string Regex, string Data)
            {
                result = false;
                value = Data;
                regex = Regex;
            }
            private void Run()
            {
                Regex c = new Regex(regex);
                result = c.Match(value).Success;
                searching = false;
            }
            public int IsMatch()
            {
                Thread quick = new Thread(Run);
                searching = true;
                quick.Start();
                int check = 0;
                while (searching)
                {
                    check++;
                    Thread.Sleep(10);
                    if (check > 50)
                    {
                        quick.Abort();
                        return 2;
                    }
                }
                if (result)
                {
                    return 1;
                }
                return 0;
            }
        }

        /// <summary>
        /// Encode a data before saving it to a file
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns></returns>
        public static string encode(string text)
        {
            return text.Replace(config.separator, "<separator>");
        }

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="text">String</param>
        /// <returns></returns>
        public static string decode(string text)
        {
            return text.Replace("<separator>", config.separator);
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
                if (config.debugchan != null)
                {
                    irc._SlowQueue.DeliverMessage("DEBUG Exception: " + ex.Message + " last input was " + LastText + " I feel crushed, uh :|", config.debugchan);
                }
                Program.Log("DEBUG Exception: " + ex.Message + ex.Source + ex.StackTrace);
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
            foreach (config.channel current in config.channels)
            {
                if (current.Name.ToLower() == name.ToLower())
                {
                    return current;
                }
            }
            return null;
        }

        /// <summary>
        /// Change rights of user
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="channel">Channel</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <returns></returns>
        public static int modifyRights(string message, config.channel channel, string user, string host)
        {
            try
            {
                if (message.StartsWith("@trustadd"))
                {
                    string[] rights_info = message.Split(' ');
                    if (channel.Users.isApproved(user, host, "trustadd"))
                    {
                        if (rights_info.Length < 3)
                        {
                            irc.Message(messages.get("Trust1", channel.Language), channel.Name);
                            return 0;
                        }
                        if (!(rights_info[2] == "admin" || rights_info[2] == "trusted"))
                        {
                            irc.Message(messages.get("Unknown1", channel.Language), channel.Name);
                            return 2;
                        }
                        if (rights_info[2] == "admin")
                        {
                            if (!channel.Users.isApproved(user, host, "admin"))
                            {
                                irc.Message(messages.get("PermissionDenied", channel.Language), channel.Name);
                                return 2;
                            }
                        }
                        if (channel.Users.addUser(rights_info[2], rights_info[1]))
                        {
                            irc.Message(messages.get("UserSc", channel.Language) + rights_info[1], channel.Name);
                            return 0;
                        }
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Authorization", channel.Language), channel.Name);
                        return 0;
                    }
                }
                if (message.StartsWith("@trusted"))
                {
                    channel.Users.listAll();
                    return 0;
                }
                if (message.StartsWith("@trustdel"))
                {
                    string[] rights_info = message.Split(' ');
                    if (rights_info.Length > 1)
                    {
                        string x = rights_info[1];
                        if (channel.Users.isApproved(user, host, "trustdel"))
                        {
                            channel.Users.delUser(channel.Users.getUser(user + "!@" + host), rights_info[1]);
                            return 0;
                        }
                        else
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("Authorization", channel.Language), channel.Name);
                            return 0;
                        }
                    }
                    irc.Message(messages.get("InvalidUser", channel.Language), channel.Name);
                }
            }
            catch (Exception b)
            {
                handleException(b);
            }
            return 0;
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
            config.channel curr = getChannel(Channel);
            Logs.chanLog(message, curr, nick, host, false);
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
                Program.Log("Unfinished transaction from ~" + name + " was stored as " + temp);
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
            if (File.Exists(config.tempName(name)))
            {
                backupRecovery(name);
            }
            File.Copy(name, config.tempName(name), true);
            return true;
        }

        public static bool recoverFile(string name, string ch = "unknown object")
        {
            if (File.Exists(config.tempName(name)))
            {
                if (!Program.Temp(name))
                {
                    Program.Log("Unfinished transaction could not be restored! DB of " + name + " is probably broken");
                    return false;
                }
                else
                {
                    Program.Log( "Restoring unfinished transaction of " + ch + " for db_" + name );
                    File.Copy(config.tempName(name), name, true);
                    return true;
                }
            }
            return false;
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
                            foreach (config.channel cu in config.channels)
                            {
                                if (channel == cu.Name)
                                {
                                    irc._SlowQueue.DeliverMessage(messages.get("ChannelIn", chan.Language), chan.Name);
                                    return;
                                }
                            }
                            bool existing = config.channel.channelExist(channel);
                            config.channels.Add(new config.channel(channel));
                            config.Save();
                            irc.wd.WriteLine("JOIN " + channel);
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
        public static void partChannel(config.channel chan, string user, string host, string message)
        {
            try
            {
                if (message == "@drop")
                {
                    if (chan.Users.isApproved(user, host, "admin"))
                    {
                        irc.wd.WriteLine("PART " + chan.Name);
                        Thread.Sleep(100);
                        chan.Feed = false;
                        irc.wd.Flush();
                        try
                        {
                            if (Directory.Exists(chan.Log))
                            {
                                Directory.Delete(chan.Log, true);
                            }
                        }
                        catch (Exception)
                        { }
                        try
                        {
                            File.Delete(variables.config + "/" + chan.Name + ".setting");
                            File.Delete(chan.Users.File);
                            if (File.Exists(variables.config + "/" + chan.Name + ".list"))
                            {
                                File.Delete(variables.config + "/" + chan.Name + ".list");
                            }
                        }
                        catch (Exception) { }
                        config.channels.Remove(chan);
                        config.Save();
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name);
                    return;
                }
                if (message == "@part")
                {
                    if (chan.Users.isApproved(user, host, "admin"))
                    {
                        irc.wd.WriteLine("PART " + chan.Name);
                        chan.Feed = false;
                        Thread.Sleep(100);
                        irc.wd.Flush();
                        config.channels.Remove(chan);
                        config.Save();
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name);
                    return;
                }
            }
            catch (Exception x)
            {
                handleException(x);
            }
        }

        /// <summary>
        /// Display admin command
        /// </summary>
        /// <param name="chan">Channel</param>
        /// <param name="user">User name</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        public static void admin(config.channel chan, string user, string host, string message)
        {
            User invoker = new User(user, host);
            if (message == "@reload")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    chan.LoadConfig();
                    chan.Keys = new infobot_core(chan.keydb, chan.Name);
                    irc._SlowQueue.DeliverMessage(messages.get("Config", chan.Language), chan.Name);
                    return;
                }
                irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name);
                return;
            }
            if (message == "@refresh")
            {
                if (chan.Users.isApproved(invoker.nick, host, "flushcache"))
                {
                    irc._Queue.Abort();
                    irc._SlowQueue.newmessages.Clear();
                    irc._Queue = new System.Threading.Thread(new System.Threading.ThreadStart(irc._SlowQueue.Run));
                    irc._SlowQueue.messages.Clear();
                    irc._Queue.Start();
                    irc.Message(messages.get("MessageQueueWasReloaded", chan.Language), chan.Name);
                    return;
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == ("@info"))
            {
                irc._SlowQueue.DeliverMessage(config.url + config.DumpDir + "/" + System.Web.HttpUtility.UrlEncode(chan.Name) + ".htm", chan.Name);
                return;
            }

            if (message == "@recentchanges-on")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "recentchanges-manage"))
                {
                    if (chan.Feed)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Feed1", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Feed2", chan.Language), chan.Name);
                        chan.Feed = true;
                        chan.SaveConfig();
                        config.Save();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message.StartsWith("@recentchanges+"))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "recentchanges-manage"))
                {
                    if (chan.Feed)
                    {
                        if (!message.Contains(" "))
                        {
							if (!chan.suppress_warnings)
							{
                            	irc._SlowQueue.DeliverMessage(messages.get("InvalidWiki", chan.Language), chan.Name);
							}
                            return;
                        }
                        string channel = message.Substring(message.IndexOf(" ") + 1);
                        if (RecentChanges.InsertChannel(chan, channel))
                        {
                            irc.Message(messages.get("Wiki+", chan.Language), chan.Name);
                        }
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Feed3", chan.Language), chan.Name);
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message.StartsWith("@recentchanges- "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.Feed)
                    {
                        if (!message.Contains(" "))
                        {
							if (!chan.suppress_warnings)
							{
                            	irc._SlowQueue.DeliverMessage(messages.get("InvalidWiki", chan.Language), chan.Name);
							}
                            return;
                        }
                        string channel = message.Substring(message.IndexOf(" ") + 1);
                        if (RecentChanges.DeleteChannel(chan, channel))
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("Wiki-", chan.Language), chan.Name, IRC.priority.high);
                        }
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Feed3", chan.Language), chan.Name);
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                		irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message.StartsWith("@RC+ "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "trust"))
                {
                    if (chan.Feed)
                    {
                        string[] a = message.Split(' ');
                        if (a.Length < 3)
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("Feed4", chan.Language) + user + messages.get("Feed5", chan.Language), chan.Name);
                            return;
                        }
                        string wiki = a[1];
                        string Page = a[2];
                        chan.RC.insertString(wiki, Page);
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Feed3", chan.Language), chan.Name);
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message.StartsWith("@language"))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    string parameter = "";
                    if (message.Contains(" "))
                    {
                        parameter = message.Substring(message.IndexOf(" ") + 1).ToLower();
                    }
                    if (parameter != "")
                    {
                        if (messages.exist(parameter))
                        {
                            chan.Language = parameter;
                            irc._SlowQueue.DeliverMessage(messages.get("Language", chan.Language), chan.Name);
                            chan.SaveConfig();
                            return;
                        }
						if (!chan.suppress_warnings)
						{
                        	irc._SlowQueue.DeliverMessage(messages.get("InvalidCode", chan.Language), chan.Name);
						}
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("LanguageInfo", chan.Language), chan.Name);
                        return;
                    }
                }
                else
                {
					if (!chan.suppress_warnings)
					{
                    	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
					}
                    return;
                }
            }

            if (message.StartsWith("@help"))
            {
                string parameter = "";
                if (message.Contains(" "))
                {
                    parameter = message.Substring(message.IndexOf(" ") + 1);
                }
                if (parameter != "")
                {
                    ShowHelp(parameter, chan);
                    return;
                }
                else
                {
                    irc._SlowQueue.DeliverMessage("Type @commands for list of commands. This bot is running http://meta.wikimedia.org/wiki/WM-Bot version " + config.version + " source code licensed under GPL and located at https://github.com/benapetr/wikimedia-bot", chan.Name);
                    return;
                }
            }

            if (message.StartsWith("@RC-"))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "trust"))
                {
                    if (chan.Feed)
                    {
                        string[] a = message.Split(' ');
                        if (a.Length < 3)
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("Feed8", chan.Language, new List<string> { user }), chan.Name);
                            return;
                        }
                        string wiki = a[1];
                        string Page = a[2];
                        chan.RC.removeString(wiki, Page);
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Feed3", chan.Language), chan.Name);
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@suppress-off")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (!chan.suppress)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Silence1", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        chan.suppress = false;
                        irc._SlowQueue.DeliverMessage(messages.get("Silence2", chan.Language), chan.Name);
                        chan.SaveConfig();
                        config.Save();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@suppress-on")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.suppress)
                    {
                        //Message("Channel had already quiet mode disabled", chan.name);
                        return;
                    }
                    else
                    {
                        irc.Message(messages.get("SilenceBegin", chan.Language), chan.Name);
                        chan.suppress = true;
                        chan.SaveConfig();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@recentchanges-off")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (!chan.Feed)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Feed6", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Feed7", chan.Language), chan.Name);
                        chan.Feed = false;
                        chan.SaveConfig();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@logon")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.Logged)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("ChannelLogged", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("LoggingOn", chan.Language), chan.Name);
                        chan.Logged = true;
                        chan.SaveConfig();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@whoami")
            {
                user current = chan.Users.getUser(user + "!@" + host);
                if (current.level == "null")
                {
                    irc._SlowQueue.DeliverMessage(messages.get("Unknown", chan.Language), chan.Name);
                    return;
                }
                irc._SlowQueue.DeliverMessage(messages.get("usr1", chan.Language, new List<string> { current.level, current.name }), chan.Name);
                return;
            }

            if (message == "@logoff")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (!chan.Logged)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("LogsE1", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        chan.Logged = false;
                        chan.SaveConfig();
                        irc._SlowQueue.DeliverMessage(messages.get("NotLogged", chan.Language), chan.Name);
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@channellist")
            {
                string channels = "";
                foreach (config.channel a in config.channels)
                {
                    channels = channels + a.Name + ", ";
                }
                irc._SlowQueue.DeliverMessage(messages.get("List", chan.Language) + channels, chan.Name);
                return;
            }

            if (message == "@infobot-off")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (!chan.Info)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot1", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot2", chan.Language), chan.Name, IRC.priority.high);
                        chan.Info = false;
                        chan.SaveConfig();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@infobot-on")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.Info)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot3", chan.Language), chan.Name);
                        return;
                    }
                    chan.Info = true;
                    chan.SaveConfig();
                    irc._SlowQueue.DeliverMessage(messages.get("infobot4", chan.Language), chan.Name, IRC.priority.high);
                    return;
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@infobot-share-on")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.shared == "local")
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot11", chan.Language), chan.Name, IRC.priority.high);
                        return;
                    }
                    if (chan.shared != "local" && chan.shared != "")
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot15", chan.Language), chan.Name, IRC.priority.high);
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot12", chan.Language), chan.Name);
                        chan.shared = "local";
                        chan.SaveConfig();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message.StartsWith("@configure "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    string text = message.Substring("@configure ".Length);
                    if (text == "")
                    {
                        return;
                    }
                    if (text.Contains("=") && !text.EndsWith("="))
                    {
                        string name = text.Substring(0, text.IndexOf("="));
                        string value = text.Substring(text.IndexOf("=") + 1);
                        bool _temp_a;
                        switch (name)
                        {
                            case "ignore-unknown":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.ignore_unknown = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
                            case "infobot-trim-white-space-in-name":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.infobot_trim_white_space_in_name = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
                            case "infobot-auto-complete":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.infobot_auto_complete = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
                            case "infobot-sorted":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.infobot_sorted = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
                            case "logs-no-write-data":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.logs_no_write_data = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
                            case "respond-wait":
                                int _temp_b;
                                if (int.TryParse(value, out _temp_b))
                                {
                                    if (_temp_b > 1 && _temp_b < 364000)
                                    {
                                        chan.respond_wait = _temp_b;
                                        irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                        chan.SaveConfig();
                                        return;
                                    }
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
                            case "respond-message":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.respond_message = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
							case "suppress-warnings":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.suppress_warnings = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
                            case "infobot-help":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.infobot_help = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan.Name);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan.Name);
                                return;
                        }
						if (!chan.suppress_warnings)
						{
                        	irc._SlowQueue.DeliverMessage(messages.get("configure-wrong", chan.Language), chan.Name);
						}
                        return;
                    }
					if (!chan.suppress_warnings)
					{
                    	irc._SlowQueue.DeliverMessage(messages.get("configure-wrong", chan.Language), chan.Name);
					}
                    return;
                }
                else
                {
					if (!chan.suppress_warnings)
					{
                    	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
					}
                    return;
                }
            }

            if (message.StartsWith("@infobot-share-trust+ "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.shared != "local")
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot16", chan.Language), chan.Name);
                        return;
                    }
                    if (chan.shared != "local" && chan.shared != "")
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot15", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        if (message.Length <= "@infobot-share-trust+ ".Length)
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("db6", chan.Language), chan.Name);
                            return;
                        }
                        string name = message.Substring("@infobot-share-trust+ ".Length);
                        config.channel guest = core.getChannel(name);
                        if (guest == null)
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("db8", chan.Language), chan.Name);
                            return;
                        }
                        if (chan.sharedlink.Contains(guest))
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("db14", chan.Language), chan.Name);
                            return;
                        }
                        irc._SlowQueue.DeliverMessage(messages.get("db1", chan.Language, new List<string> { name }), chan.Name);
                        chan.sharedlink.Add(guest);
                        chan.SaveConfig();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message.StartsWith("@infobot-ignore- "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "trust"))
                {
                    string item = message.Substring("@infobot-ignore+ ".Length);
                    if (item != "")
                    {
                        if (!chan.Infobot_IgnoredNames.Contains(item))
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-found", chan.Language, new List<string> { item }), chan.Name);
                            return;
                        }
                        chan.Infobot_IgnoredNames.Remove(item);
                        irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-rm", chan.Language, new List<string> { item }), chan.Name);
                        chan.SaveConfig();
                        return;
                    }
                }
                else
                {
                    if (!chan.suppress_warnings)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
                    }
                }
            }

            if (message.StartsWith("@infobot-ignore+ "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "trust"))
                {
                    string item = message.Substring("@infobot-ignore+ ".Length);
                    if (item != "")
                    {
                        if (chan.Infobot_IgnoredNames.Contains(item))
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-exist", chan.Language, new List<string> { item }), chan.Name);
                            return;
                        }
                        chan.Infobot_IgnoredNames.Add(item);
                        irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-ok", chan.Language, new List<string> { item }), chan.Name);
                        chan.SaveConfig();
                        return;
                    }
                }
                else
                {
                    if (!chan.suppress_warnings)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
                    }
                }
            }

            if (message.StartsWith("@join "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "reconnect"))
                {
                    config.channel channel = core.getChannel(message.Substring("@join ".Length));
                    irc.Join(channel);
                }
            }

            if (message.StartsWith("@infobot-share-trust- "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.shared != "local")
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot16", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        if (message.Length <= "@infobot-share-trust+ ".Length)
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("db6", chan.Language), chan.Name);
                            return;
                        }
                        string name = message.Substring("@infobot-share-trust- ".Length);
                        config.channel target = core.getChannel(name);
                        if (target == null)
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("db8", chan.Language), chan.Name);
                            return;
                        }
                        if (chan.sharedlink.Contains(target))
                        {
                            chan.sharedlink.Remove(target);
                            irc._SlowQueue.DeliverMessage(messages.get("db2", chan.Language, new List<string> { name }), chan.Name);
                            chan.SaveConfig();
                            return;
                        }
                        irc._SlowQueue.DeliverMessage(messages.get("db4", chan.Language), chan.Name);
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message.StartsWith("@infobot-link "))
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.shared == "local")
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot17", chan.Language), chan.Name);
                        return;
                    }
                    if (chan.shared != "")
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot18", chan.Language, new List<string> { chan.shared }), chan.Name);
                        return;
                    }
                    if ((message.Length - 1) < "@infobot-link ".Length)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("db6", chan.Language), chan.Name);
                        return;
                    }
                    string name = message.Substring("@infobot-link ".Length);
                    config.channel db = core.getChannel(name);
                    if (db == null)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("db8", chan.Language), chan.Name);
                        return;
                    }
                    if (!infobot_core.Linkable(db, chan))
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("db9", chan.Language), chan.Name);
                        return;
                    }
                    chan.shared = name.ToLower();
                    irc._SlowQueue.DeliverMessage(messages.get("db10", chan.Language), chan.Name);
                    chan.SaveConfig();
                    return;
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@infobot-share-off")
            {
                if (chan.Users.isApproved(invoker.nick, invoker.host, "admin"))
                {
                    if (chan.shared == "")
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot14", chan.Language), chan.Name);
                        return;
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("infobot13", chan.Language), chan.Name);
                        foreach (config.channel curr in config.channels)
                        {
                            if (curr.shared == chan.Name.ToLower())
                            {
                                curr.shared = "";
                                curr.SaveConfig();
                                irc._SlowQueue.DeliverMessage(messages.get("infobot19", curr.Language, new List<string> { user }), curr.Name);
                            }
                        }
                        chan.shared = "";
                        chan.SaveConfig();
                        return;
                    }
                }
				if (!chan.suppress_warnings)
				{
                	irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
				}
                return;
            }

            if (message == "@commands")
            {
                irc._SlowQueue.DeliverMessage("Commands: channellist, trusted, trustadd, trustdel, info, configure, infobot-link, infobot-share-trust+, infobot-share-trust-, infobot-share-off, infobot-share-on, infobot-off, refresh, infobot-on, drop, whoami, add, reload, suppress-off, suppress-on, help, RC-, recentchanges-on, language, infobot-ignore+, infobot-ignore-, recentchanges-off, logon, logoff, recentchanges-, recentchanges+, RC+", chan.Name);
                return;
            }
        }

        public static void Connect()
        {
            dumphtmt = new Thread(HtmlDump.Start);
            dumphtmt.Start();
            Program.Log("Loading RC module");
            rc = new Thread(RecentChanges.Start);
            rc.Start();
            Program.Log("Loading infobot module");
            ib = new Thread(infobot_core.Initialise);
            ib.Start();
            Program.Log("Modules loaded");
            irc = new IRC(config.network, config.username, config.name, config.name);
            irc.Connect();
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
                Logs.chanLog(message, curr, nick, host);
                if (curr.ignore_unknown)
                {
                    if (!curr.Users.isApproved(nick, host, "trust"))
                    {
                        return false;
                    }
                }
                 // "\uff01" is the full-width version of "!".
                if ((message.StartsWith("!") || message.StartsWith("\uff01")) && curr.Info)
                {
                    while (infobot_core.Unwritable)
                    {
                        Thread.Sleep(10);
                    }
                    infobot_core.Unwritable = true;
                    infobot_core.InfoItem item = new infobot_core.InfoItem();
                    item.Channel = curr;
                    item.Name = "!" + message.Substring(1); // Normalizing "!".
                    item.User = nick;
                    item.Host = host;
                    infobot_core.jobs.Add(item);
                    infobot_core.Unwritable = false;
                }
                if (message.StartsWith("@"))
                {
                    if (curr.Info)
                    {
                        curr.Keys.Find(message, curr);
                        curr.Keys.RSearch(message, curr);
                    }
                    modifyRights(message, curr, nick, host);
                    addChannel(curr, nick, host, message);
                    admin(curr, nick, host, message);
                    partChannel(curr, nick, host, message);
                }
                if (curr.respond_message)
                {
                    if (message.StartsWith(config.username + ":"))
                    {
                        if (System.DateTime.Now >= curr.last_msg.AddSeconds(curr.respond_wait))
                        {
                            irc._SlowQueue.DeliverMessage( messages.get("hi", curr.Language, new List<string> { nick }), curr.Name );
                            curr.last_msg = System.DateTime.Now;
                        }
                    }
                }
            }

            return false;
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
            switch (parameter.ToLower())
            {
                case "infobot-ignore-":
                case "infobot-ignore+":
                case "trustdel":
                case "refresh":
                case "infobot-on":
                case "infobot-off":
                case "channellist":
                case "trusted":
                case "trustadd":
                case "drop":
                case "part":
                case "language":
                case "whoami":
                case "suppress-on":
                case "configure":
                case "add":
                case "reload":
                case "logon":
                case "logoff":
                case "recentchanges-on":
                case "recentchanges-off":
                case "recentchanges-":
                case "recentchanges+":
                case "infobot-share-on":
                case "infobot-share-trust+":
                case "infobot-share-trust-":
                case "infobot-link":
                case "info":
                case "rc-":
                case "infobot-share-off":
                case "rc+":
                case "suppress-off":
                    showInfo(parameter, messages.get(parameter.ToLower(), channel.Language), channel.Name);
                    return false;
            }
            irc._SlowQueue.DeliverMessage("Unknown command type @commands for a list of all commands I know", channel.Name);
            return false;
        }

        public static string getUptime()
        {
            System.TimeSpan uptime = System.DateTime.Now - config.UpTime;
            return uptime.Days.ToString() + " days  " + uptime.Hours.ToString() + " hours since " + config.UpTime.ToString();
        }
    }
}
