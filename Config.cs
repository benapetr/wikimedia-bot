//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena benapetr@gmail.com

using System;
using System.Collections.Generic;
using System.IO;

namespace wmib
{
    public static class config
    {
        public class channel
        {
            /// <summary>
            /// Channel name
            /// </summary>
            public string Name;
            public string Language;

            public bool Logged;

            /// <summary>
            /// Log
            /// </summary>
            public string Log;

            public bool Feed;
            public bool Info;

            public bool suppress;

            public List<string> Infobot_IgnoredNames = new List<string>();

            /// <summary>
            /// Keys
            /// </summary>
            public infobot_core Keys;

            public int respond_wait = 120;

            public bool respond_message = false;

            public System.DateTime last_msg = System.DateTime.Now;

            public bool infobot_trim_white_space_in_name = true;

            /// <summary>
            /// Infobot help
            /// </summary>
            public bool infobot_help = false;

            /// <summary>
            /// Infobot sorted
            /// </summary>
            public bool infobot_sorted = false;
			
            /// <summary>
            /// Doesn't send any warnings on error
            /// </summary>
			public bool suppress_warnings = false;

            public bool logs_no_write_data = false;

            /// <summary>
            /// Completion
            /// </summary>
            public bool infobot_auto_complete = false;

            /// <summary>
            /// Recent changes
            /// </summary>
            public RecentChanges RC;

            /// <summary>
            /// Configuration text
            /// </summary>
            private string conf;

            public bool ignore_unknown = false;

            public string shared;

            public List<config.channel> sharedlink;

            /// <summary>
            /// Users
            /// </summary>
            public IRCTrust Users;

            /// <summary>
            /// Path of db
            /// </summary>
            public string keydb = "";

            /// <summary>
            /// Add a line to config
            /// </summary>
            /// <param name="a">Name of key</param>
            /// <param name="b">Value</param>
            private void AddConfig(string a, string b)
            {
                conf += "\n" + a + "=" + b + ";";
            }

            public static bool channelExist(string _Channel)
            {
                string conf_file = variables.config + "/" + _Channel + ".setting";
                if (File.Exists(conf_file))
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Load config of channel :)
            /// </summary>
            public void LoadConfig()
            {
                string conf_file = variables.config + "/" + Name + ".setting";
                core.recoverFile(conf_file, Name);
                RecentChanges.InsertSite();
                if (!File.Exists(conf_file))
                {
                    File.WriteAllText(conf_file, "");
                    Program.Log("Creating datafile for channel " + Name);
                    return;
                }
                conf = File.ReadAllText(conf_file);
                if (parseConfig(conf, "keysdb") != "")
                {
                    keydb = (parseConfig(conf, "keysdb"));
                }
                bool.TryParse(parseConfig(conf, "logged"), out Logged);
				bool.TryParse(parseConfig(conf, "suppress-warnings"), out suppress_warnings);
                bool.TryParse(parseConfig(conf, "respond_message"), out respond_message);
                int _temp_respond_wait;
                if ( int.TryParse(parseConfig(conf, "respond_wait"), out _temp_respond_wait) )
                {
                    respond_wait = _temp_respond_wait;
                }
                if (!bool.TryParse(parseConfig(conf, "infobot-trim-white-space-in-name"), out infobot_trim_white_space_in_name))
                {
                    infobot_trim_white_space_in_name = true;
                }
                last_msg = last_msg.AddSeconds((-1) * respond_wait);
                bool.TryParse(parseConfig(conf, "feed"), out Feed);
                bool.TryParse(parseConfig(conf, "infobot-sorted-list"), out infobot_sorted);
                bool.TryParse(parseConfig(conf, "ignore-unknown"), out ignore_unknown);
                if (parseConfig(conf, "infodb") != "")
                {
                    Info = bool.Parse(parseConfig(conf, "infodb"));
                }
                shared = parseConfig(conf, "sharedinfo");
                bool.TryParse(parseConfig(conf, "infobot-help"), out infobot_help);
                bool.TryParse(parseConfig(conf, "infobot-auto-complete"), out infobot_auto_complete);
                string infobot_ignore = parseConfig(conf, "infobot_ignores");
                if (infobot_ignore != "")
                {
                    foreach (string x in infobot_ignore.Replace("\n", "").Split(','))
                    {
                        string item = x.Replace(" ", "");
                        if (item != "")
                        {
                            Infobot_IgnoredNames.Add(item);
                        }
                    }
                }
                if (parseConfig(conf, "langcode") != "")
                {
                    Language = parseConfig(conf, "langcode");
                }
                bool.TryParse(parseConfig(conf, "talkmode"), out suppress);
            }

            /// <summary>
            /// Save config
            /// </summary>
            public void SaveConfig()
            {
                conf = "";
                AddConfig("infodb", Info.ToString());
                AddConfig("logged", Logged.ToString());
                AddConfig("feed", Feed.ToString());
                AddConfig("talkmode", suppress.ToString());
                AddConfig("infobot-sorted-list", infobot_sorted.ToString());
                AddConfig("langcode", Language);
                AddConfig("respond_message", respond_message.ToString());
                AddConfig("respond_wait", respond_wait.ToString());
                AddConfig("infobot-trim-white-space-in-name", infobot_trim_white_space_in_name.ToString());
                AddConfig("ignore-unknown", ignore_unknown.ToString());
                AddConfig("infobot-help", infobot_help.ToString());
				AddConfig("suppress-warnings", suppress_warnings.ToString());
                AddConfig("keysdb", keydb);
                AddConfig("infobot-auto-complete", infobot_auto_complete.ToString());
                AddConfig("sharedinfo", shared);
                if (!(sharedlink.Count < 1))
                {
                    conf += "\nsharedchan=";
                    foreach (channel current in sharedlink)
                    {
                        conf += current.Name + ",\n";
                    }
                    conf = conf + ";";
                }
                if (!(Infobot_IgnoredNames.Count < 1))
                {
                    conf = conf + "\ninfobot_ignores=";
                    foreach (string curr in Infobot_IgnoredNames)
                    {
                        conf = conf + curr + ",\n";
                    }
                    conf += ";";
                }
                core.backupData(variables.config + "/" + Name + ".setting");
                try
                {
                    File.WriteAllText(variables.config + "/" + Name + ".setting", conf);
                    File.Delete(tempName(variables.config + "/" + Name + ".setting"));
                }
                catch (Exception)
                {
                    core.recoverFile(variables.config + "/" + Name + ".setting", Name);
                }
            }

            public int Shares()
            {
                string conf_file = variables.config + "/" + Name + ".setting";
                conf = File.ReadAllText(conf_file);
                foreach (string x in parseConfig(conf, "sharedchan").Replace("\n", "").Split(','))
                {
                    string name = x.Replace(" ", "");
                    if (name != "")
                    {
                        if (core.getChannel(name) != null)
                        {
                            if (sharedlink.Contains(core.getChannel(name)) == false)
                            {
                                sharedlink.Add(core.getChannel(name));
                            }
                        }
                    }
                }
                return 0;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="Name">Channel</param>
            public channel(string name)
            {
                conf = "";
                keydb = variables.config + Path.DirectorySeparatorChar + name + ".db";
                Info = true;
                Language = "en";
                sharedlink = new List<channel>();
                shared = "";
                suppress = false;
                Feed = false;
                Logged = false;
                Name = name;
                LoadConfig();
                RC = new RecentChanges(this);
                if (!Directory.Exists("log"))
                {
                    Directory.CreateDirectory("log");
                }
                if (!Directory.Exists("log/" + Name))
                {
                    Directory.CreateDirectory("log/" + Name);
                }
                Keys = new infobot_core(keydb, Name);
                Log = "log/" + Name + "/";
                Users = new IRCTrust(Name);
            }
        }

        // Configuration is down, bellow are functions

        /// <summary>
        /// Add line to the config file
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private static void AddConfig(string a, string b)
        {
            text = text + "\n" + a + "=" + b + ";";
        }

        public static void Save()
        {
            text = "";
            AddConfig("username", username);
            AddConfig("password", password);
            AddConfig("web", url);
            AddConfig("serverIO", serverIO.ToString());
            AddConfig("debug", debugchan);
            AddConfig("network", network);
            AddConfig("style_html_file", css);
            AddConfig("nick", login);
            text += "\nchannels=";
            foreach (channel current in channels)
            {
                text += current.Name + ",\n";
            }
            text = text + ";";
            File.WriteAllText(variables.config + "/wmib", text);
        }

        /// <summary>
        /// Parse config data text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string parseConfig(string text, string name)
        {
            if (text.Contains(name))
            {
                string x = text;
                x = text.Substring(text.IndexOf(name + "=")).Replace(name + "=", "");
                x = x.Substring(0, x.IndexOf(";"));
                return x;
            }
            return "";
        }

        public static string tempName(string file)
        {
            return (file + "~");
        }

        /// <summary>
        /// Load config of bot
        /// </summary>
        public static int Load()
        {
            try
            {
                if (Directory.Exists(variables.config) == false)
                {
                    Directory.CreateDirectory(variables.config);
                }
                if (!System.IO.File.Exists(variables.config + "/wmib"))
                {
                    System.IO.File.WriteAllText(variables.config + "/wmib", "//this is configuration file for bot, you need to fill in some stuff for it to work");
                }
                text = File.ReadAllText(variables.config + "/wmib");
                bool _serverIO;
                if (bool.TryParse(parseConfig(text, "serverIO"), out _serverIO))
                {
                    serverIO = _serverIO;
                }
                foreach (string x in parseConfig(text, "channels").Replace("\n", "").Split(','))
                {
                    string name = x.Replace(" ", "");
                    if (name != "")
                    {
                        channels.Add(new channel(name));
                    }
                }
                Program.Log("Channels were all loaded");

                // Now when all chans are loaded let's link them together
                foreach (channel ch in channels)
                {
                    ch.Shares();
                }
                Program.Log("Channel db's working");
                username = parseConfig(text, "username");
                network = parseConfig(text, "network");
                login = parseConfig(text, "nick");
                debugchan = parseConfig(text, "debug");
                css = parseConfig(text, "style_html_file");
                url = parseConfig(text, "web");
                password = parseConfig(text, "password");
                if (login == "")
                {
                    Console.WriteLine("Error there is no username for bot");
                    return 1;
                }
                if (network == "")
                {
                    Console.WriteLine("Error irc server is wrong");
                    return 1;
                }
                if (username == "")
                {
                    Console.WriteLine("Error there is no username for bot");
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                core.handleException(ex);
            }
            if (!Directory.Exists(DumpDir))
            {
                Directory.CreateDirectory(DumpDir);
            }
            return 0;
        }

        public static string text;

        /// <summary>
        /// Network
        /// </summary>

        public static string network = "irc.freenode.net";

        /// <summary>
        /// Nick name
        /// </summary>

        public static string username = "wm-bot";

        /// <summary>
        /// Uptime
        /// </summary>
        public static System.DateTime UpTime;

        public static string debugchan = null;

        public static string css;

        /// <summary>
        /// Login name
        /// </summary>
        public static string login = "";

        /// <summary>
        /// Login pw
        /// </summary>
        public static string password = "";

        public static bool serverIO = false;

        /// <summary>
        /// The webpages url
        /// </summary>
        public static string url = "";

        /// <summary>
        /// Dump
        /// </summary>
        public static string DumpDir = "dump";

        public static string TransactionLog = "transaction.dat";

        /// <summary>
        /// Version
        /// </summary>
        public static string version = "wikimedia bot v. 1.8.2";

        /// <summary>
        /// Separator
        /// </summary>
        public static string separator = "|";

        /// <summary>
        /// User name
        /// </summary>
        public static string name = "wm-bot";

        /// <summary>
        /// Channels
        /// </summary>
        public static List<channel> channels = new List<channel>();
    }
}
