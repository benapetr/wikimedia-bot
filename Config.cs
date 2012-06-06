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

            /// <summary>
            /// Keys
            /// </summary>
            public infobot_core Keys;

            public bool infobot_help = false;

            public bool infobot_sorted = false;

            public bool infobot_auto_complete = false;

            /// <summary>
            /// Recent changes
            /// </summary>
            public RecentChanges RC;

            /// <summary>
            /// Configuration text
            /// </summary>
            private string conf;

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
                bool.TryParse(parseConfig(conf, "feed"), out Feed);
                bool.TryParse(parseConfig(conf, "infobot-sorted-list"), out infobot_sorted);
                if (parseConfig(conf, "infodb") != "")
                {
                    Info = bool.Parse(parseConfig(conf, "infodb"));
                }
                shared = parseConfig(conf, "sharedinfo");
                bool.TryParse(parseConfig(conf, "infobot-help"), out infobot_help);
                bool.TryParse(parseConfig(conf, "infobot-auto-complete"), out infobot_auto_complete);
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
                AddConfig("infobot-help", infobot_help.ToString());
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
                File.WriteAllText(variables.config + "/" + Name + ".setting", conf);
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
                keydb = variables.config + "/" + name + ".db";
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

        /// <summary>
        /// The webpages url
        /// </summary>
        public static string url = "";

        /// <summary>
        /// Dump
        /// </summary>
        public static string DumpDir = "dump";

        /// <summary>
        /// Version
        /// </summary>
        public static string version = "wikimedia bot v. 1.3.8";

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
