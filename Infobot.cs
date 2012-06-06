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
using System.IO;

namespace wmib
{
    public class infobot_core
    {
        /// <summary>
        /// Data file
        /// </summary>
        public string datafile = "";

        // if we need to update dump
        public bool update = true;

        /// <summary>
        /// Locked
        /// </summary>
        public bool locked = false;

        public static config.channel Reply;

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

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="Key">Key</param>
            /// <param name="Text">Text of the key</param>
            /// <param name="User">User who created the key</param>
            /// <param name="Lock">If key is locked or not</param>
            public item(string Key, string Text, string User, string Lock = "false")
            {
                text = Text;
                key = Key;
                locked = Lock;
                user = User;
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

        public static List<InfoItem> jobs = new List<InfoItem>();

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

        private static bool running;

        public static bool Unwritable;

        public static bool Disabled;

        private string search_key;

        /// <summary>
        /// Load it
        /// </summary>
        public void Load()
        {
            text.Clear();
            if (!File.Exists(datafile))
            {
                // Create db
                File.WriteAllText(datafile, "");
            }

            string[] db = File.ReadAllLines(datafile);
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
                        text.Add(new item(name, value, "", Locked));
                    }
                    else
                    {
                        Alias.Add(new staticalias(name, value));
                    }
                }
            }
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
                    foreach(item g in OriginalList)
                    {
                        if (f == g.key)
                        {
                            Item.Add(g);
                            break;
                        }
                    }
                }
            }
            } catch(Exception)
            {
                Program.Log("Exception while creating list for html");
                locked = false;
            }
            return Item;
        }

        public static void Initialise()
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
                        List<InfoItem> list = new List<InfoItem>();
                        list.AddRange(jobs);
                        jobs.Clear();
                        Unwritable = false;
                        foreach (InfoItem item in list)
                        {
                            item.Channel.Keys.print(item.Name, item.User, item.Channel, item.Host);
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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="database"></param>
        /// <param name="channel"></param>
        public infobot_core(string database, string channel)
        {
            datafile = database;
            Channel = channel;
            Load();
        }

        public static string parseInfo(string key, string[] pars)
        {
            string keyv = key;
            if (pars.Length > 1)
            {
                int curr = 1;
                while (pars.Length > curr)
                {
                    keyv = keyv.Replace("$" + curr.ToString(), pars[curr]);
                    curr++;
                }
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
                File.WriteAllText(datafile, "");
                foreach (staticalias key in Alias)
                {
                    File.AppendAllText(datafile,
                                       key.Name + config.separator + key.Key + config.separator + "alias" + "\n");
                }
                foreach (item key in text)
                {
                    File.AppendAllText(datafile,
                                       key.key + config.separator + key.text + config.separator + "key" +
                                       config.separator + key.locked + config.separator + key.user + "\n");
                }
            }
            catch (Exception b)
            {
                core.handleException(b, Channel);
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
                    return core.decode(data.text);
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
                if (name.Contains(" "))
                {
                    string[] parm = name.Split(' ');
                    if (parm[1] == "is")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                return true;
                            }
                            if (parm.Length < 3)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("key", chan.Language), chan.Name);
                                return true;
                            }
                            string key = name.Substring(name.IndexOf(" is") + 4);
                            data.Keys.setKey(key, parm[0], "", chan);
                        }
                        else
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                        }
                        return false;
                    }
                    if (parm[1] == "alias")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                return true;
                            }
                            if (parm.Length < 3)
                            {
                                core.irc.Message(messages.get("InvalidAlias", chan.Language), chan.Name);
                                return true;
                            }
                            data.Keys.aliasKey(name.Substring(name.IndexOf(" alias") + 7), parm[0], "", chan);
                        }
                        else
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
                        }
                        return false;
                    }
                    if (parm[1] == "unalias")
                    {
                        if (chan.Users.isApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("db7", chan.Language), chan.Name);
                                return true;
                            }
                            foreach (staticalias b in data.Keys.Alias)
                            {
                                if (b.Name == parm[0])
                                {
                                    data.Keys.Alias.Remove(b);
                                    core.irc.Message(messages.get("AliasRemoved", chan.Language), chan.Name);
                                    data.Keys.Save();
                                    return false;
                                }
                            }
                            return false;
                        }
                        core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
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
                            data.Keys.rmKey(parm[0], "", chan);
                        }
                        else
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Authorization", chan.Language), chan.Name);
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
                    if (User.StartsWith(" "))
                    {
                        while (User.StartsWith(" "))
                        {
                            User = User.Substring(1);
                        }
                    }
                    name = name.Substring(0, name.IndexOf("|"));
                }
                string[] p = name.Split(' ');
                int parameters = p.Length;
                string keyv = data.Keys.getValue(p[0]);
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
                foreach (staticalias b in data.Keys.Alias)
                {
                    if (b.Name == p[0])
                    {
                        keyv = data.Keys.getValue(b.Key);
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

                    if (chan.infobot_auto_complete)
                    {
                        List<string> results = new List<string>();
                        foreach (item f in data.Keys.text)
                        {
                            if (f.key.StartsWith(p[0]))
                            {
                                results.Add(f.key);
                            }
                        }
                        foreach (staticalias f in data.Keys.Alias)
                        {
                            if (f.Key.StartsWith(p[0]))
                            {
                                results.Add(f.Key);
                            }
                        }

                        if (results.Count == 1)
                        {
                            keyv = data.Keys.getValue(results[0]);
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
                            foreach (staticalias alias in data.Keys.Alias)
                            {
                                if (alias.Name == p[0])
                                {
                                    keyv = data.Keys.getValue(alias.Key);
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
                            string x = "";
                            foreach (string ix in results)
                            {
                                x += ix + ", ";
                            }
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot-a-e", chan.Language, new List<string>() { x }), chan.Name);
                            return true;
                        }
                    }

                    if (chan.infobot_help == true)
                    {
                        List<string> Sugg = new List<string>();
                        p[0] = p[0].ToLower();
                        foreach (item f in data.Keys.text)
                        {
                            if (f.text.Contains(p[0]) || f.key.ToLower().Contains(p[0]))
                            {
                                Sugg.Add(f.key);
                            }
                        }

                        if (Sugg.Count > 0)
                        {
                            string x = "";
                            foreach (string a in Sugg)
                            {
                                x += a + ", ";
                            }
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot-help", chan.Language, new List<string>() { x }), chan.Name);
                            return true;
                        }
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
            foreach (item data in text)
            {
                if (data.key == search_key || value.Match(data.text).Success)
                {
                    count++;
                    results = results + data.key + ", ";
                }
            }
            if (results == "")
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("ResultsWereNotFound", Reply.Language), Reply.Name);
            }
            else
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("Results", _channel.Language, new List<string>{count.ToString()})  + results, Reply.Name);
            }
            running = false;
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
            bool Allowed = Chan != null;
            if (!Allowed)
            {
                core.irc._SlowQueue.DeliverMessage(messages.get("db7", Chan.Language), Chan.Name);
                return;
            }
            data.Keys.search_key = key.Substring(11);
            running = true;
            Reply = Chan;
            Thread th = new Thread(data.Keys.StartSearch);
            th.Start();
            int check = 1;
            while (running)
            {
                check++;
                Thread.Sleep(100);
                if (check > 8)
                {
                    th.Abort();
                    core.irc.Message(messages.get("Error2", Chan.Language), Chan.Name);
                    running = false;
                    return;
                }
            }
        }

        public void Find(string key, config.channel Chan)
        {
            if (!key.StartsWith("@search"))
            {
                return;
            }
            config.channel data = isAllowed(Chan);
            bool Allowed = (Chan != null);
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
            string results = "";
            foreach (item Data in data.Keys.text)
            {
                if (Data.key == key || Data.text.Contains(key))
                {
                    results = results + Data.key + ", ";
                    count++;
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
			config.channel ch = core.getChannel (Channel);
            try
            {
                foreach (item data in text)
                {
                    if (data.key == key)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Error3", chan.Language), chan.Name);
                        return;
                    }
                }
                text.Add(new item(key, core.encode(Text), user, "false"));
                core.irc.Message(messages.get( "infobot6", chan.Language ), chan.Name);
                ch.Keys.Save();
            }
            catch (Exception b)
            {
                core.handleException(b);
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
            foreach (staticalias stakey in Alias)
            {
                if (stakey.Name == al)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("infobot7", chan.Language), chan.Name);
                    return;
                }
            }
            Alias.Add(new staticalias(al, key));
            core.irc._SlowQueue.DeliverMessage(messages.get( "infobot8", chan.Language), chan.Name);
            Save();
        }

        public void rmKey(string key, string user, config.channel _ch)
        {
            config.channel ch = core.getChannel(Channel);
            while (locked)
            {
                Thread.Sleep(200);
            }
            foreach (item keys in text)
            {
                if (keys.key == key)
                {
                    text.Remove(keys);
                    core.irc._SlowQueue.DeliverMessage(messages.get( "infobot9", _ch.Language ) + key, _ch.Name);
                    Save();
                    return;
                }
            }
            core.irc._SlowQueue.DeliverMessage(messages.get( "infobot10", _ch.Language ), _ch.Name);
        }
    }
}
