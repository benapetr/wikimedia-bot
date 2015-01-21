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
using System.Xml;

namespace wmib
{
    /// <summary>
    /// Represent a channel
    /// </summary>
    [Serializable]
    public class Channel : libirc.Target
    {
        /// <summary>
        /// Channel name
        /// </summary>
        public string Name = null;
        /// <summary>
        /// Language used in this channel
        /// </summary>
        public string Language = "en";
        /// <summary>
        /// The irc channel as maintained by IRC library, this is the linked irc channel that is handled
        /// by the instance which is servicing that
        /// </summary>
        public libirc.Channel IrcChannel = null;
        /// <summary>
        /// Whether the channel contains a fresh user list (in case it doesn't bot will auto reparse it from ircd)
        /// </summary>
        public bool HasFreshUserList = false;
        /// <summary>
        /// List of channels that have shared infobot db
        /// </summary>
        public List<string> SharedChans = new List<string>();
        /// <summary>
        /// Objects created by extensions
        /// </summary>
        public Dictionary<string, object> ExtensionObjects = new Dictionary<string, object>();
        private readonly Dictionary<string, string> ExtensionData = new Dictionary<string, string>();
        /// <summary>
        /// If this is true, no messages are sent to this channel
        /// </summary>
        public bool Suppress = false;
        /// <summary>
        /// List of ignored names for infobot
        /// </summary>
        public List<string> Infobot_IgnoredNames = new List<string>();
        /// <summary>
        /// Wait time between responses to users who try to speak to the bot
        /// </summary>
        public int RespondWait = 120;
        /// <summary>
        /// Whether bot should respond to users who think that the bot is user and speak to him
        /// </summary>
        public bool RespondMessage = false;
        /// <summary>
        /// Time of last message received in channel
        /// </summary>
        public DateTime TimeOfLastMsg = DateTime.Now;
        /// <summary>
        /// Doesn't send any warnings on error
        /// </summary>
        public bool SuppressWarnings = false;
        /// <summary>
        /// Whether unknown users should be ignored or not
        /// </summary>
        public bool IgnoreUnknown = false;
        /// <summary>
        /// Target db of shared infobot
        /// </summary>
        public string SharedDB = "";
        /// <summary>
        /// List of channels we share db with
        /// </summary>
        public List<Channel> SharedLinkedChan = new List<Channel>();
        /// <summary>
        /// Default instance this channel belongs to
        /// </summary>
        public string DefaultInstance = "any";
        /// <summary>
        /// Current instance
        /// </summary>
        [NonSerialized]
        public Instance PrimaryInstance = null;
        public override string TargetName
        {
            get
            {
                return this.Name;
            }
        }
        /// <summary>
        /// Users
        /// </summary>
        public Security SystemUsers = null;
        private bool IsRemoved = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="wmib.Channel"/> class.
        /// </summary>
        /// <param name='name'>
        /// Name.
        /// </param>
        public Channel(string name)
        {
            Name = name;
            Suppress = false;
            SystemUsers = new Security(this);
            LoadConfig();
            if (DefaultInstance == "any")
            {
                PrimaryInstance = Instance.GetInstance();
                // we need to save the instance so that next time bot reconnect to bouncer it uses the same instance
                DefaultInstance = PrimaryInstance.Nick;
                SaveConfig();
            }
            else
            {
                if (!Instance.Instances.ContainsKey(DefaultInstance))
                {
                    Syslog.WarningLog("There is no instance " + DefaultInstance + " reassigning channel " + this.Name +
                                      " to a different instance");
                    this.PrimaryInstance = Instance.GetInstance();
                    Syslog.Log("Reassigned to " + this.PrimaryInstance.Nick);
                }
                else
                {
                    PrimaryInstance = Instance.Instances[DefaultInstance];
                }
            }
            if (!Directory.Exists(Configuration.WebPages.HtmlPath))
                Directory.CreateDirectory(Configuration.WebPages.HtmlPath);

            foreach (Module module in ExtensionHandler.ExtensionList)
            {
                try
                {
                    if (module.IsWorking)
                    {
                        Channel self = this;
                        module.Hook_Channel(self);
                    }
                }
                catch (Exception fail)
                {
                    Syslog.Log("MODULE: exception at Hook_Channel in " + module.Name, true);
                    Core.HandleException(fail);
                }
            }
        }

        /// <summary>
        /// Returns true if this channel is already existing in memory
        /// </summary>
        /// <param name="_Channel"></param>
        /// <returns></returns>
        public static bool ConfigExists(string _Channel)
        {
            return File.Exists(GetConfigFilePath(_Channel));
        }

        public static string GetConfigFilePath(string _Channel)
        {
            return Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + _Channel + ".xml";
        }

        public string GetConfigFilePath()
        {
            return GetConfigFilePath(this.Name);
        }

        public libirc.Channel GetChannel()
        {
            if (this.IrcChannel == null)
                this.RefetchChannel();

            return this.IrcChannel;
        }

        protected void RefetchChannel()
        {
            if (this.IrcChannel == null)
                this.IrcChannel = this.PrimaryInstance.Network.GetChannel(this.Name);
        }

        /// <summary>
        /// Change the config
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public void Extension_SetConfig(string name, string data)
        {
            lock (ExtensionData)
            {
                if (ExtensionData.ContainsKey(name))
                {
                    ExtensionData[name] = data;
                    return;
                }
            }
            ExtensionData.Add(name, data);
        }

        /// <summary>
        /// Retrieve a config
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Extension_GetConfig(string key, string missing = null)
        {
            lock (ExtensionData)
            {
                if (ExtensionData.ContainsKey(key))
                {
                    return ExtensionData[key];
                }
            }
            return missing;
        }

        /// <summary>
        /// Get an object created by extension of name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object RetrieveObject(string name)
        {
            try
            {
                lock (ExtensionObjects)
                {
                    if (ExtensionObjects.ContainsKey(name))
                    {
                        return ExtensionObjects[name];
                    }
                }
            }
            catch (Exception er)
            {
                Core.HandleException(er);
            }
            return null;
        }

        /// <summary>
        /// Remove object from memory
        /// </summary>
        /// <param name="Nm">Name of object</param>
        /// <returns></returns>
        public bool UnregisterObject(string Nm)
        {
            try
            {
                lock (ExtensionObjects)
                {
                    if (!ExtensionObjects.ContainsKey(Nm))
                    {
                        return true;
                    }
                    ExtensionObjects.Remove(Nm);
                    return true;
                }
            }
            catch (Exception er)
            {
                Core.HandleException(er);
            }
            return false;
        }

        /// <summary>
        /// Register a new object in memory
        /// </summary>
        /// <param name="Ob">Data</param>
        /// <param name="Nm">Name</param>
        /// <returns></returns>
        public bool RegisterObject(object Ob, string Nm)
        {
            try
            {
                lock (ExtensionObjects)
                {
                    if (ExtensionObjects.ContainsKey(Nm))
                    {
                        return false;
                    }
                    ExtensionObjects.Add(Nm, Ob);
                    return true;
                }
            }
            catch (Exception er)
            {
                Core.HandleException(er);
                return false;
            }
        }

        public libirc.User RetrieveUser(string nick)
        {
            this.RefetchChannel();
            if (this.IrcChannel == null)
            {
                return null;
            }
            return this.IrcChannel.UserFromName(nick);
        }

        public void LoadConfig()
        {
            string conf_file = GetConfigFilePath();
            Core.RecoverFile(conf_file, Name);
            try
            {
                XmlDocument data = new XmlDocument();
                if (!File.Exists(conf_file))
                {
                    SaveConfig();
                    return;
                }
                data.Load(conf_file);
                foreach (XmlNode xx in data.ChildNodes[0].ChildNodes)
                {
                    switch (xx.Name)
                    {
                        case "user":
                            this.SystemUsers.InsertUser(xx);
                            continue;
                        case "extension":
                            if (ExtensionData.ContainsKey(xx.Attributes[0].Value))
                            {
                                ExtensionData[xx.Attributes[0].Value] = xx.Attributes[1].Value;
                            }
                            else
                            {
                                ExtensionData.Add(xx.Attributes[0].Value, xx.Attributes[1].Value);
                            }
                            continue;
                        case "ignored":
                            Infobot_IgnoredNames.Add(xx.Attributes[1].Value);
                            continue;
                        case "sharedch":
                            SharedChans.Add(xx.Attributes[1].Value);
                            continue;

                    }
                    switch (xx.Attributes[0].Value)
                    {
                        case "talkmode":
                            Suppress = bool.Parse(xx.Attributes[1].Value);
                            break;
                        case "langcode":
                            Language = xx.Attributes[1].Value;
                            break;
                        case "respond_message":
                            RespondMessage = bool.Parse(xx.Attributes[1].Value);
                            break;
                        case "ignore-unknown":
                            IgnoreUnknown = bool.Parse(xx.Attributes[1].Value);
                            break;
                        case "suppress-warnings":
                            SuppressWarnings = bool.Parse(xx.Attributes[1].Value);
                            break;
                        case "respond_wait":
                            RespondWait = int.Parse(xx.Attributes[1].Value);
                            break;
                        case "sharedinfo":
                            SharedDB = xx.Attributes[1].Value;
                            break;
                        case "defaultbot":
                            DefaultInstance = xx.Attributes[1].Value;
                            break;
                    }
                }
            }
            catch (Exception fail)
            {
                Syslog.Log("Unable to load the config of " + Name, true);
                Core.HandleException(fail);
            }
        }

        private static void InsertData(string key, string value, ref XmlDocument document, ref XmlNode node, string Name = "local")
        {
            XmlAttribute name = document.CreateAttribute("key");
            name.Value = key;
            XmlAttribute kk = document.CreateAttribute("value");
            kk.Value = value;
            XmlNode db = document.CreateElement(Name);
            db.Attributes.Append(name);
            db.Attributes.Append(kk);
            node.AppendChild(db);
        }

        /// <summary>
        /// Save config
        /// </summary>
        public void SaveConfig()
        {
            string fn = GetConfigFilePath();
            try
            {
                XmlDocument data = new XmlDocument();
                XmlNode xmlnode = data.CreateElement("channel");
                InsertData("talkmode", Suppress.ToString(), ref data, ref xmlnode);
                InsertData("langcode", Language, ref data, ref xmlnode);
                InsertData("respond_message", RespondMessage.ToString(), ref data, ref xmlnode);
                InsertData("ignore-unknown", IgnoreUnknown.ToString(), ref data, ref xmlnode);
                InsertData("suppress-warnings", SuppressWarnings.ToString(), ref data, ref xmlnode);
                InsertData("respond_wait", RespondWait.ToString(), ref data, ref xmlnode);
                InsertData("sharedinfo", SharedDB, ref data, ref xmlnode);
                InsertData("defaultbot", DefaultInstance, ref data, ref xmlnode);
                if (!(SharedLinkedChan.Count < 1))
                    foreach (Channel current in SharedLinkedChan)
                        InsertData("name", current.Name, ref data, ref xmlnode, "sharedch");

                if (!(Infobot_IgnoredNames.Count < 1))
                    foreach (string curr in Infobot_IgnoredNames)
                        InsertData("name", curr, ref data, ref xmlnode, "ignored");

                if (ExtensionData.Count > 0)
                    foreach (KeyValuePair<string, string> item in ExtensionData)
                        InsertData(item.Key, item.Value, ref data, ref xmlnode, "extension");

                lock (this.SystemUsers.Users)
                {
                    foreach (SystemUser user in this.SystemUsers.Users)
                    {
                        XmlAttribute name = data.CreateAttribute("regex");
                        name.Value = user.Name;
                        XmlAttribute kk = data.CreateAttribute("role");
                        kk.Value = user.Role;
                        XmlNode db = data.CreateElement("user");
                        db.Attributes.Append(name);
                        db.Attributes.Append(kk);
                        xmlnode.AppendChild(db);
                    }
                }
                if (File.Exists(fn))
                {
                    Core.BackupData(fn);
                    if (!File.Exists(Configuration.TempName(fn)))
                    {
                        Syslog.WarningLog("Unable to create backup file for " + Name);
                    }
                }
                data.AppendChild(xmlnode);
                data.Save(fn);
                if (File.Exists(Configuration.TempName(fn)))
                    File.Delete(Configuration.TempName(fn));
            }
            catch (Exception)
            {
                Core.RecoverFile(fn, Name);
            }
        }

        /// <summary>
        /// Remove all refs
        /// </summary>
        public void Remove()
        {
            if (IsRemoved)
            {
                Syslog.DebugLog("Channel is already removed");
                return;
            }
            SystemUsers = null;
            lock (ExtensionData)
            {
                ExtensionData.Clear();
            }
            lock (ExtensionObjects)
            {
                ExtensionObjects.Clear();
            }
            SharedDB = null;
            SharedChans.Clear();
            SharedLinkedChan.Clear();
            if (Configuration.Channels.Contains(this))
                Configuration.Channels.Remove(this);
        }

        /// <summary>
        /// Return true if user is present in channel
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsUser(string name)
        {
            this.RefetchChannel();
            if (this.IrcChannel != null)
                return this.IrcChannel.ContainsUser(name);
            else
                Syslog.DebugLog("IrcChannel is NULL: " + Name);
            return false;
        }

        /// <summary>
        /// Return number of channels that infobot share db with
        /// </summary>
        /// <returns></returns>
        public int InitializeShares()
        {
            foreach (string x in SharedChans)
            {
                Channel channel = Core.GetChannel(x.Trim());
                if (channel != null && !SharedLinkedChan.Contains(channel))
                    SharedLinkedChan.Add(channel);
            }
            return 0;
        }
    }
}

