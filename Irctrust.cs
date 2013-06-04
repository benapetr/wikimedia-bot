//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace wmib
{
    /// <summary>
    /// Permissions system
    /// </summary>
    [Serializable()]
    public class IRCTrust
    {
        /// <summary>
        /// Filesystem
        /// </summary>
        private static System.IO.FileSystemWatcher fs = new System.IO.FileSystemWatcher();
        private static List<core.SystemUser> GlobalUsers = new List<core.SystemUser>();
        /// <summary>
        /// List of all users in a channel
        /// </summary>
        private List<core.SystemUser> Users = new List<core.SystemUser>();
        /// <summary>
        /// Channel this class belong to
        /// </summary>
        private string ChannelName = null;
        /// <summary>
        /// File where data are stored
        /// </summary>
        public string File = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="channel"></param>
        public IRCTrust(string channel)
        {
            // Load
            File = variables.config + System.IO.Path.DirectorySeparatorChar + channel + "_user";
            core.recoverFile(File);
            if (!System.IO.File.Exists(File))
            {
                // Create db
                Program.Log("Creating user file for " + channel);
                System.IO.File.WriteAllText(File, "");
            }
            string[] db = System.IO.File.ReadAllLines(File);
            ChannelName = channel;
            foreach (string x in db)
            {
                if (x.Contains(config.separator))
                {
                    string[] info = x.Split(Char.Parse(config.separator));
                    string level = info[1];
                    string name = core.decode2(info[0]);
                    Users.Add(new core.SystemUser(level, name));
                }
            }
        }

        private static void GlobalLoad()
        {
            string[] dba = System.IO.File.ReadAllLines(variables.config + System.IO.Path.DirectorySeparatorChar + "admins");
            lock (GlobalUsers)
            {
                GlobalUsers.Clear();
                foreach (string x in dba)
                {
                    if (x.Contains(config.separator))
                    {
                        string[] info = x.Split(Char.Parse(config.separator));
                        string level = info[1];
                        string name = core.decode2(info[0]);
                        GlobalUsers.Add(new core.SystemUser(level, name));
                        core.DebugLog("Registered global user (" + level + "): " + name, 2);
                    }
                }
            }
        }

        private static void GlobalChanged(object sender, EventArgs e)
        {
            core.Log("Global user list has been changed");
            GlobalLoad();
        }

        /// <summary>
        /// Load a global list
        /// </summary>
        public static void Global()
        {
            if (!System.IO.File.Exists(variables.config + System.IO.Path.DirectorySeparatorChar + "admins"))
            {
                // Create db
                Program.Log("Creating user file for admins");
                System.IO.File.WriteAllText(variables.config + System.IO.Path.DirectorySeparatorChar + "admins", "");
            }
            GlobalLoad();
            core.DebugLog("Registering fs watcher");
            fs.Path = variables.config;
            fs.Changed += new System.IO.FileSystemEventHandler(GlobalChanged);
            fs.Created += new System.IO.FileSystemEventHandler(GlobalChanged);
            fs.Filter = "admins";
            fs.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Save
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            core.DebugLog("Saving user file of " + ChannelName);
            core.backupData(File);
            try
            {
                StringBuilder data = new StringBuilder("");
                foreach (core.SystemUser u in Users)
                {
                    data.Append(core.encode2(u.name) + config.separator + u.level + "\n");
                }
                System.IO.File.WriteAllText(File, data.ToString());
                System.IO.File.Delete(config.tempName(File));
            }
            catch (Exception b)
            {
                core.recoverFile(File, ChannelName);
                core.handleException(b);
            }
            return true;
        }

        /// <summary>
        /// Normalize user
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string normalize(string name)
        {
            name = Regex.Escape(name);
            name = name.Replace("?", "\\?");
            return name;
        }

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="level">Level</param>
        /// <param name="user">Regex</param>
        /// <returns></returns>
        public bool addUser(string level, string user)
        {
            if (!misc.IsValidRegex(user))
            {
                core.Log("Unable to create user " + user + " because the regex is invalid", true);
                return false;
            }
            foreach (core.SystemUser u in Users)
            {
                if (u.name == user)
                {
                    core.irc._SlowQueue.DeliverMessage("Unable to add user because this user is already in a list", ChannelName);
                    return false;
                }
            }
            Users.Add(new core.SystemUser(level, user));
            Save();
            return true;
        }

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="user">Regex</param>
        /// <returns></returns>
        public bool delUser(core.SystemUser origin, string user)
        {
            config.channel channel = core.getChannel(ChannelName);
            if (channel == null)
            {
                core.irc._SlowQueue.DeliverMessage("Error: unable to get pointer of current channel", ChannelName);
                return false;
            }
            foreach (core.SystemUser u in Users)
            {
                if (u.name == user)
                {
                    if (getLevel(u.level) > getLevel(origin.level))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Trust1", channel.Language), ChannelName);
                        return true;
                    }
                    if (u.name == origin.name)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Trust2", channel.Language), ChannelName);
                        return true;
                    }
                    Users.Remove(u);
                    Save();
                    core.irc._SlowQueue.DeliverMessage(messages.get("Trust3", channel.Language), ChannelName);
                    return true;
                }
            }
            core.irc._SlowQueue.DeliverMessage(messages.get("Trust4", channel.Language), ChannelName);
            return true;
        }

        /// <summary>
        /// Return level
        /// </summary>
        /// <param name="level">User level</param>
        /// <returns>0</returns>
        private int getLevel(string level)
        {
            switch (level)
            {
                // root is special only for global admins etc
                case "root":
                    return 65534;
                case "admin":
                    return 10;
                case "trusted":
                    return 2;
            }
            return 0;
        }

        /// <summary>
        /// Return user object from a name
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public core.SystemUser getUser(string user)
        {
            core.SystemUser lv = new core.SystemUser("null", "");
            int current = 0;
            lock (GlobalUsers)
            {
                foreach (core.SystemUser b in GlobalUsers)
                {
                    core.RegexCheck id = new core.RegexCheck(b.name, user);
                    if (id.IsMatch() == 1)
                    {
                        if (getLevel(b.level) > current)
                        {
                            current = getLevel(b.level);
                            lv = b;
                        }
                    }
                }
            }
            lock (Users)
            {
                foreach (core.SystemUser b in Users)
                {
                    core.RegexCheck id = new core.RegexCheck(b.name, user);
                    if (id.IsMatch() == 1)
                    {
                        if (getLevel(b.level) > current)
                        {
                            current = getLevel(b.level);
                            lv = b;
                        }
                    }
                }
            }
            return lv;
        }

        /// <summary>
        /// List all users to a channel
        /// </summary>
        public void listAll()
        {
            config.channel Channel = core.getChannel(ChannelName);
            if (Channel == null)
            {
                core.irc._SlowQueue.DeliverMessage("Error: unable to get pointer of current channel", ChannelName);
                return;
            }
            string users_ok = "";
            lock (Users)
            {
                foreach (core.SystemUser b in Users)
                {
                    users_ok += " " + b.name + " (2" + b.level + ")" + ",";
                }
            }
            core.irc._SlowQueue.DeliverMessage(messages.get("TrustedUserList", Channel.Language) + users_ok, ChannelName);
        }

        /// <summary>
        /// Check if user match the necessary level
        /// </summary>
        /// <param name="level">Permission level</param>
        /// <param name="rights">Userrights</param>
        /// <returns></returns>
        public bool matchLevel(int level, string rights)
        {
            if (rights == "root")
            {
                return true;
            }
            switch (level)
            {
                case 2:
                    return (rights == "admin");
                case 1:
                    return (rights == "trusted" || rights == "admin");
            }
            return false;
        }

        /// <summary>
        /// Check if user is approved to do operation requested
        /// </summary>
        /// <param name="User">Username</param>
        /// <param name="Host">Hostname</param>
        /// <param name="command">Approved for specified object / request</param>
        /// <returns></returns>
        public bool isApproved(string User, string Host, string command)
        {
            core.SystemUser current = getUser(User + "!@" + Host);
            if (current.level == "null")
            {
                return false;
            }
            switch (command)
            {
                case "alias_key":
                case "delete_key":
                case "trust":
                case "info":
                case "trustadd":
                case "trustdel":
                case "recentchanges":
                    return matchLevel(1, current.level);
                case "admin":
                case "infobot-manage":
                case "recentchanges-manage":
                case "shutdown":
                    return matchLevel(2, current.level);
                case "flushcache":
                    return matchLevel(200, current.level);
                case "reconnect":
                    return matchLevel(800, current.level);
                case "root":
                    return matchLevel(65535, current.level);
            }
            return false;
        }
    }
}
