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
    [Serializable]
    public class Security
    {
        /// <summary>
        /// Filesystem
        /// </summary>
        private static readonly System.IO.FileSystemWatcher fs = new System.IO.FileSystemWatcher();
        private static readonly List<SystemUser> GlobalUsers = new List<SystemUser>();
        /// <summary>
        /// List of all users in a channel
        /// </summary>
        private readonly List<SystemUser> Users = new List<SystemUser>();
        /// <summary>
        /// Channel this class belong to
        /// </summary>
        private readonly string ChannelName;
        /// <summary>
        /// File where data are stored
        /// </summary>
        public string UserFile = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="channel"></param>
        public Security(string channel)
        {
            // Load
            UserFile = Variables.ConfigurationDirectory + System.IO.Path.DirectorySeparatorChar + channel + "_user";
            Core.RecoverFile(UserFile);
            if (!System.IO.File.Exists(UserFile))
            {
                // Create db
                Syslog.Log("Creating user file for " + channel);
                System.IO.File.WriteAllText(UserFile, "");
            }
            string[] db = System.IO.File.ReadAllLines(UserFile);
            ChannelName = channel;
            foreach (string x in db)
            {
                if (x.Contains(Configuration.System.Separator))
                {
                    string[] info = x.Split(Char.Parse(Configuration.System.Separator));
                    string level = info[1];
                    string name = Core.decode2(info[0]);
                    Users.Add(new SystemUser(level, name));
                }
            }
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="User">Username</param>
        /// <param name="Password">Password</param>
        /// <returns></returns>
        public static int Auth(string User, string Password)
        {
            lock (GlobalUsers)
            {
                foreach (SystemUser user in GlobalUsers)
                {
                    if (user.Password == Password && user.UserName == User)
                    {
                        switch (user.Role)
                        {
                            case "trusted":
                                return 1;
                            case "admin":
                                return 2;
                            case "root":
                                return 10;
                        }
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Load all global users of bot
        /// </summary>
        private static void GlobalLoad()
        {
            string[] dba = System.IO.File.ReadAllLines(Variables.ConfigurationDirectory + 
                           System.IO.Path.DirectorySeparatorChar + "admins");
            lock (GlobalUsers)
            {
                GlobalUsers.Clear();
                foreach (string x in dba)
                {
                    if (x.Contains(Configuration.System.Separator))
                    {
                        string[] info = x.Split(Char.Parse(Configuration.System.Separator));
                        string level = info[1];
                        string name = Core.decode2(info[0]);
                        SystemUser user = new SystemUser(level, name);
                        if (info.Length > 3)
                        {
                            user.UserName = info[3];
                            user.Password = info[2];
                        }
                        GlobalUsers.Add(user);
                        Syslog.DebugLog("Registered global user (" + level + "): " + name, 2);
                    }
                }
            }
        }

        /// <summary>
        /// This is called when the file is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void GlobalChanged(object sender, EventArgs e)
        {
            Syslog.Log("Global user list has been changed");
            GlobalLoad();
        }

        /// <summary>
        /// Load a global list
        /// </summary>
        public static void Global()
        {
            if (!System.IO.File.Exists(Variables.ConfigurationDirectory + System.IO.Path.DirectorySeparatorChar + "admins"))
            {
                // Create db
                Syslog.Log("Creating user file for admins");
                System.IO.File.WriteAllText(Variables.ConfigurationDirectory + System.IO.Path.DirectorySeparatorChar + "admins", "");
            }
            GlobalLoad();
            Syslog.DebugLog("Registering fs watcher for global user list");
            fs.Path = Variables.ConfigurationDirectory;
            fs.Changed += GlobalChanged;
            fs.Created += GlobalChanged;
            fs.Filter = "admins";
            fs.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Save
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            Syslog.DebugLog("Saving user file of " + ChannelName);
            Core.BackupData(UserFile);
            try
            {
                StringBuilder data = new StringBuilder("");
                lock (Users)
                {
                    foreach (SystemUser u in Users)
                    {
                        data.Append(Core.encode2(u.Name) + Configuration.System.Separator + u.Role + "\n");
                    }
                }
                System.IO.File.WriteAllText(UserFile, data.ToString());
                System.IO.File.Delete(Configuration.TempName(UserFile));
            }
            catch (Exception b)
            {
                Core.RecoverFile(UserFile, ChannelName);
                Core.HandleException(b);
            }
            return true;
        }

        /// <summary>
        /// Normalize user
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string EscapeUser(string name)
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
        public bool AddUser(string level, string user)
        {
            if (!misc.IsValidRegex(user))
            {
                Syslog.Log("Unable to create user " + user + " because the regex is invalid", true);
                Core.irc.Queue.DeliverMessage("Unable to add user because this regex is not valid", ChannelName);
                return false;
            }
            foreach (SystemUser u in Users)
            {
                if (u.Name == user)
                {
                    Core.irc.Queue.DeliverMessage("Unable to add user because this user is already in a list", ChannelName);
                    return false;
                }
            }
            Users.Add(new SystemUser(level, user));
            Save();
            return true;
        }

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="user">Regex</param>
        /// <returns></returns>
        public bool DeleteUser(SystemUser origin, string user)
        {
            Channel channel = Core.GetChannel(ChannelName);
            if (channel == null)
            {
                Core.irc.Queue.DeliverMessage("Error: unable to get pointer of current channel", ChannelName);
                return false;
            }
            foreach (SystemUser u in Users)
            {
                if (u.Name == user)
                {
                    if (GetLevel(u.Role) > GetLevel(origin.Role))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("Trust1", channel.Language), ChannelName);
                        return true;
                    }
                    if (u.Name == origin.Name)
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("Trust2", channel.Language), ChannelName);
                        return true;
                    }
                    Users.Remove(u);
                    Save();
                    Core.irc.Queue.DeliverMessage(messages.Localize("Trust3", channel.Language), ChannelName);
                    return true;
                }
            }
            Core.irc.Queue.DeliverMessage(messages.Localize("Trust4", channel.Language), ChannelName);
            return true;
        }

        /// <summary>
        /// Return level
        /// </summary>
        /// <param name="level">User level</param>
        /// <returns>0</returns>
        private int GetLevel(string level)
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
        public SystemUser GetUser(string user)
        {
            SystemUser lv = new SystemUser("null", "");
            int current = 0;
            lock (GlobalUsers)
            {
                foreach (SystemUser b in GlobalUsers)
                {
                    Core.RegexCheck id = new Core.RegexCheck(b.Name, user);
                    if (id.IsMatch() == 1)
                    {
                        if (GetLevel(b.Role) > current)
                        {
                            current = GetLevel(b.Role);
                            lv = b;
                        }
                    }
                }
            }
            lock (Users)
            {
                foreach (SystemUser b in Users)
                {
                    Core.RegexCheck id = new Core.RegexCheck(b.Name, user);
                    if (id.IsMatch() == 1)
                    {
                        if (GetLevel(b.Role) > current)
                        {
                            current = GetLevel(b.Role);
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
        public void ListAll()
        {
            Channel Channel = Core.GetChannel(ChannelName);
            if (Channel == null)
            {
                Core.irc.Queue.DeliverMessage("Error: unable to get pointer of current channel", ChannelName);
                return;
            }
            string users_ok = "";
            lock (Users)
            {
                foreach (SystemUser b in Users)
                {
                    users_ok += " " + b.Name + " (2" + b.Role + ")" + ",";
                }
            }
            Core.irc.Queue.DeliverMessage(messages.Localize("TrustedUserList", Channel.Language) + users_ok, ChannelName);
        }

        /// <summary>
        /// Check if user match the necessary level
        /// </summary>
        /// <param name="level">Permission level</param>
        /// <param name="rights">Userrights</param>
        /// <returns></returns>
        public bool MatchesRole(int level, string role)
        {
            if (role == "root")
            {
                return true;
            }
            switch (level)
            {
                case 2:
                    return (role == "admin");
                case 1:
                    return (role == "trusted" || role == "admin");
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
        public bool IsApproved(string User, string Host, string command)
        {
            SystemUser current = GetUser(User + "!@" + Host);
            if (current.Role == "null")
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
                    return MatchesRole(1, current.Role);
                case "admin":
                case "infobot-manage":
                case "recentchanges-manage":
                case "shutdown":
                    return MatchesRole(2, current.Role);
                case "flushcache":
                    return MatchesRole(200, current.Role);
                case "reconnect":
                    return MatchesRole(800, current.Role);
                case "root":
                    return MatchesRole(65535, current.Role);
            }
            return false;
        }

        /// <summary>
        /// Check if user is approved to do operation requested
        /// </summary>
        /// <param name="user"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool IsApproved(User user, string command)
        {
            return IsApproved(user.Nick, user.Host, command);
        }
    }
}
