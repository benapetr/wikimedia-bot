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
    public class core : MarshalByRefObject
    {
        public static AppDomain domain = null;
        public static string LastText;
        public static bool disabled;
        public static bool exit = false;
        public static IRC irc;
        public static Dictionary<Module, AppDomain> Domains = new Dictionary<Module, AppDomain>();
        private static List<user> User = new List<user>();
        private static Dictionary<string, string> HelpData = new Dictionary<string, string>();

        [Serializable()]
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

        [Serializable()]
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

        public static void InitialiseMod(Module module)
        { 
            if (module.Name == null || module.Name == "")
            {
                core.Log("This module has invalid name and was terminated to prevent troubles", true);
                throw new Exception("Invalid name");
            }
            module.Date = DateTime.Now;
            if (Module.Exist(module.Name))
            {
                core.Log("This module is already registered " + module.Name + " this new instance was terminated to prevent troubles", true);
                throw new Exception("This module is already registered");
            }
            try
            {
                lock (module)
                {
                    core.Log("Loading module: " + module.Name);
                    Module.module.Add(module);
                }
                if (module.start)
                {
                    module.Init();
                }
            }
            catch (Exception fail)
            {
                module.working = false;
                core.Log("Unable to create instance of " + module.Name);
                core.handleException(fail);
            }
        }

        public static bool LoadMod(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    System.Reflection.Assembly library = System.Reflection.Assembly.LoadFrom(path);

                    //AppDomain domain = AppDomain.CreateDomain("$" + path);
                    if (library == null)
                    {
                        Program.Log("Unable to load " + path + " because the file can't be read", true);
                        return false;
                    }
                    Type[] types = library.GetTypes();
                    Type type = library.GetType("wmib.RegularModule");
                    Type pluginInfo = null;
                    foreach (Type curr in types)
                    {
                        if (curr.IsAssignableFrom(type))            
                        {
                            pluginInfo = curr;
                            break;
                        }
                    }
                    if (pluginInfo == null)
                    {
                        Program.Log("Unable to load " + path + " because the library contains no module", true);
                        return false;
                    }

                     
                    Module _plugin = (Module)Activator.CreateInstance(pluginInfo);
                    
                    //Module _plugin = domain.CreateInstanceFromAndUnwrap(path, "wmib.RegularModule") as Module;

                    _plugin.ParentDomain = core.domain;
                    if (!_plugin.Construct())
                    {
                        core.Log("Invalid module", true);
                        _plugin.Exit();
                        return false;
                    }

                    lock (Domains)
                    {
                        //Domains.Add(_plugin, domain);
                    }

                    InitialiseMod(_plugin);
                    return true;
                }
                Program.Log("Unable to load " + path + " because the file can't be read", true);
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return false;
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
                        irc.wd.WriteLine("PART " + chan.Name + " :" + "dropped by " + user + " from " + origin);
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
                        catch (Exception)
                        { }
                        try
                        {
                            //Feed feed = (Feed)chan.RetrieveObject("rss");
                            //if (feed != null)
                            //{
                            //    feed.Delete();
                            //}
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
                        catch (Exception) { }
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
                        irc.wd.WriteLine("PART " + chan.Name + " :" + "removed by " + user + " from " + origin);
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

        /// <summary>
        /// Display admin command
        /// </summary>
        /// <param name="chan">Channel</param>
        /// <param name="user">User name</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        public static void admin(config.channel chan, string user, string host, string message)
        {
            User invoker = new User(user, host, "");
            if (message == "@reload")
            {
                if (chan.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    chan.LoadConfig();
                    lock (Module.module)
                    {
                        foreach (Module xx in Module.module)
                        {
                            try
                            {
                                if (xx.working)
                                {
                                    xx.Hook_ReloadConfig(chan);
                                }
                            }
                            catch (Exception fail)
                            {
                                Program.Log("Crash on Hook_Reload in " + xx.Name);
                                core.handleException(fail);
                            }
                        }
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("Config", chan.Language), chan.Name);
                    return;
                }
                irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name);
                return;
            }
            if (message == "@refresh")
            {
                if (chan.Users.isApproved(invoker.Nick, host, "flushcache"))
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

            if (message.StartsWith("@part "))
            {
                string channel = message.Substring(6);
                if (channel != "")
                {
                    config.channel Channel = core.getChannel(channel);
                    if (Channel == null)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("UnknownChan", chan.Language), chan.Name, IRC.priority.low);
                        return;
                    }
                    core.partChannel(Channel, invoker.Nick, invoker.Host, "@part", chan.Name);
                    return;
                }
                irc._SlowQueue.DeliverMessage("It would be cool to give me a name of channel you want to part", chan.Name, IRC.priority.low);
                return;
            }

            if (message.StartsWith("@drop "))
            {
                string channel = message.Substring(6);
                if (channel != "")
                {
                    config.channel Channel = core.getChannel(channel);
                    if (Channel == null)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("UnknownChan", chan.Language), chan.Name, IRC.priority.low);
                        return;
                    }
                    core.partChannel(Channel, invoker.Nick, invoker.Host, "@drop", chan.Name);
                    return;
                }
                irc._SlowQueue.DeliverMessage("It would be cool to give me a name of channel you want to drop", chan.Name, IRC.priority.low);
                return;
            }

            if (message.StartsWith("@language"))
            {
                if (chan.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
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

            if (message == "@suppress-off")
            {
                if (chan.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
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
                if (chan.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
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

            if (message.StartsWith("@configure "))
            {
                if (chan.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
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
                        }
                        bool exist = false;
                        lock (Module.module)
                        {
                            foreach (Module curr in Module.module)
                            {
                                try
                                {
                                    if (curr.working)
                                    {
                                        if (curr.Hook_SetConfig(chan, invoker, name, value))
                                        {
                                            exist = true;
                                        }
                                    }
                                }
                                catch (Exception fail)
                                {
                                    Program.Log("Error on Hook_SetConfig module " + curr.Name);
                                    core.handleException(fail);
                                }
                            }
                        }
                        if (!chan.suppress_warnings && !exist)
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

            if (message.StartsWith("@system-lm "))
            {
                if (chan.Users.isApproved(invoker.Nick, invoker.Host, "root"))
                {
                    string module = message.Substring("@system-lm ".Length);
                    if (isModule(module) || module.EndsWith(".bin"))
                    {
                        Module _m = null;
                        _m = getModule(module);
                        if (_m != null)
                        {
                            irc._SlowQueue.DeliverMessage("This module was already loaded and you can't load one module twice, module will be reloaded now", chan.Name, IRC.priority.high);
                            _m.Exit();
                        }
                        if (module.EndsWith(".bin"))
                        {
                            module = "modules" + Path.DirectorySeparatorChar + module;
                            if (File.Exists(module))
                            {
                                if (LoadMod(module))
                                {
                                    irc._SlowQueue.DeliverMessage("Loaded module " + module, chan.Name, IRC.priority.high);
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage("Unable to load module " + module, chan.Name, IRC.priority.high);
                                return;
                            }
                            irc._SlowQueue.DeliverMessage("File not found " + module, chan.Name, IRC.priority.high);
                            return;
                        }

                        irc._SlowQueue.DeliverMessage("Loaded module " + module, chan.Name, IRC.priority.high);
                        return;
                    }
                    irc._SlowQueue.DeliverMessage("This module is not currently loaded in core", chan.Name, IRC.priority.high);
                    return;

                }
            }

            if (message.StartsWith("@system-rm "))
            {
                if (chan.Users.isApproved(invoker.Nick, invoker.Host, "root"))
                {
                    string module = message.Substring("@system-lm ".Length);
                    Module _m = getModule(module);
                    if (_m == null)
                    {
                        irc._SlowQueue.DeliverMessage("This module is not currently loaded in core", chan.Name, IRC.priority.high);
                        return;
                    }
                    _m.Exit();
                    irc._SlowQueue.DeliverMessage("Unloaded module " + module, chan.Name, IRC.priority.high);
                }
            }

            if (message.StartsWith("@join "))
            {
                if (chan.Users.isApproved(invoker.Nick, invoker.Host, "reconnect"))
                {
                    config.channel channel = core.getChannel(message.Substring("@join ".Length));
                    irc.Join(channel);
                }
            }

            lock (Module.module)
            {
                foreach (Module _Module in Module.module)
                {
                    try
                    {
                        if (_Module.working)
                        {
                            _Module.Hook_PRIV(chan, invoker, message);
                        }
                    }
                    catch (Exception f)
                    {
                        core.Log("MODULE: exception at Hook_PRIV in " + _Module.Name, true);
                        core.handleException(f);
                    }
                }
            }

            if (message == "@commands")
            {
                irc._SlowQueue.DeliverMessage("Commands: there is too many commands to display on one line, see http://meta.wikimedia.org/wiki/wm-bot for a list of commands and help", chan.Name);
                return;
            }
        }

        public static Module getModule(string name)
        {
            lock (Module.module)
            {
                foreach (Module module in Module.module)
                {
                    if (module.Name == name)
                    {
                        return module;
                    }
                }
            }
            return null;
        }

        public static bool isModule(string name)
        {
            return false;
        }

        public static void Connect()
        {
            irc = new IRC(config.network, config.username, config.name, config.name);
            irc.Connect();
        }

        public static void SearchMods()
        {
            if (Directory.Exists(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                + Path.DirectorySeparatorChar + "modules"))
            {
                foreach (string dll in Directory.GetFiles(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    + Path.DirectorySeparatorChar + "modules", "*.bin"))
                {
                    LoadMod(dll);
                }
            }
            Program.Log("Modules loaded");
        }

        public static void Kill()
        {
            irc.disabled = true;
            exit = true;
            irc.wd.Close();
            irc.rd.Close();
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
                Register("rss-on", null);
                Register("rss-off", null);
                Register("rss+", null);
                Register("rss-", null);
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
