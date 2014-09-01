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
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace wmib
{
    /// <summary>
    /// Permissions system
    /// </summary>
    [Serializable]
    public class Security
    {
        public class Role
        {
            public List<string> Permissions = new List<string>();
            /// <summary>
            /// Every role may contain other roles as well
            /// </summary>
            public List<Role> Roles = new List<Role>();
            /// <summary>
            /// The level of role used to compare which role is higher
            /// </summary>
            public int Level;
            // For serialization only
            public Role(){}

            public Role(int level_)
            {
                this.Level = level_;
            }
            
            public void Revoke(string permission)
            {
                lock (this.Permissions)
                {
                    if (this.Permissions.Contains(permission))
                    {
                        this.Permissions.Remove(permission);
                    }
                }
            }
            
            public void Revoke(Role role)
            {
                lock (this.Roles)
                {
                    if (this.Roles.Contains(role))
                    {
                        this.Roles.Remove(role);
                    }
                }
            }
            
            public void Grant(Role role)
            {
                lock (this.Roles)
                {
                    if (!this.Roles.Contains(role))
                    {
                        this.Roles.Add(role);
                    }
                }
            }
            
            public void Grant(string permission)
            {
                lock (this.Permissions)
                {
                    if (!this.Permissions.Contains(permission))
                        this.Permissions.Add(permission);
                }
            }
            
            public bool IsPermitted(string permission)
            {
                if (this.Permissions.Contains("root") || this.Permissions.Contains(permission))
                    return true;
                lock (this.Roles)
                {
                    foreach (Role role in Roles)
                    {
                        if (role.IsPermitted(permission))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// IDictionary can't be serialized so we need to convert the dictionary to some other object with similar schema that
        /// can be serialized before we do that, this class is used to serialize role information
        /// </summary>
        [Serializable]
        public class DumpableDict
        {
            public class RoleInfo
            {
                public string Name;
                public List<string> Roles = new List<string>();
                public List<string> Permissions = new List<string>();
                public int Level;
                public RoleInfo() {}
                public RoleInfo(Role role)
                {
                    lock (Security.Roles)
                    {
                        // This could eventually cause a deadlock better make sure it's not
                        lock (role.Roles)
                        {
                            foreach (Role r in role.Roles)
                                foreach (KeyValuePair<string, Role> kd in Security.Roles)
                                    if (kd.Value == r)
                                        this.Roles.Add(kd.Key);
                        }
                    }
                    Level = role.Level;
                    Permissions.AddRange(role.Permissions);
                }
            }

            public List<RoleInfo> Roles = new List<RoleInfo>();
            public DumpableDict() { }
            public DumpableDict(Dictionary<string, Role> Roles)
            {
                foreach (KeyValuePair<string,Role> role in Roles)
                {
                    RoleInfo i = new RoleInfo(role.Value);
                    i.Name = role.Key;
                    this.Roles.Add(i);
                }
            }
        }

        public static Dictionary<string, Role> Roles = new Dictionary<string, Role>();
        /// <summary>
        /// Filesystem
        /// </summary>
        private static readonly FileSystemWatcher fs = new FileSystemWatcher();
        private static readonly List<SystemUser> globalUsers = new List<SystemUser>();
        /// <summary>
        /// List of all users in a channel
        /// </summary>
        public readonly List<SystemUser> Users = new List<SystemUser>();
        /// <summary>
        /// Channel this class belong to
        /// </summary>
        private readonly Channel _Channel;

        public Security(Channel channel)
        {
            this._Channel = channel;
        }
        
        public static bool IsGloballyApproved(SystemUser user, string permission)
        {
            return HasPrivilege(permission, user.Role);
        }
        
        /// <summary>
        /// Load all roles
        /// </summary>
        public static void Init()
        {
            Core.RecoverFile(Configuration.Paths.Security);
            if (File.Exists(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "security.xml"))
            {
                DumpableDict dict = new DumpableDict();
                XmlSerializer xmlSerializer = new XmlSerializer(dict.GetType());
                using (StreamReader reader = new StreamReader(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "security.xml"))
                {
                    dict = (DumpableDict)xmlSerializer.Deserialize(reader);
                }
                // first we need to create all roles in the file, that is important
                foreach (DumpableDict.RoleInfo ri in dict.Roles)
                {
                    Roles.Add(ri.Name, new Role(ri.Level));
                }
                foreach (DumpableDict.RoleInfo ri in dict.Roles)
                {
                    // now give this role all permissions and roles it had
                    foreach (string permission in ri.Permissions)
                    {
                        Roles[ri.Name].Grant(permission);
                    }
                    foreach (string role in ri.Roles)
                    {
                        if (!Roles.ContainsKey(role))
                        {
                            Syslog.WarningLog("There is no role " + role + " that could be granted to " + ri.Name + " skipping it");
                        }
                        Roles[ri.Name].Grant(Roles[role]);
                    }
                }
                return;
            }
            // let's assume there is no role definition file, so we create some initial, built-in roles
            Roles.Add("null", new Role(0));
            Roles.Add("trusted", new Role(1));
            Roles.Add("operator", new Role(6));
            Roles.Add("admin", new Role(20));
            Roles.Add("root", new Role(65535));
            Roles["trusted"].Grant("trust");
            // trusted users can add users to trust list
            Roles["trusted"].Grant("trustadd");
            Roles["trusted"].Grant("trustdel");
            Roles["operator"].Grant(Roles["trusted"]);
            Roles["operator"].Grant("join");
            Roles["admin"].Grant("admin");
            Roles["admin"].Grant("drop");
            Roles["admin"].Grant("suppress");
            Roles["admin"].Grant("unsuppress");
            Roles["admin"].Grant("part");
            Roles["admin"].Grant(Roles["operator"]);
            // admins have all privileges as trusted users
            Roles["admin"].Grant(Roles["trusted"]);
            Roles["root"].Grant("root");
            lock (ExtensionHandler.Extensions)
            {
                foreach (Module module in ExtensionHandler.Extensions)
                {
                    try
                    {
                        module.RegisterPermissions();
                    }
                    catch (Exception fail)
                    {
                        Core.HandleException(fail, module.Name);
                    }
                }
            }
        }

        public static string Dump()
        {
            DumpableDict du = new DumpableDict(Roles);
            XmlSerializer xmlSerializer = new XmlSerializer(du.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, du);
                return textWriter.ToString();
            }
        }

        public static int GetLevelOfRole(string role)
        {
            if (Roles.ContainsKey(role))
            {
                return Roles[role].Level;
            }
            return 0;
        }
        
        /// <summary>
        /// Verify the users credentials and if they are correct, returns a user instance
        /// </summary>
        /// <param name="User">Username</param>
        /// <param name="Password">Password</param>
        /// <returns></returns>
        public static SystemUser Auth(string User, string Password)
        {
            lock (globalUsers)
            {
                foreach (SystemUser user in globalUsers)
                {
                    if (user.Password == Password && user.UserName == User)
                    {
                        return user;
                    }
                }
            }
            return null;
        }

        public static bool IsRole(string name)
        {
            if (name == "root" || name == "null")
            {
                return false;
            }
            return Roles.ContainsKey(name);
        }

        public static string GetNameOfRole(Security.Role role)
        {
            lock (Security.Roles)
            {
                foreach (KeyValuePair<string, Security.Role> xx in Security.Roles)
                {
                    if (role == xx.Value)
                        return xx.Key;
                }
            }
            return "{unknown role}";
        }

        public void InsertUser(XmlNode node)
        {
            string regex = null;
            string role = null;
            foreach (XmlAttribute info in node.Attributes)
            {
                switch (info.Name)
                {
                    case "regex":
                        regex = info.Value;
                        break;
                    case "role":
                        role = info.Value;
                        break;
                }
            }
            if (regex == null || role == null)
            {
                Syslog.WarningLog("Skipping invalid user record for " + this._Channel.Name);
            }
            lock (Users)
            {
                Users.Add(new SystemUser(role, regex));
            }
        }

        /// <summary>
        /// Load all global users of bot
        /// </summary>
        private static void GlobalLoad()
        {
            string[] dba = File.ReadAllLines(Variables.ConfigurationDirectory + 
                           Path.DirectorySeparatorChar + "admins");
            lock (globalUsers)
            {
                globalUsers.Clear();
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
                        globalUsers.Add(user);
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
            if (!File.Exists(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "admins"))
            {
                // Create db
                Syslog.Log("Creating user file for admins");
                File.WriteAllText(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "admins", "");
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
            this._Channel.SaveConfig();
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
                IRC.DeliverMessage("Unable to add user because this regex is not valid", this._Channel);
                return false;
            }
            foreach (SystemUser u in Users)
            {
                if (u.Name == user)
                {
                    IRC.DeliverMessage("Unable to add user because this user is already in a list", this._Channel);
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
        public void DeleteUser(SystemUser origin, string user)
        {
            foreach (SystemUser u in Users)
            {
                if (u.Name == user)
                {
                    if (GetLevelOfRole(u.Role) > GetLevelOfRole(origin.Role))
                    {
                        // users with role that has lower level than role of user who is to be removed aren't allowed to do that
                        // eg. trusted can't delete admin from channel
                        IRC.DeliverMessage(messages.Localize("RoleMismatch", this._Channel.Language), this._Channel);
                        return;
                    }
                    if (u.Name == origin.Name)
                    {
                        // users aren't permitted to delete themselve
                        IRC.DeliverMessage(messages.Localize("Trust2", this._Channel.Language), this._Channel);
                        return;
                    }
                    Users.Remove(u);
                    Save();
                    IRC.DeliverMessage(messages.Localize("Trust3", this._Channel.Language), this._Channel);
                    return;
                }
            }
            IRC.DeliverMessage(messages.Localize("Trust4", this._Channel.Language), this._Channel);
            return;
        }
        
        /// <summary>
        /// Gets the global user. Work just as GetUser, but only for global records
        /// </summary>
        /// <returns>
        /// The global user or null in case there is no match
        /// </returns>
        /// <param name='user'>
        /// Identification string against which the user regexes are tested, this is usually a string
        /// in format of 'nick!ident@hostname'
        /// </param>
        public static SystemUser GetGlobalUser(string user)
        {
            SystemUser lv = null;
            int level = 0;
            lock (globalUsers)
            {
                foreach (SystemUser b in globalUsers)
                {
                    Core.RegexCheck id = new Core.RegexCheck(b.Name, user);
                    if (id.IsMatch() == 1)
                    {
                        // if there is multiple records matching the regex, we need to pick that one
                        // with highest privileges
                        int rl = GetLevelOfRole(b.Role);
                        if (rl > level)
                        {
                            level = rl;
                            lv = b;
                        }
                    }
                }
            }
            return lv;
        }
        
        /// <summary>
        /// Return user object from a name
        /// 
        /// Search all global and local records for matching regex, if multiple matches are existing the one with
        /// highest privileges is returned.
        /// </summary>
        /// <param name="user">
        /// Identification string against which the user regexes are tested, this is usually a string
        /// in format of nick!ident@hostname (typical for IRC protocol)
        /// </param>
        /// <returns>
        /// This function always return an instance of SystemUser even if no such a user exists, in that case user
        /// with "null" role is returned, which has no privileges by default
        /// </returns>
        public SystemUser GetUser(string user)
        {
            SystemUser lv = GetGlobalUser(user);
            if (lv == null)
            {
                lv = new SystemUser("null", "");
            }
            int current = GetLevelOfRole(lv.Role);
            lock (Users)
            {
                foreach (SystemUser b in Users)
                {
                    Core.RegexCheck id = new Core.RegexCheck(b.Name, user);
                    if (id.IsMatch() == 1)
                    {
                        int level = GetLevelOfRole(b.Role);
                        if (level > current)
                        {
                            current = level;
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
        public string ListAll()
        {
            string users_ok = "";
            lock (Users)
            {
                foreach (SystemUser b in Users)
                {
                    users_ok += " " + b.Name + " (" + Variables.ColorChar + "2" + b.Role + Variables.ColorChar +")" + ",";
                }
            }
            return users_ok;
        }
        
        private static bool HasPrivilege(string privilege, string role)
        {
            // this is just a performance hack
            if (role == "root")
                return true;
            if (Roles.ContainsKey(role))
            {
                return Roles[role].IsPermitted(privilege);
            }
            return false;
        }
        
        /// <summary>
        /// Check if user is approved to do operation requested
        /// </summary>
        /// <param name="User">Username</param>
        /// <param name="Host">Hostname</param>
        /// <param name="privilege">Approved for specified object / request</param>
        /// <returns></returns>
        public bool IsApproved(string User, string Host, string privilege)
        {
            SystemUser current = GetUser(User + "!@" + Host);
            return HasPrivilege(privilege, current.Role);
        }

        /// <summary>
        /// Check if user is approved to do operation requested
        /// </summary>
        /// <param name="user"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool IsApproved(libirc.UserInfo user, string command)
        {
            return IsApproved(user.Nick, user.Host, command);
        }

        /// <summary>
        /// Return a level of role that this user posses
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public int GetLevel(libirc.UserInfo user)
        {
            SystemUser current = GetUser(user.Nick + "!@" + user.Host);
            return GetLevelOfRole(current.Role);
        }
    }
}
