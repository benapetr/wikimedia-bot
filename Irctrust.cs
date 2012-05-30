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
    public class IRCTrust
    {
        private List<core.user> GlobalUsers = new List<core.user>();
        /// <summary>
        /// List of all users in a channel
        /// </summary>
        private List<core.user> Users = new List<core.user>();

        /// <summary>
        /// Channel this class belong to
        /// </summary>
        public string _Channel;
        /// <summary>
        /// File where data are stored
        /// </summary>
        public string File;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="channel"></param>
        public IRCTrust(string channel)
        {
            // Load
            File = variables.config + "/" + channel + "_user";
            if (!System.IO.File.Exists(File))
            {
                // Create db
                Program.Log("Creating user file for " + channel);
                System.IO.File.WriteAllText(File, "");
            }
            if (!System.IO.File.Exists(variables.config + "/" + "admins"))
            {
                // Create db
                Program.Log("Creating user file for admins");
                System.IO.File.WriteAllText(variables.config + "/" + "admins", "");
            }
            string[] db = System.IO.File.ReadAllLines(File);
            _Channel = channel;
            foreach (string x in db)
            {
                if (x.Contains(config.separator))
                {
                    string[] info = x.Split(Char.Parse(config.separator));
                    string level = info[1];
                    string name = core.decode(info[0]);
                    Users.Add(new core.user(level, name));
                }
            }
            string[] dba = System.IO.File.ReadAllLines(variables.config + "/" + "admins");
            _Channel = channel;
            foreach (string x in dba)
            {
                if (x.Contains(config.separator))
                {
                    string[] info = x.Split(Char.Parse(config.separator));
                    string level = info[1];
                    string name = core.decode(info[0]);
                    GlobalUsers.Add(new core.user(level, name));
                }
            }
        }

        /// <summary>
        /// Save
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            System.IO.File.WriteAllText(File, "");
            foreach (core.user u in Users)
            {
                System.IO.File.AppendAllText(File, core.encode(u.name) + config.separator + u.level + "\n");
            }
            return true;
        }

        public static string normalize(string name)
        {
            name = Regex.Escape(name);
            name = name.Replace("?", "\\?");
            return name;
        }

        /// <summary>
        /// New
        /// </summary>
        /// <param name="level">Level</param>
        /// <param name="user">Regex</param>
        /// <returns></returns>
        public bool addUser(string level, string user)
        {
            if (!misc.IsValidRegex(user))
            {
                return false;
            }
            foreach (core.user u in Users)
            {
                if (u.name == user)
                {
                    return false;
                }
            }
            Users.Add(new core.user(level, user));
            Save();
            return true;
        }

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="user">Regex</param>
        /// <returns>bool</returns>
        public bool delUser(core.user trusted, string user)
        {
            config.channel channel = core.getChannel(_Channel);
            if (channel == null)
            {
                core.irc._SlowQueue.DeliverMessage("Error: unable to get pointer of current channel", _Channel);
                return false;
            }
            foreach (core.user u in Users)
            {
                if (u.name == user)
                {
                    if (getLevel(u.level) > getLevel(trusted.level))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Trust1", channel.Language), _Channel);
                        return true;
                    }
                    if (u.name == trusted.name)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Trust2", channel.Language), _Channel);
                        return true;
                    }
                    Users.Remove(u);
                    Save();
                    core.irc._SlowQueue.DeliverMessage(messages.get("Trust3", channel.Language), _Channel);
                    return true;
                }
            }
            core.irc._SlowQueue.DeliverMessage(messages.get("Trust4", channel.Language), _Channel);
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
        public core.user getUser(string user)
        {
            core.user lv = new core.user("null", "");
            int current = 0;
            foreach (core.user b in GlobalUsers)
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
            foreach (core.user b in Users)
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
            return lv;
        }

        /// <summary>
        /// List all users to a channel
        /// </summary>
        public void listAll()
        {
            config.channel Channel = core.getChannel(_Channel);
            if (Channel == null)
            {
                core.irc._SlowQueue.DeliverMessage("Error: unable to get pointer of current channel", _Channel);
                return;
            }
            string users_ok = "";
            foreach (core.user b in Users)
            {
                users_ok += " " + b.name + " (2" + b.level + ")" + ",";
            }
            core.irc._SlowQueue.DeliverMessage(messages.get("TrustedUserList", Channel.Language) + users_ok, _Channel);
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
            if (level == 2)
            {
                return (rights == "admin");
            }
            if (level == 1)
            {
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
            core.user current = getUser(User + "!@" + Host);
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
            }
            return false;
        }
    }
}
