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
                if (parseConfig(conf, "logged") != "")
                {
                    Logged = bool.Parse(parseConfig(conf, "logged"));
                }
                if (parseConfig(conf, "feed") != "")
                {
                    Feed = bool.Parse(parseConfig(conf, "feed"));
                }
                if (parseConfig(conf, "infodb") != "")
                {
                    Info = bool.Parse(parseConfig(conf, "infodb"));
                }
                shared = parseConfig(conf, "shared");
                if (parseConfig(conf, "langcode") != "")
                {
                    Language = parseConfig(conf, "langcode");
                }
				if (parseConfig(conf, "talkmode") != "")
                {
                    suppress = bool.Parse(parseConfig(conf, "talkmode"));
                }
                this.sharedlink = new List<channel>();
                    foreach (string x in parseConfig(text, "sharedchan").Replace("\n", "").Split(','))
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
				AddConfig("talkmode", suppress.ToString ());
                AddConfig("langcode", Language);
                AddConfig("keysdb", keydb);
                AddConfig("sharedinfo", shared);
                conf += conf + "\nsharedchan=";
                foreach (channel current in sharedlink)
                {
                    conf += current.Name + ",\n";
                }
                conf = conf + ";";
                File.WriteAllText(variables.config + "/" + Name + ".setting", conf);
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="Name">Channel</param>
            public channel(string name)
            {
                conf = "";
                keydb = variables.config + "/" + Name + ".db";
                Info = true;
                Language = "en";
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
            AddConfig("network", network);
            AddConfig("debug", debugchan);
            AddConfig("nick", login);
            text += text + "\nchannels=";
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
				if (!System.IO.File.Exists (variables.config + "/wmib"))
				{
					System.IO.File.WriteAllText (variables.config + "/wmib", "//this is configuration file for bot, you need to fill in some stuff for it to work");
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
                username = parseConfig( text, "username" );
                network = parseConfig( text, "network" );
                login = parseConfig( text, "nick" );
                debugchan = parseConfig( text, "debug" );
				url = parseConfig( text, "web" );
                password = parseConfig( text, "password" );
				if (login == "")
				{
					Console.WriteLine ("Error there is no username for bot");
					return 1;
				}
				if (network == "")
				{
					Console.WriteLine ("Error irc server is wrong" );
					return 1;
				}
				if (username == "")
				{
					Console.WriteLine ("Error there is no username for bot");
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
        public static string version = "wikimedia bot v. 1.3.6";

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
