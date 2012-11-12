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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;

namespace wmib
{
    [Serializable()]
    public class RegularModule : Module
    {
        public List<infobot_core.InfoItem> jobs = new List<infobot_core.InfoItem>();
        public static bool running;
        public bool Unwritable;
        public bool Disabled;
        public infobot_writer writer = null;

        public override bool Hook_OnUnload()
        {
            bool success = true;
            if (writer != null)
            {
                writer.Exit();
                writer = null;
            }
            lock (config.channels)
            {
                foreach (config.channel channel in config.channels)
                {
                    if (!channel.UnregisterObject("Infobot"))
                    {
                        success = false;
                    }
                }
            }
            if (!success)
            {
                core.Log("Failed to unregister infobot objects in some channels", true);
            }
            return success;
        }

        public string getDB(config.channel chan)
        {
            return Module.GetConfig(chan, "Infobot.Keydb", (string)variables.config + Path.DirectorySeparatorChar + chan.Name + ".db");
        }

        public override void Hook_Channel(config.channel channel)
        {
            if (channel.RetrieveObject("Infobot") == null)
            {
                channel.RegisterObject(new infobot_core(getDB(channel), channel.Name), "Infobot");
            }
        }

        public override bool Hook_OnRegister()
        {
            bool success = true;
            writer = new infobot_writer();
            lock (config.channels)
            {
                foreach (config.channel channel in config.channels)
                {
                    if (!channel.RegisterObject(new infobot_core(getDB(channel), channel.Name), "Infobot"))
                    {
                        success = false;
                    }
                }
            }
            if (!success)
            {
                core.Log("Failed to register infobot objects in some channels", true);
            }
            return success;
        }

        public override string Extension_DumpHtml(config.channel channel)
        {
            string HTML = "";
            infobot_core info = (infobot_core)channel.RetrieveObject("Infobot");
            if (info != null)
            {
                HTML += "\n<table border=1 class=\"infobot\" width=100%>\n<tr><th width=10%>Key</th><th>Value</th></tr>\n";
                List<infobot_core.item> list = new List<infobot_core.item>();
                info.locked = true;
                lock (info.text)
                {
                    if (Module.GetConfig(channel, "Infobot.Sorted", false) != false)
                    {
                        list = info.SortedItem();
                    }
                    else
                    {
                        list.AddRange(info.text);
                    }
                }
                if (info.text.Count > 0)
                {
                    foreach (infobot_core.item Key in list)
                    {
                        HTML += core.HTML.AddKey(Key.key, Key.text);
                    }
                }
                HTML += "</table>\n";
                HTML += "<h4>Aliases</h4>\n<table class=\"infobot\" border=1 width=100%>\n";
                lock (info.Alias)
                {
                    foreach (infobot_core.staticalias data in info.Alias)
                    {
                        HTML += core.HTML.AddLink(data.Name, data.Key);
                    }
                }
                HTML += "</table><br>\n";
                info.locked = false;
            }
            return HTML;
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            // "\uff01" is the full-width version of "!".
            if ((message.StartsWith("!") || message.StartsWith("\uff01")) && GetConfig(channel, "Infobot.Enabled", true))
            {
                while (Unwritable)
                {
                    Thread.Sleep(10);
                }
                Unwritable = true;
                infobot_core.InfoItem item = new infobot_core.InfoItem();
                item.Channel = channel;
                item.Name = "!" + message.Substring(1); // Normalizing "!".
                item.User = invoker.Nick;
                item.Host = invoker.Host;
                jobs.Add(item);
                Unwritable = false;
            }

            infobot_core infobot = null;

            if (message.StartsWith("@"))
            {
                infobot = (infobot_core)channel.RetrieveObject("Infobot");
                if (infobot == null)
                {
                    core.Log("Object Infobot in " + channel.Name + " doesn't exist", true);
                }
                if (GetConfig(channel, "Infobot.Enabled", true))
                {
                    if (infobot != null)
                    {
                        infobot.Find(message, channel);
                        infobot.RSearch(message, channel);
                    }
                }
            }

            if (message.StartsWith("@infobot-share-trust+ "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (channel.shared != "local")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot16", channel.Language), channel.Name);
                        return;
                    }
                    if (channel.shared != "local" && channel.shared != "")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot15", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        if (message.Length <= "@infobot-share-trust+ ".Length)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db6", channel.Language), channel.Name);
                            return;
                        }
                        string name = message.Substring("@infobot-share-trust+ ".Length);
                        config.channel guest = core.getChannel(name);
                        if (guest == null)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db8", channel.Language), channel.Name);
                            return;
                        }
                        if (channel.sharedlink.Contains(guest))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db14", channel.Language), channel.Name);
                            return;
                        }
                        core.irc._SlowQueue.DeliverMessage(messages.get("db1", channel.Language, new List<string> { name }), channel.Name);
                        channel.sharedlink.Add(guest);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith("@infobot-ignore- "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "trust"))
                {
                    string item = message.Substring("@infobot-ignore+ ".Length);
                    if (item != "")
                    {
                        if (!channel.Infobot_IgnoredNames.Contains(item))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-found", channel.Language, new List<string> { item }), channel.Name);
                            return;
                        }
                        channel.Infobot_IgnoredNames.Remove(item);
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-rm", channel.Language, new List<string> { item }), channel.Name);
                        channel.SaveConfig();
                        return;
                    }
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
            }

            if (message.StartsWith("@infobot-ignore+ "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "trust"))
                {
                    string item = message.Substring("@infobot-ignore+ ".Length);
                    if (item != "")
                    {
                        if (channel.Infobot_IgnoredNames.Contains(item))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-exist", channel.Language, new List<string> { item }), channel.Name);
                            return;
                        }
                        channel.Infobot_IgnoredNames.Add(item);
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-ok", channel.Language, new List<string> { item }), channel.Name);
                        channel.SaveConfig();
                        return;
                    }
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
            }

            if (message == "@infobot-off")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!GetConfig(channel, "Infobot.Enabled", true))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot1", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot2", channel.Language), channel.Name, IRC.priority.high);
                        SetConfig(channel, "Infobot.Enabled", false);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith("@infobot-share-trust- "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (channel.shared != "local")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot16", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        if (message.Length <= "@infobot-share-trust+ ".Length)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db6", channel.Language), channel.Name);
                            return;
                        }
                        string name = message.Substring("@infobot-share-trust- ".Length);
                        config.channel target = core.getChannel(name);
                        if (target == null)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db8", channel.Language), channel.Name);
                            return;
                        }
                        if (channel.sharedlink.Contains(target))
                        {
                            channel.sharedlink.Remove(target);
                            core.irc._SlowQueue.DeliverMessage(messages.get("db2", channel.Language, new List<string> { name }), channel.Name);
                            channel.SaveConfig();
                            return;
                        }
                        core.irc._SlowQueue.DeliverMessage(messages.get("db4", channel.Language), channel.Name);
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith("@infobot-detail "))
            {
                if ((message.Length) <= "@infobot-detail ".Length)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("db6", channel.Language), channel.Name);
                    return;
                }
                if (GetConfig(channel, "Infobot.Enabled", true))
                {
                    if (channel.shared == "local" || channel.shared == "")
                    {
                        if (infobot != null)
                        {
                            infobot.Info(message.Substring(16), channel);
                        }
                        return;
                    }
                    if (channel.shared != "")
                    {
                        config.channel db = core.getChannel(channel.shared);
                        if (db == null)
                        {
                            core.irc._SlowQueue.DeliverMessage("Error, null pointer to shared channel", channel.Name, IRC.priority.low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.Info(message.Substring(16), channel);
                        }
                        return;
                    }
                    return;
                }
                core.irc._SlowQueue.DeliverMessage("Infobot is not enabled on this channel", channel.Name, IRC.priority.low);
                return;
            }

            if (message.StartsWith("@infobot-link "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (channel.shared == "local")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot17", channel.Language), channel.Name);
                        return;
                    }
                    if (channel.shared != "")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot18", channel.Language, new List<string> { channel.shared }), channel.Name);
                        return;
                    }
                    if ((message.Length - 1) < "@infobot-link ".Length)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("db6", channel.Language), channel.Name);
                        return;
                    }
                    string name = message.Substring("@infobot-link ".Length);
                    config.channel db = core.getChannel(name);
                    if (db == null)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("db8", channel.Language), channel.Name);
                        return;
                    }
                    if (!infobot_core.Linkable(db, channel))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("db9", channel.Language), channel.Name);
                        return;
                    }
                    channel.shared = name.ToLower();
                    core.irc._SlowQueue.DeliverMessage(messages.get("db10", channel.Language), channel.Name);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@infobot-share-off")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (channel.shared == "")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot14", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot13", channel.Language), channel.Name);
                        foreach (config.channel curr in config.channels)
                        {
                            if (curr.shared == channel.Name.ToLower())
                            {
                                curr.shared = "";
                                curr.SaveConfig();
                                core.irc._SlowQueue.DeliverMessage(messages.get("infobot19", curr.Language, new List<string> { invoker.Nick }), curr.Name);
                            }
                        }
                        channel.shared = "";
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@infobot-on")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (GetConfig(channel, "Infobot.Enabled", true))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot3", channel.Language), channel.Name);
                        return;
                    }
                    SetConfig(channel, "Infobot.Enabled", true);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage(messages.get("infobot4", channel.Language), channel.Name, IRC.priority.high);
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@infobot-share-on")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (channel.shared == "local")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot11", channel.Language), channel.Name, IRC.priority.high);
                        return;
                    }
                    if (channel.shared != "local" && channel.shared != "")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot15", channel.Language), channel.Name, IRC.priority.high);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot12", channel.Language), channel.Name);
                        channel.shared = "local";
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }
        }

        public override bool Hook_SetConfig(config.channel chan, User invoker, string config, string value)
        {
            bool _temp_a;
            switch (config)
            {
                case "infobot-trim-white-space-in-name":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.Trim-white-space-in-name", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-auto-complete":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.auto-complete", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-sorted":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.Sorted", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-help":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.Help", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
            }
            return false;
        }

        public override bool Construct()
        {
            Version = "1.0.2";
            base.Create("Infobot core", true, true);
            return true;
        }

        public override void Hook_ReloadConfig(config.channel chan)
        {
            if (chan.ExtensionObjects.ContainsKey("Infobot"))
            {
                chan.ExtensionObjects["Infobot"] = new infobot_core(getDB(chan), chan.Name);
            }
        }

        public override void Load()
        {
            try
            {
                Unwritable = false;
                while (Disabled != true)
                {
                    if (Unwritable)
                    {
                        Thread.Sleep(200);
                    }
                    else if (jobs.Count > 0)
                    {
                        Unwritable = true;
                        List<infobot_core.InfoItem> list = new List<infobot_core.InfoItem>();
                        list.AddRange(jobs);
                        jobs.Clear();
                        Unwritable = false;
                        foreach (infobot_core.InfoItem item in list)
                        {
                            infobot_core infobot = (infobot_core)item.Channel.RetrieveObject("Infobot");
                            if (infobot != null)
                            {
                                infobot.print(item.Name, item.User, item.Channel, item.Host);
                            }
                        }
                    }
                    Thread.Sleep(200);
                }
            }
            catch (Exception b)
            {
                Unwritable = false;
                Console.WriteLine(b.InnerException);
            }
            return;
        }
    }

    [Serializable()]
    public class infobot_writer : Module
    {
        public override bool Construct()
        {
            Version = "1.0.0";
            base.Create("Infobot DB", true, true);
            return true;
        }

        public override void Load()
        {
            try
            {
                while (true)
                {
                    SaveData();
                    Thread.Sleep(2000);
                }
            }
            catch (ThreadAbortException)
            {
                SaveData();
            }
        }

        public void SaveData()
        {
            lock (config.channels)
            {
                foreach (config.channel x in config.channels)
                {
                    infobot_core infobot = (infobot_core)x.RetrieveObject("Infobot");
                    if (infobot != null)
                    {
                        if (infobot.stored == false)
                        {
                            infobot.stored = true;
                            infobot.Save();
                        }
                    }
                }
            }
        }
    }

    public class infobot_core
    {
        /// <summary>
        /// Data file
        /// </summary>
        public string datafile_raw = "";
        public string datafile_xml = "";
        public bool stored = true;

        [NonSerialized()]
        Thread Th;

        // if we need to update dump
        public bool update = true;

        /// <summary>
        /// Locked
        /// </summary>
        public bool locked = false;

        public static config.channel Reply;

        public static DateTime NA = DateTime.MaxValue;

        public class item
        {
            /// <summary>
            /// Text
            /// </summary>
            public string text;

            /// <summary>
            /// Key
            /// </summary>
            public string key;

            public string user;

            public string locked;

            public DateTime created;

            public int Displayed = 0;

            public DateTime lasttime;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="Key">Key</param>
            /// <param name="Text">Text of the key</param>
            /// <param name="User">User who created the key</param>
            /// <param name="Lock">If key is locked or not</param>
            public item(string Key, string Text, string User, string Lock = "false", string date = "", string time = "", int Number = 0)
            {
                text = Text;
                key = Key;
                locked = Lock;
                user = User;
                Displayed = Number;
                if (time == "")
                {
                    lasttime = NA;
                }
                else
                {
                    lasttime = DateTime.FromBinary(long.Parse(time));
                }
                if (date == "")
                {
                    created = DateTime.Now;
                }
                else
                {
                    created = DateTime.FromBinary(long.Parse(date));
                }
            }
        }

        public class staticalias
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name;

            /// <summary>
            /// Key
            /// </summary>
            public string Key;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">Alias</param>
            /// <param name="key">Key</param>
            public staticalias(string name, string key)
            {
                Name = name;
                Key = key;
            }
        }

        public class InfoItem
        {
            public config.channel Channel;
            public string User;
            public string Name;
            public string Host;
        }

        /// <summary>
        /// List of all items in class
        /// </summary>
        public List<item> text = new List<item>();

        /// <summary>
        /// List of all aliases we want to use
        /// </summary>
        public List<staticalias> Alias = new List<staticalias>();

        /// <summary>
        /// Channel name
        /// </summary>
        public string Channel;

        private string search_key;



        /// <summary>
        /// Load it
        /// </summary>
        public bool Load()
        {
            text.Clear();
            // Checking if db isn't broken
            core.recoverFile(datafile_raw, Channel);
            if (!File.Exists(datafile_raw))
            {
                return false;
            }

            string[] db = File.ReadAllLines(datafile_raw);
            foreach (string x in db)
            {
                if (x.Contains(config.separator))
                {
                    string[] info = x.Split(Char.Parse(config.separator));
                    string type = info[2];
                    string value = info[1];
                    string name = info[0];
                    if (type == "key")
                    {
                        string Locked = info[3];
                        text.Add(new item(name.Replace("<separator>", "|"), value.Replace("<separator>", "|"), "", Locked, NA.ToBinary().ToString(),
                            NA.ToBinary().ToString()));
                    }
                    else
                    {
                        Alias.Add(new staticalias(name.Replace("<separator>", "|"), value.Replace("<separator>", "|")));
                    }
                }
            }
            return true;
        }

        public bool LoadData()
        {
            text.Clear();
            // Checking if db isn't broken
            core.recoverFile(datafile_xml, Channel);
            if (Load())
            {
                core.Log("Obsolete database found for " + Channel + " converting to new format");
                Save();
                File.Delete(datafile_raw);
                return true;
            }
            if (!File.Exists(datafile_xml))
            {
                // Create db
                Save();
                return true;
            }
            try
            {
                System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                if (!File.Exists(datafile_xml))
                {
                    text.Clear();
                    return true;
                }
                data.Load(datafile_xml);
                lock (text)
                {
                    lock (Alias)
                    {
                        text.Clear();
                        Alias.Clear();
                        foreach (System.Xml.XmlNode xx in data.ChildNodes[0].ChildNodes)
                        {
                            if (xx.Name == "alias")
                            {
                                staticalias _Alias = new staticalias(xx.Attributes[0].Value, xx.Attributes[1].Value);
                                Alias.Add(_Alias);
                                continue;
                            }
                            item _key = new item(xx.Attributes[0].Value, xx.Attributes[1].Value, xx.Attributes[2].Value, "false", xx.Attributes[3].Value,
                            xx.Attributes[4].Value, int.Parse(xx.Attributes[5].Value));
                            text.Add(_key);
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return true;
        }

        public void Info(string key, config.channel chan)
        {
            foreach (item CV in text)
            {
                if (CV.key == key)
                {
                    string created = "N/A";
                    string last = "N/A";
                    string name = "N/A";
                    if (CV.lasttime != NA)
                    {
                        TimeSpan span = DateTime.Now - CV.lasttime;
                        last = CV.lasttime.ToString() + " (" + span.ToString() + " ago)";
                    }
                    if (CV.created != NA)
                    {
                        created = CV.created.ToString();
                    }
                    if (CV.user != "")
                    {
                        name = CV.user;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("infobot-data", chan.Language, new List<string> {key, name, created, CV.Displayed.ToString(),
                        last }), chan.Name, IRC.priority.low);
                    return;
                }
            }
            core.irc._SlowQueue.DeliverMessage("There is no such a key", chan.Name, IRC.priority.low);
        }

        public List<item> SortedItem()
        {
            List<item> OriginalList = new List<item>();
            List<item> Item = new List<item>();
            locked = true;
            OriginalList.AddRange(text);
            locked = false;
            try
            {
                if (text.Count > 0)
                {
                    List<string> Name = new List<string>();
                    foreach (item curr in OriginalList)
                    {
                        Name.Add(curr.key);
                    }
                    Name.Sort();
                    foreach (string f in Name)
                    {
                        foreach (item g in OriginalList)
                        {
                            if (f == g.key)
                            {
                                Item.Add(g);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                core.Log("Exception while creating list for html");
                locked = false;
            }
            return Item;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="database"></param>
        /// <param name="channel"></param>
        public infobot_core(string database, string channel)
        {
            datafile_xml = database + ".xml";
            datafile_raw = database;
            Channel = channel;
            LoadData();
        }

        public static string parseInfo(string key, string[] pars)
        {
            string keyv = key;
            if (pars.Length > 1)
            {
                string keys = "";
                int curr = 1;
                while (pars.Length > curr)
                {
                    keyv = keyv.Replace("$" + curr.ToString(), pars[curr]);
                    keyv = keyv.Replace("$url_encoded_" + curr.ToString(), System.Web.HttpUtility.UrlEncode(pars[curr]));
                    if (keys == "")
                    {
                        keys = pars[curr];
                    }
                    else
                    {
                        keys = keys + " " + pars[curr];
                    }
                    curr++;
                }
                keyv = keyv.Replace("$*", keys);
                keyv = keyv.Replace("$url_encoded_*", System.Web.HttpUtility.UrlEncode(keys));
            }
            return keyv;
        }

        public static bool Linkable(config.channel host, config.channel guest)
        {
            if (host == null)
            {
                return false;
            }
            if (guest == null)
            {
                return false;
            }
            if (host.sharedlink.Contains(guest))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Save to a file
        /// </summary>
        public void Save()
        {
            update = true;
            try
            {
                if (File.Exists(datafile_xml))
                {
                    core.backupData(datafile_xml);
                    if (!File.Exists(config.tempName(datafile_xml)))
                    {
                        core.Log("Unable to create backup file for " + this.Channel);
                    }
                }
                System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                System.Xml.XmlNode xmlnode = data.CreateElement("database");

                lock (Alias)
                {
                    foreach (staticalias key in Alias)
                    {
                        System.Xml.XmlAttribute name = data.CreateAttribute("alias_key_name");
                        name.Value = key.Name;
                        System.Xml.XmlAttribute kk = data.CreateAttribute("alias_key_key");
                        kk.Value = key.Key;
                        System.Xml.XmlAttribute created = data.CreateAttribute("date");
                        created.Value = "";
                        System.Xml.XmlNode db = data.CreateElement("alias");
                        db.Attributes.Append(name);
                        db.Attributes.Append(kk);
                        db.Attributes.Append(created);
                        xmlnode.AppendChild(db);
                    }
                }
                lock (text)
                {
                    foreach (item key in text)
                    {
                        System.Xml.XmlAttribute name = data.CreateAttribute("key_name");
                        name.Value = key.key;
                        System.Xml.XmlAttribute kk = data.CreateAttribute("data");
                        kk.Value = key.text;
                        System.Xml.XmlAttribute created = data.CreateAttribute("created_date");
                        created.Value = key.created.ToBinary().ToString();
                        System.Xml.XmlAttribute nick = data.CreateAttribute("nickname");
                        nick.Value = key.user;
                        System.Xml.XmlAttribute last = data.CreateAttribute("touched");
                        last.Value = key.lasttime.ToBinary().ToString();
                        System.Xml.XmlAttribute triggered = data.CreateAttribute("triggered");
                        triggered.Value = key.Displayed.ToString();
                        System.Xml.XmlNode db = data.CreateElement("key");
                        db.Attributes.Append(name);
                        db.Attributes.Append(kk);
                        db.Attributes.Append(nick);
                        db.Attributes.Append(created);
                        db.Attributes.Append(last);
                        db.Attributes.Append(triggered);
                        xmlnode.AppendChild(db);
                    }
                }

                data.AppendChild(xmlnode);
                data.Save(datafile_xml);
                if (File.Exists(config.tempName(datafile_xml)))
                {
                    File.Delete(config.tempName(datafile_xml));
                }
            }
            catch (Exception b)
            {
                try
                {
                    if (core.recoverFile(datafile_xml, Channel))
                    {
                        core.Log("Recovered db for channel " + Channel);
                    }
                    else
                    {
                        core.handleException(b, Channel);
                    }
                }
                catch (Exception bb)
                {
                    core.handleException(bb, Channel);
                }
            }
        }

        /// <summary>
        /// Get value of key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns></returns>
        public string getValue(string key)
        {
            foreach (item data in text)
            {
                if (data.key == key)
                {
                    data.lasttime = DateTime.Now;
                    data.Displayed++;
                    stored = false;
                    return data.text;
                }
            }
            return "";
        }

        /// <summary>
        /// Print a value to channel if found, this message doesn't need to be a valid command for it to work
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="user">User</param>
        /// <param name="chan">Channel</param>
        /// <param name="host">Host name</param>
        /// <returns></returns>
        public bool print(string name, string user, config.channel chan, string host)
        {
            try
            {
                if (!name.StartsWith("!"))
                {
                    return true;
                }
                config.channel data = isAllowed(chan);
                bool Allowed = (data != null);
                name = name.Substring(1);
                infobot_core infobot = (infobot_core)data.RetrieveObject("Infobot");
                string ignore_test = name;
                if (ignore_test.Contains(" "))
                {
                    ignore_test = ignore_test.Substring(0, ignore_test.IndexOf(" "));
                }
                if (chan.Infobot_IgnoredNames.Contains(ignore_test))
                {
                    return true;
                }
                if (name.Contains(" "))
                {
                    string[] parm = name.Split(' ');
                    if (parm[1] == "is")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            if (parm.Length < 3)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage(messages.get("key", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            string key = name.Substring(name.IndexOf(" is") + 4);
                            if (parm[0].Contains("|"))
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage("Invalid symbol in the key", chan.Name);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.setKey(key, parm[0], user, chan);
                            }
                        }
                        else
                        {
                            if (!chan.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                            }
                        }
                        return false;
                    }
                    if (parm[1] == "alias")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            if (parm.Length < 3)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc.Message(messages.get("InvalidAlias", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.aliasKey(name.Substring(name.IndexOf(" alias") + 7), parm[0], "", chan);
                            }
                        }
                        else
                        {
                            if (!chan.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                            }
                        }
                        return false;
                    }
                    if (parm[1] == "unalias")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                if (!chan.suppress_warnings)
                                {
                                    core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                lock (infobot.Alias)
                                {
                                    foreach (staticalias b in infobot.Alias)
                                    {
                                        if (b.Name == parm[0])
                                        {
                                            infobot.Alias.Remove(b);
                                            core.irc.Message(messages.get("AliasRemoved", chan.Language), chan.Name);
                                            infobot.stored = false;
                                            return false;
                                        }
                                    }
                                }
                            }
                            return false;
                        }
                        if (!chan.suppress_warnings)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                        }
                        return false;
                    }
                    if (parm[1] == "del")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.rmKey(parm[0], "", chan);
                            }
                        }
                        else
                        {
                            if (!chan.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                            }
                        }
                        return false;
                    }
                }
                if (!Allowed)
                {
                    return true;
                }
                string User = "";
                if (name.Contains("|"))
                {
                    User = name.Substring(name.IndexOf("|") + 1);
                    if (Module.GetConfig(chan, "Infobot.Trim-whitespace-in-name", false))
                    {
                        if (User.EndsWith(" "))
                        {
                            while (User.EndsWith(" "))
                            {
                                User = User.Substring(0, User.Length - 1);
                            }
                        }
                        if (User.StartsWith(" "))
                        {
                            while (User.StartsWith(" "))
                            {
                                User = User.Substring(1);
                            }
                        }
                    }
                    name = name.Substring(0, name.IndexOf("|"));
                }
                string[] p = name.Split(' ');
                int parameters = p.Length;
                string keyv = "";
                if (infobot != null)
                {
                    keyv = infobot.getValue(p[0]);
                }
                if (keyv != "")
                {
                    keyv = parseInfo(keyv, p);
                    if (User == "")
                    {
                        core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                    }
                    return true;
                }
                if (infobot != null)
                {
                    foreach (staticalias b in infobot.Alias)
                    {
                        if (b.Name == p[0])
                        {
                            keyv = infobot.getValue(b.Key);
                            if (keyv != "")
                            {
                                keyv = parseInfo(keyv, p);
                                if (User == "")
                                {
                                    core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                                }
                                else
                                {
                                    core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                                }
                                return true;
                            }
                        }
                    }
                }
                if (Module.GetConfig(chan, "Infobot.auto-complete", false))
                {
                    if (infobot != null)
                    {
                        List<string> results = new List<string>();
                        foreach (item f in infobot.text)
                        {
                            if (!results.Contains(f.key) && f.key.StartsWith(p[0]))
                            {
                                results.Add(f.key);
                            }
                        }
                        foreach (staticalias f in infobot.Alias)
                        {
                            if (!results.Contains(f.Key) && f.Key.StartsWith(p[0]))
                            {
                                results.Add(f.Key);
                            }
                        }

                        if (results.Count == 1)
                        {
                            keyv = infobot.getValue(results[0]);
                            if (keyv != "")
                            {
                                keyv = parseInfo(keyv, p);
                                if (User == "")
                                {
                                    core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                                }
                                else
                                {
                                    core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                                }
                                return true;
                            }
                            foreach (staticalias alias in infobot.Alias)
                            {
                                if (alias.Name == p[0])
                                {
                                    keyv = infobot.getValue(alias.Key);
                                    if (keyv != "")
                                    {
                                        keyv = parseInfo(keyv, p);
                                        if (User == "")
                                        {
                                            core.irc._SlowQueue.DeliverMessage(keyv, chan.Name);
                                        }
                                        else
                                        {
                                            core.irc._SlowQueue.DeliverMessage(User + ": " + keyv, chan.Name);
                                        }
                                        return true;
                                    }
                                }
                            }
                        }

                        if (results.Count > 1)
                        {
                            if (Module.GetConfig(chan, "Infobot.Sorted", false))
                            {
                                results.Sort();
                            }
                            string x = "";
                            foreach (string ix in results)
                            {
                                x += ix + ", ";
                            }
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot-c-e", chan.Language, new List<string>() { x }), chan.Name);
                            return true;
                        }
                    }
                }

                if (Module.GetConfig(chan, "Infobot.Help", false) && infobot != null)
                {
                    List<string> Sugg = new List<string>();
                    p[0] = p[0].ToLower();
                    foreach (item f in infobot.text)
                    {
                        if (!Sugg.Contains(f.key) && (f.text.Contains(p[0]) || f.key.ToLower().Contains(p[0])))
                        {
                            Sugg.Add(f.key);
                        }
                    }

                    if (Sugg.Count > 0)
                    {
                        string x = "";
                        if (Module.GetConfig(chan, "Infobot.Sorted", false))
                        {
                            Sugg.Sort();
                        }
                        foreach (string a in Sugg)
                        {
                            x += "!" + a + ", ";
                        }
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot-help", chan.Language, new List<string>() { x }), chan.Name);
                        return true;
                    }
                }
            }
            catch (Exception b)
            {
                core.handleException(b);
            }
            return true;
        }

        private void StartSearch()
        {
            Regex value = new Regex(search_key, RegexOptions.Compiled);
            config.channel _channel = core.getChannel(Channel);
            string results = "";
            int count = 0;
            lock (text)
            {
                foreach (item data in text)
                {
                    if (data.key == search_key || value.Match(data.text).Success)
                    {
                        count++;
                        results = results + data.key + ", ";
                    }
                }
            }
            if (results == "")
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("ResultsWereNotFound", Reply.Language), Reply.Name);
            }
            else
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("Results", _channel.Language, new List<string> { count.ToString() }) + results, Reply.Name);
            }
            RegularModule.running = false;
        }

        /// <summary>
        /// Search
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="Chan"></param>
        public void RSearch(string key, config.channel Chan)
        {
            if (!key.StartsWith("@regsearch"))
            {
                return;
            }
            if (!misc.IsValidRegex(key))
            {
                core.irc.Message(messages.get("Error1", Chan.Language), Chan.Name);
                return;
            }
            if (key.Length < 11)
            {
                core.irc.Message(messages.get("Search1", Chan.Language), Chan.Name);
                return;
            }
            config.channel data = isAllowed(Chan);
            bool Allowed = (data != null);
            if (!Allowed)
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("db7", Chan.Language), Chan.Name);
                return;
            }
            infobot_core infobot = (infobot_core)data.RetrieveObject("Infobot");
            if (infobot == null)
            {
                core.Log("Unable to perform regsearch because the Infobot doesn't exist in " + Chan.Name, true);
                return;
            }
            infobot.search_key = key.Substring(11);
            RegularModule.running = true;
            Reply = Chan;
            Th = new Thread(infobot.StartSearch);
            Th.Start();
            int check = 1;
            while (RegularModule.running)
            {
                check++;
                Thread.Sleep(100);
                if (check > 8)
                {
                    Th.Abort();
                    core.irc.Message(messages.get("Error2", Chan.Language), Chan.Name);
                    RegularModule.running = false;
                    return;
                }
            }
        }

        public void Find(string key, config.channel Chan)
        {
            if (Chan == null)
            {
                return;
            }
            if (!key.StartsWith("@search"))
            {
                return;
            }
            config.channel data = isAllowed(Chan);
            bool Allowed = (data != null);
            if (!Allowed)
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("db7", Chan.Language), Chan.Name);
                return;
            }
            if (key.Length < 9)
            {
                core.irc.Message(messages.get("Error1", Chan.Language), Chan.Name);
                return;
            }
            key = key.Substring(8);
            int count = 0;
            infobot_core infobot = (infobot_core)data.RetrieveObject("Infobot");
            if (infobot == null)
            {
                core.Log("Unable to perform regsearch because the Infobot doesn't exist in " + Chan.Name, true);
                return;
            }
            string results = "";
            lock (infobot.text)
            {
                foreach (item Data in infobot.text)
                {
                    if (Data.key == key || Data.text.Contains(key))
                    {
                        results = results + Data.key + ", ";
                        count++;
                    }
                }
            }
            if (results == "")
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("ResultsWereNotFound", Chan.Language), Chan.Name);
            }
            else
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("Results", Chan.Language, new List<string> { count.ToString() }) + results, Chan.Name);
            }
        }

        private config.channel isAllowed(config.channel chan)
        {
            bool Allowed;
            config.channel data = null;
            if (chan == null)
            {
                return chan;
            }
            if (chan.shared == "local" || chan.shared == "")
            {
                data = chan;
                Allowed = true;
            }
            else
            {
                Allowed = Linkable(core.getChannel(chan.shared), chan);
                if (Allowed != false)
                {
                    data = core.getChannel(chan.shared);
                }
                if (data == null)
                {
                    Allowed = false;
                }
            }
            if (Allowed)
            {
                return data;
            }
            return null;
        }

        /// <summary>
        /// Save a new key
        /// </summary>
        /// <param name="Text">Text</param>
        /// <param name="key">Key</param>
        /// <param name="user">User who created it</param>
        public void setKey(string Text, string key, string user, config.channel chan)
        {
            while (locked)
            {
                Thread.Sleep(200);
            }
            lock (text)
            {
                config.channel ch = core.getChannel(Channel);
                try
                {
                    foreach (item data in text)
                    {
                        if (data.key == key)
                        {
                            if (!chan.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("Error3", chan.Language), chan.Name);
                            }
                            return;
                        }
                    }
                    text.Add(new item(key, Text, user, "false"));
                    core.irc.Message(messages.get("infobot6", chan.Language), chan.Name);
                    infobot_core infobot = (infobot_core)ch.RetrieveObject("Infobot");
                    if (infobot == null)
                    {
                        core.Log("Unable to save the key because the Infobot doesn't exist in " + ch.Name, true);
                        return;
                    }
                    infobot.stored = false;
                }
                catch (Exception b)
                {
                    core.handleException(b);
                }
            }
        }

        /// <summary>
        /// Alias
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="al">Alias</param>
        /// <param name="user">User</param>
        public void aliasKey(string key, string al, string user, config.channel chan)
        {
            config.channel ch = core.getChannel(Channel);
            if (ch == null)
            {
                return;
            }
            lock (Alias)
            {
                foreach (staticalias stakey in Alias)
                {
                    if (stakey.Name == al)
                    {
                        if (!chan.suppress_warnings)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot7", chan.Language), chan.Name);
                        }
                        return;
                    }
                }
                Alias.Add(new staticalias(al, key));
            }
            core.irc._SlowQueue.DeliverMessage(messages.get("infobot8", chan.Language), chan.Name);
            stored = false;
        }

        public void rmKey(string key, string user, config.channel _ch)
        {
            config.channel ch = core.getChannel(Channel);
            while (locked)
            {
                Thread.Sleep(200);
            }
            lock (text)
            {
                foreach (item keys in text)
                {
                    if (keys.key == key)
                    {
                        text.Remove(keys);
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot9", _ch.Language) + key, _ch.Name);
                        stored = false;
                        return;
                    }
                }
            }
            core.irc._SlowQueue.DeliverMessage(messages.get("infobot10", _ch.Language), _ch.Name);
        }
    }
}
