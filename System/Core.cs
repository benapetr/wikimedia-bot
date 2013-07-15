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
using System.IO;

namespace wmib
{
    /// <summary>
    /// variables
    /// </summary>
    public class variables
    {
        /// <summary>
        /// Configuration directory
        /// </summary>
        public static readonly string config = "configuration";
        /// <summary>
        /// Prefix for a log directory
        /// </summary>
        public static readonly string prefix_logdir = "log";
        /// <summary>
        /// This string represent a character that changes text to bold
        /// </summary>
        public static readonly string bold = ((char)002).ToString();
    }

    /// <summary>
    /// misc
    /// </summary>
    public class misc
    {
        /// <summary>
        /// Check if a regex is valid
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool IsValidRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;

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

    [Serializable]
    public partial class core : MarshalByRefObject
    {
        /// <summary>
        /// Domain in which the core is running
        /// </summary>
        public static AppDomain domain = null;
        /// <summary>
        /// Last line of text received on irc
        /// </summary>
        public static string LastText = null;
        /// <summary>
        /// Status
        /// </summary>
        public static Status _Status = Status.OK;
        /// <summary>
        /// irc
        /// </summary>
        public static IRC irc = null;
        /// <summary>
        /// Thread which is writing the system data to files
        /// </summary>
        public static Thread WriterThread = null;
        /// <summary>
        /// Domains available in core
        /// </summary>
        public static Dictionary<Module, AppDomain> Domains = new Dictionary<Module, AppDomain>();
        private static readonly Dictionary<string, string> HelpData = new Dictionary<string, string>();
        public static Dictionary<string, Instance> Instances = new Dictionary<string, Instance>();

        /// <summary>
        /// System user
        /// </summary>
        [Serializable]
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

        /// <summary>
        /// Store a traffic log to a file
        /// </summary>
        /// <param name="text"></param>
        public static void TrafficLog(string text)
        {
            if (config.Logging)
            { 
                StorageWriter.InsertLine("trafficlog.dat", DateTime.Now.ToString() + ": " + text, false);
            }
        }

        public static int CreateInstance(string name, int port = 0)
        {
            lock (Instances)
            {
                if (Instances.ContainsKey(name))
                {
                    throw new Exception("Can't load instance " + name + " because this instance already is present");
                }
                Instances.Add(name, new Instance(name, port));
            }
            return 0;
        }

        /// <summary>
        /// Return instance with lowest number of channels
        /// </summary>
        /// <returns></returns>
        public static Instance getInstance()
        {
            int lowest = 99999999;
            Instance instance = null;
            lock (Instances)
            {
                foreach (Instance xx in Instances.Values)
                {
                    if (xx.ChannelCount < lowest)
                    {
                        lowest = xx.ChannelCount;
                        instance = xx;
                    }
                }
            }
            return instance;
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

        /// <summary>
        /// Write a system log
        /// </summary>
        /// <param name="text">Text of log</param>
        /// <param name="error">Should be considered warning</param>
        public static void Log(string text, bool error = false)
        {
            Program.Log(text, error);
        }

        /// <summary>
        /// Debug log
        /// </summary>
        /// <param name="text"></param>
        /// <param name="verbosity"></param>
        public static void DebugLog(string text, int verbosity = 1)
        {
            if (config.SelectedVerbosity >= verbosity)
            {
                Log("DEBUG: " + text);
            }
        }

        /// <summary>
        /// Exception handler
        /// </summary>
        /// <param name="ex">Exception pointer</param>
        /// <param name="module">Channel name</param>
        public static void handleException(Exception ex, string module)
        {
            try
            {
                if (!string.IsNullOrEmpty(config.debugchan))
                {
                    irc._SlowQueue.DeliverMessage("DEBUG Exception in module " + module + ": " + ex.Message + " last input was " + LastText, config.debugchan);
                }
                Program.Log("DEBUG Exception in module " + module + ": " + ex.Message + ex.Source + ex.StackTrace, true);
            }
            catch (Exception) // exception happened while we tried to handle another one, ignore that (probably issue with logging)
            { }
        }

        /// <summary>
        /// Exception handler
        /// </summary>
        /// <param name="ex">Exception pointer</param>
        public static void handleException(Exception ex)
        {
            try
            {
                if (!string.IsNullOrEmpty(config.debugchan))
                {
                    irc._SlowQueue.DeliverMessage("DEBUG Exception: " + ex.Message + " last input was " + LastText, config.debugchan);
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

        /// <summary>
        /// Return true in case the filename is valid
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns></returns>
        public static bool validFile(string name)
        {
            return !(name.Contains(" ") || name.Contains("?") || name.Contains("|") || name.Contains("/")
                || name.Contains("\\") || name.Contains(">") || name.Contains("<") || name.Contains("*"));
        }

        /// <summary>
        /// Recover a file that had a backup and remove it
        /// </summary>
        /// <param name="FileName">Name of file</param>
        /// <returns></returns>
        public static bool backupRecovery(string FileName)
        {
            if (File.Exists(config.tempName(FileName)))
            {
                string temp = System.IO.Path.GetTempFileName();
                File.Copy(config.tempName(FileName), temp, true);
                Program.Log("Unfinished transaction from " + FileName + "~ was stored as " + temp);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Recover a file that had a backup and remove it
        /// </summary>
        /// <param name="name">Name of file</param>
        /// <param name="ch"></param>
        /// <returns></returns>
        [Obsolete]
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

        /// <summary>
        /// Recover a file that was previously stored
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool recoverFile(string name, string ch = "unknown object")
        {
            try
            {
                if (File.Exists(config.tempName(name)))
                {
                    if (Program.Temp(name))
                    {
                        Program.Log("Restoring unfinished transaction of " + ch + " for db_" + name);
                        File.Copy(config.tempName(name), name, true);
                        return true;
                        
                    }
                    Program.Log("Unfinished transaction could not be restored! DB of " + name + " is probably broken", true);
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
        /// Connect to network
        /// </summary>
        public static void Connect()
        {
            irc = Instances[config.username].irc;
            // now we load all instances
            lock (Instances)
            {
                foreach (Instance instance in Instances.Values)
                {
                    // connect it to irc
                    instance.Init();
                }
                // now we need to wait for all instances to connect
                core.Log("Waiting for all instances to connect to irc");
                bool IsOk = false;
                while (!IsOk)
                {
                    foreach (Instance instance in Instances.Values)
                    {
                        if (!instance.IsWorking)
                        {
                            Thread.Sleep(100);
                            break;
                        }
                    }
                    IsOk = true;
                }
                // now we make all instances join their channels
                foreach (Instance instance in Instances.Values)
                {
                    instance.Join();
                }

                // wait for all instances to join their channels
                core.Log("Waiting for all instances to join channels");
                IsOk = false;
                while (!IsOk)
                {
                    foreach (Instance instance in Instances.Values)
                    {
                        if (!instance.irc.ChannelsJoined)
                        {
                            Thread.Sleep(100);
                            break;
                        }
                    }
                    IsOk = true;
                }
                core.Log("All instances joined their channels");
            }

            while (_Status == Status.OK)
            {
                Thread.Sleep(200);
            }
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

        /// <summary>
        /// This will disable bot and close this process
        /// </summary>
        public static void Kill()
        {
            try
            {
                if (_Status == Status.ShuttingDown)
                {
                    DebugLog("Attempt to kill bot while it's already being killed", 2);
                    return;
                }
                _Status = Status.ShuttingDown;
                irc.Disconnect();
                irc._SlowQueue.Exit();
                StorageWriter.isRunning = false;
                Thread modules = new Thread(Terminate) {Name = "KERNEL: Core helper shutdown thread"};
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
                    if (!curr.Users.IsApproved(nick, host, "trust"))
                    {
                        return false;
                    }
                }
                if (message.StartsWith(config.CommandPrefix))
                {
                    ModifyRights(message, curr, nick, host);
                    addChannel(curr, nick, host, message);
                    partChannel(curr, nick, host, message);
                }
                ParseAdmin(curr, nick, host, message);
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

        /// <summary>
        /// Help
        /// </summary>
        public static class Help
        {
            /// <summary>
            /// Register a new item
            /// </summary>
            /// <param name="name">Name</param>
            /// <param name="text">Text</param>
            /// <returns></returns>
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

            /// <summary>
            /// Initialise help
            /// </summary>
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

            /// <summary>
            /// Remove a help
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
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

        /// <summary>
        /// This is a helper class that convert some special cloaks
        /// </summary>
        public static class Host
        {
            /// <summary>
            /// Change the freenode special cloaks
            /// </summary>
            /// <param name="host">Host</param>
            /// <returns></returns>
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
                if (host.StartsWith("wikipedia/"))
                {
                    host = host.Substring("wikipedia/".Length);
                    return "https://en.wikipedia.org/wiki/User:" + host;
                }
                if (host.StartsWith("mediawiki/"))
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
                    if (HelpData[parameter.ToLower()] == null)
                    {
                        showInfo(parameter, messages.get(parameter.ToLower(), channel.Language), channel.Name);
                        return true;
                    }
                    showInfo(parameter, HelpData[parameter.ToLower()], channel.Name);
                    return true;
                }
            }
            irc._SlowQueue.DeliverMessage("Unknown command type @commands for a list of all commands I know", channel.Name);
            return false;
        }

        /// <summary>
        /// Return uptime
        /// </summary>
        /// <returns></returns>
        public static string getUptime()
        {
            System.TimeSpan uptime = System.DateTime.Now - config.UpTime;
            return uptime.Days.ToString() + " days  " + uptime.Hours.ToString() + " hours since " + config.UpTime.ToString();
        }

        /// <summary>
        /// This class allow you to do some elementar operations with web pages
        /// </summary>
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

        /// <summary>
        /// Status of bot
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// OK
            /// </summary>
            OK,
            /// <summary>
            /// System is being killed
            /// </summary>
            ShuttingDown,
        }
    }
}
