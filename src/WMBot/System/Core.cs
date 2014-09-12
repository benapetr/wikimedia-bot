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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web;
using ThreadState = System.Threading.ThreadState;

namespace wmib
{
    public partial class Core : MarshalByRefObject
    {
        /// <summary>
        /// Return true if database server is available
        /// </summary>
        public static bool DatabaseServerIsAvailable
        {
            get
            {
                if (Configuration.MySQL.MysqlHost == null || Configuration.MySQL.MysqlUser == null)
                {
                    return false;
                }
                return true;
            }
        }
        /// <summary>
        /// Database server
        /// </summary>
        public static Database DB = null;
        /// <summary>
        /// Last line of text received on irc
        /// </summary>
        public static string LastText = null;
        /// <summary>
        /// Status
        /// </summary>
        private static Status _Status = Status.OK;
        /// <summary>
        /// Thread which is writing the system data to files
        /// </summary>
        public static Thread WriterThread = null;
        public static Thread KernelThread = null;
        private static readonly Dictionary<string, string> HelpData = new Dictionary<string, string>();
        public static bool IsRunning
        {
            get
            {
                if (_Status == Status.ShuttingDown)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Store a traffic log to a file
        /// </summary>
        /// <param name="text"></param>
        public static void TrafficLog(string text)
        {
            if (Configuration.Network.Logging)
            {
                StorageWriter.InsertLine("trafficlog.dat", DateTime.Now + ": " + text, false);
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

        /// <summary>
        /// Exception handler
        /// </summary>
        /// <param name="ex">Exception pointer</param>
        /// <param name="module">Channel name</param>
        public static void HandleException(Exception ex, string module)
        {
            try
            {
                if (!string.IsNullOrEmpty(Configuration.System.DebugChan))
                {
                    if (Configuration.System.SelectedVerbosity > 0)
                        IRC.DeliverMessage("DEBUG Exception in module " + module + ": " + ex.Message + " st: "
                                                     + ex.StackTrace.Replace(Environment.NewLine, ""), Configuration.System.DebugChan);
                    else
                        IRC.DeliverMessage("DEBUG Exception in module " + module + ": " + ex.Message + " last input was "
                                                     + LastText, Configuration.System.DebugChan);
                }
                Syslog.Log("DEBUG Exception in module " + module + ": " + ex.Message + ex.Source + ex.StackTrace, true);
            }
            catch (Exception fail)
            {
                // exception happened while we tried to handle another one, ignore that (probably issue with logging
                Console.WriteLine(fail.ToString());
            }
        }

        /// <summary>
        /// Exception handler
        /// </summary>
        /// <param name="ex">Exception pointer</param>
        public static void HandleException(Exception ex)
        {
            try
            {
                if (!string.IsNullOrEmpty(Configuration.System.DebugChan))
                {
                    if (Configuration.System.SelectedVerbosity > 0)
                        IRC.DeliverMessage("DEBUG Exception: " + ex.Message + " st: " + ex.StackTrace.Replace(Environment.NewLine, ""), Configuration.System.DebugChan);
                    else
                        IRC.DeliverMessage("DEBUG Exception: " + ex.Message + " last input was " + LastText, Configuration.System.DebugChan);
                }
                if (ex.InnerException != null)
                {
                    Syslog.ErrorLog("DEBUG Exception: " + ex.Message + ex.Source + ex.StackTrace +
                                "\n\nThread name: " + Thread.CurrentThread.Name + "\n\nInner: " +
                                    ex.InnerException);
                }
                else
                {
                    Syslog.ErrorLog("DEBUG Exception: " + ex.Message + ex.Source + ex.StackTrace +
                                    "\n\nThread name: " + Thread.CurrentThread.Name);
                }
            }
            catch (Exception fail)
            {
                // exception happened while we tried to handle another one, ignore that (probably issue with logging)
                Console.WriteLine(fail.ToString());
            }
        }

        public static string Trim(string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;
            while (input.StartsWith(" "))
                input = input.Substring(1);
            while (input.EndsWith(" "))
                input = input.Substring(0, input.Length - 1);
            return input;
        }

        /// <summary>
        /// Get a channel object
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns></returns>
        public static Channel GetChannel(string name)
        {
            lock (Configuration.Channels)
            {
                foreach (Channel current in Configuration.Channels)
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
        public static bool GetAction(string message, string Channel, string host, string nick)
        {
            Channel channel = GetChannel(Channel);
            if (channel != null)
            {
                foreach (Module curr in ExtensionHandler.ExtensionList)
                {
                    if (!curr.IsWorking)
                        continue;
                    try
                    {
                        curr.Hook_ACTN(channel, new libirc.UserInfo(nick, "", host), message);
                    }
                    catch (Exception fail)
                    {
                        Syslog.Log("Exception on Hook_ACTN in module: " + curr.Name);
                        HandleException(fail, curr.Name);
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
        public static bool ValidFile(string name)
        {
            return !(name.Contains(" ") || name.Contains("?") || name.Contains("|") || name.Contains("/")
                || name.Contains("\\") || name.Contains(">") || name.Contains("<") || name.Contains("*"));
        }

        /// <summary>
        /// Recover a file that had a backup and remove it
        /// </summary>
        /// <param name="FileName">Name of file</param>
        /// <returns></returns>
        public static bool BackupRecovery(string FileName)
        {
            if (File.Exists(Configuration.TempName(FileName)))
            {
                string temp = Path.GetTempFileName();
                File.Copy(Configuration.TempName(FileName), temp, true);
                Syslog.Log("Unfinished transaction from " + FileName + "~ was stored as " + temp);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create a backup file
        /// 
        /// This is very useful function in case you need to overwrite some file. In that case
        /// there is a chance for program to crash during the write and this would left the
        /// file corrupted, this function will create a copy of a file using config.tempName()
        /// which later needs to be deleted (after you finish your write operation).
        /// 
        /// The file that is to be backed up doesn't need to exist, if it's not present the
        /// function just return false
        /// </summary>
        /// <param name="name">Full path of a file that you want to make a backup of</param>
        /// <returns>True on success or false</returns>
        public static bool BackupData(string name)
        {
            try
            {
                if (!File.Exists(name))
                    return false;
                if (File.Exists(Configuration.TempName(name)))
                    BackupRecovery(name);
                File.Copy(name, Configuration.TempName(name), true);
            }
            catch (Exception b)
            {
                HandleException(b);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Recover a file that was previously stored
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool RecoverFile(string name, string ch = "unknown object")
        {
            try
            {
                if (File.Exists(Configuration.TempName(name)))
                {
                    if (Program.Temp(name))
                    {
                        Syslog.Log("Restoring unfinished transaction of " + ch + " for db_" + name);
                        File.Copy(Configuration.TempName(name), name, true);
                        return true;

                    }
                    Syslog.Log("Unfinished transaction could not be restored! DB of " + name + " is probably broken", true);
                }
                return false;
            }
            catch (Exception b)
            {
                HandleException(b);
                Syslog.Log("Unfinished transaction could not be restored! DB of " + name + " is now broken");
                return false;
            }
        }

        private static void Terminate()
        {
            try
            {
                foreach (Module d in ExtensionHandler.ExtensionList)
                {
                    try
                    {
                        d.Exit();
                    }
                    catch (Exception fail)
                    {
                        HandleException(fail);
                    }
                }
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        /// <summary>
        /// This will disable bot and close this process
        /// </summary>
        public static void Kill()
        {
            try
            {
                if (!IsRunning)
                {
                    Syslog.DebugLog("Attempt to kill bot while it's already being killed", 2);
                    return;
                }
                _Status = Status.ShuttingDown;
                Instance.Kill();
                StorageWriter.IsRunning = false;
                Thread modules = new Thread(Terminate) { Name = "KERNEL: Core helper shutdown thread" };
                modules.Start();
                Syslog.WriteNow("Giving grace time for all modules to finish ok");
                int kill = 0;
                while (kill < 20)
                {
                    kill++;
                    if (ExtensionHandler.ExtensionList.Count == 0)
                    {
                        Syslog.WriteNow("KERNEL: Modules are all down");
                        if (WriterThread.ThreadState == ThreadState.Running || WriterThread.ThreadState == ThreadState.WaitSleepJoin)
                        {
                            Syslog.WriteNow("KERNEL: Writer thread didn't shut down gracefully, waiting 2 seconds", true);
                            Thread.Sleep(2000);
                            if (WriterThread.ThreadState == ThreadState.Running || WriterThread.ThreadState == ThreadState.WaitSleepJoin)
                            {
                                Syslog.WriteNow("KERNEL: Writer thread didn't shut down gracefully, killing", true);
                                WriterThread.Abort();
                            }
                            else
                            {
                                Syslog.WriteNow("KERNEL: Writer thread is shut down", true);
                            }
                        }
                        else
                        {
                            Syslog.WriteNow("KERNEL: Writer thread is down ok");
                        }
                        break;
                    }
                    Thread.Sleep(1000);
                }
                if (ExtensionHandler.ExtensionList.Count == 0)
                {
                    Syslog.WriteNow("KERNEL: Giving a grace time to other threads to finish");
                    Thread.Sleep(200);
                    Syslog.WriteNow("KERNEL: Terminated (ok)");
                    Environment.Exit(0);
                }
            }
            catch (Exception fail)
            {
                HandleException(fail);

            }
            Syslog.WriteNow("There was problem shutting down " + ExtensionHandler.ExtensionList.Count + " modules, terminating process");
            Syslog.WriteNow("KERNEL: Terminated (error)");
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Called when someone post a message to server
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="nick">Nick</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        /// <returns></returns>
        public static bool GetMessage(string channel, string nick, string host, string message)
        {
            LastText = nick + " chan: " + channel + " " + message;
            Channel channel_ = GetChannel(channel);
            if (channel_ != null)
            {
                if (!channel_.IgnoreUnknown || channel_.SystemUsers.IsKnown(nick, host))
                {
                    if (message.StartsWith(Configuration.System.CommandPrefix))
                        Commands.PartChannel(channel_, nick, host, message);
                    Commands.Processing.ProcessCommands(channel_, nick, "", host, message);
                }
                foreach (Module _Module in ExtensionHandler.ExtensionList)
                {
                    try
                    {
                        if (_Module.IsWorking)
                        {
                            _Module.Hook_PRIV(channel_, new libirc.UserInfo(nick, "", host), message);
                        }
                    }
                    catch (Exception f)
                    {
                        Syslog.Log("MODULE: exception at Hook_PRIV in " + _Module.Name, true);
                        HandleException(f);
                    }
                }
                if (channel_.RespondMessage)
                {
                    if (message.StartsWith(Configuration.IRC.NickName + ":"))
                    {
                        DateTime time = channel_.TimeOfLastMsg;
                        if (DateTime.Now >= time.AddSeconds(channel_.RespondWait))
                        {
                            IRC.DeliverMessage(messages.Localize("hi", channel_.Language, new List<string> { nick }), channel_.Name);
                            channel_.TimeOfLastMsg = DateTime.Now;
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
            IRC.DeliverMessage("Info for " + name + ": " + info, channel);
        }

        public static bool ShowHelp(string parameter, Channel channel)
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
                        showInfo(parameter, messages.Localize(parameter.ToLower(), channel.Language), channel.Name);
                        return true;
                    }
                    showInfo(parameter, HelpData[parameter.ToLower()], channel.Name);
                    return true;
                }
            }
            IRC.DeliverMessage("Unknown command type @commands for a list of all commands I know", channel.Name);
            return false;
        }

        /// <summary>
        /// Return uptime
        /// </summary>
        /// <returns></returns>
        public static string getUptime()
        {
            TimeSpan uptime = DateTime.Now - Configuration.System.UpTime;
            return uptime.Days + " days  " + uptime.Hours + " hours since " +
                Configuration.System.UpTime;
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
                return "<tr><td>" + HttpUtility.HtmlEncode(name) + "</td><td><a href=\"#" +
                        HttpUtility.HtmlEncode(value) + "\">" +
                        HttpUtility.HtmlEncode(value) + "</a></td></tr>\n";
            }

            /// <summary>
            /// Insert another table row
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public static string AddKey(string name, string value)
            {
                return "<tr id=\"" + HttpUtility.HtmlEncode(name) + "\"><td>" + HttpUtility.HtmlEncode(name)
                    + "</td><td>" + HttpUtility.HtmlEncode(value) + "</td></tr>\n";
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
