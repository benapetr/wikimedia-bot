using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Text;

namespace wmib
{
    public partial class Infobot
    {
        /// <summary>
        /// Data file
        /// </summary>
        public string datafile_raw = "";
        public string datafile_xml = "";
        public string temporary_data = "";
        public bool Sensitive = true;
        public bool stored = true;
        public static string DefaultPrefix = "!";
        public string prefix = "!";

        private Thread tSearch = null;
        public Thread SnapshotManager = null;
        private Module Parent;

        // if we need to update dump
        public bool update = true;

        public static Channel ReplyChan = null;

        public static DateTime NA = DateTime.MaxValue;

        /// <summary>
        /// List of all items in class
        /// </summary>
        public List<InfobotKey> Keys = new List<InfobotKey>();

        /// <summary>
        /// List of all aliases we want to use
        /// </summary>
        public List<InfobotAlias> Alias = new List<InfobotAlias>();

        public Channel pChannel;

        private string search_key;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="database"></param>
        /// <param name="channel"></param>
        public Infobot(string database, Channel channel, Module module, bool sensitive = true)
        {
            Sensitive = sensitive;
            datafile_xml = database + ".xml";
            datafile_raw = database;
            pChannel = channel;
            Parent = module;
            prefix = Module.GetConfig(pChannel, "Infobot.Prefix", DefaultPrefix);
            LoadData();
        }

        public bool AliasExists(string name, bool sensitive = true)
        {
            if (sensitive)
            {
                lock (this)
                {
                    foreach (InfobotAlias key in Alias)
                    {
                        if (key.Name == name)
                        {
                            return true;
                        }
                    }
                }
            }
            if (!sensitive)
            {
                name = name.ToLower();
                lock (this)
                {
                    foreach (InfobotAlias key in Alias)
                    {
                        if (key.Name.ToLower() == name)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Function returns true if key exists
        /// </summary>
        /// <param name="name">Name of key</param>
        /// <param name="sensitive">If bot is sensitive or not</param>
        /// <returns></returns>
        public bool KeyExists(string name, bool sensitive = true)
        {
            if (!sensitive)
            {
                name = name.ToLower();
                lock (this)
                {
                    foreach (InfobotKey key in Keys)
                    {
                        if (key.Key.ToLower() == name)
                        {
                            return true;
                        }
                    }
                }
            }
            if (sensitive)
            {
                lock (this)
                {
                    foreach (InfobotKey key in Keys)
                    {
                        if (key.Key == name)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public InfobotKey GetKey(string name, bool sensitive = true)
        {
            if (!sensitive)
            {
                lock (this)
                {
                    name = name.ToLower();
                    foreach (InfobotKey key in Keys)
                    {
                        if (key.Key.ToLower() == name)
                        {
                            return key;
                        }
                    }
                }
            }
            if (sensitive)
            {
                lock (this)
                {
                    foreach (InfobotKey key in Keys)
                    {
                        if (key.Key == name)
                        {
                            return key;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// @infobot-detail
        /// </summary>
        /// <param name="key"></param>
        /// <param name="chan"></param>
        public void InfobotDetail(string key, Channel chan)
        {
            InfobotKey CV = GetKey(key, Sensitive);
            if (CV == null)
            {
                chan.PrimaryInstance.irc.Queue.DeliverMessage("There is no such a key", chan, IRC.priority.low);
                return;
            }
            if (CV.Key == key)
            {
                string created = "N/A";
                string last = "N/A";
                string name = "N/A";
                if (CV.LastTime != NA)
                {
                    TimeSpan span = DateTime.Now - CV.LastTime;
                    last = CV.LastTime.ToString() + " (" + span.ToString() + " ago)";
                }
                if (CV.CreationTime != NA)
                {
                    created = CV.CreationTime.ToString();
                }
                if (CV.User != "")
                {
                    name = CV.User;
                }
                string type = " this key is normal";
                if (CV.Raw)
                {
                    type = " this key is raw";
                }
                Core.irc.Queue.DeliverMessage(messages.Localize("infobot-data", chan.Language, new List<string> {key, name, created, CV.Displayed.ToString(),
                        last + type }), chan, IRC.priority.low);
                return;
            }
        }

        public List<InfobotKey> SortedItem()
        {
            List<InfobotKey> OriginalList = new List<InfobotKey>();
            List<InfobotKey> Item = new List<InfobotKey>();
            int keycount;
            lock (this)
            {
                keycount = Keys.Count;
                OriginalList.AddRange(Keys);
            }
            try
            {
                if (keycount > 0)
                {
                    List<string> Name = new List<string>();
                    foreach (InfobotKey curr in OriginalList)
                    {
                        Name.Add(curr.Key);
                    }
                    Name.Sort();
                    foreach (string f in Name)
                    {
                        foreach (InfobotKey g in OriginalList)
                        {
                            if (f == g.Key)
                            {
                                Item.Add(g);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                Parent.HandleException(fail);
                Parent.Log("Exception while creating list for html");
            }
            return Item;
        }

        private static string ParseInfo(List<string> parameters, string original, InfobotKey Key)
        {
            bool raw = false;
            if (Key != null)
            {
                raw = Key.Raw;
            }
            string text = Key.Text;
            if (parameters.Count > 0)
            {
                string keys = "";
                int curr = 0;
                while (parameters.Count > curr)
                {
                    if (!raw)
                    {
                        text = text.Replace("$" + (curr+1).ToString(), parameters[curr]);
                        text = text.Replace("$url_encoded_" + (curr+1).ToString(), System.Web.HttpUtility.UrlEncode(parameters[curr]));
                        text = text.Replace("$wiki_encoded_" + (curr+1).ToString(), System.Web.HttpUtility.UrlEncode(parameters[curr]).Replace("+", "_").Replace("%3a", ":").Replace("%2f", "/").Replace("%28", "(").Replace("%29", ")"));
                    }
                    if (keys == "")
                    {
                        keys = parameters[curr];
                    }
                    else
                    {
                        keys = keys + " " + parameters[curr];
                    }
                    curr++;
                }
                if (original.Contains ("|") && !raw)
                {
                    original = original.Substring (0, original.IndexOf ("|"));
                    original = original.Trim ();
                }
                text = text.Replace("$*", original);
                text = text.Replace("$url_encoded_*", System.Web.HttpUtility.UrlEncode(original));
                text = text.Replace("$wiki_encoded_*", System.Web.HttpUtility.UrlEncode(original).Replace("+", "_").Replace("%3a", ":").Replace("%2f", "/").Replace("%28", "(").Replace("%29", ")"));
            }
            return text;
        }

        public static bool Linkable(Channel host, Channel guest)
        {
            if (host == null)
            {
                return false;
            }
            if (guest == null)
            {
                return false;
            }
            if (host.SharedLinkedChan.Contains(guest))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether this key is ignored for channel
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instance is ignored the specified name; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='name'>
        /// If set to <c>true</c> name.
        /// </param>
        public bool IsIgnored(string name, Channel channel)
        {
            string ignore_test = name;
            if (ignore_test.Contains(" "))
            {
                ignore_test = ignore_test.Substring(0, ignore_test.IndexOf(" "));
            }
            return (channel.Infobot_IgnoredNames.Contains(ignore_test));
        }

        private bool DeliverKey(InfobotKey Key, string OriginalText, Channel chan)
        {
            if (Key == null)
            {
                return false;
            }
            string Target_ = "";
            string text = OriginalText;
            // we remove the key name from message so that only parameters remain
            if (text.Contains(" "))
            {
                text = text.Substring(text.IndexOf(" ") + 1);
            } else
            {
                text = "";
            }
            if (text.Contains("|"))
            {
                Target_ = OriginalText.Substring(OriginalText.IndexOf("|") + 1);
                if (Module.GetConfig(chan, "Infobot.Trim-white-space-in-name", true))
                {
                    Target_ = Target_.Trim();
                }
                text = text.Substring(0, text.IndexOf("|"));
            }
            List<string> Parameters = new List<string>(text.Split(' '));
            string value_ = Key.Text;
            if (text != "")
            {
                value_ = ParseInfo(Parameters, text, Key);
            }
            if (Target_ == "")
            {
                Core.irc.Queue.DeliverMessage(value_, chan);
            }
            else
            {
                Core.irc.Queue.DeliverMessage(Target_ + ": " + value_, chan);
            }
			Key.Displayed++;
			Key.LastTime = DateTime.Now;
			this.StoreDB();
            return true;
        }

        /// <summary>
        /// Print a value to channel if found, this message doesn't need to be a valid command for it to work
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="user">User</param>
        /// <param name="chan">Channel</param>
        /// <param name="host">Host name</param>
        /// <returns></returns>
        public bool InfobotExec(string message, string user, Channel chan, string host)
        {
            try
            {
                // check if it starts with the prefix
                if (!message.StartsWith(prefix))
                {
                    return true;
                }
                // check if this channel is allowed to access the db
                Channel data = RetrieveMasterDBChannel(chan);
                bool Allowed = (data != null);
                // handle prefix
                message = message.Substring(1);
                Infobot infobot = null;

                if (Allowed)
                {
                    infobot = (Infobot)data.RetrieveObject("Infobot");
                }

                // check if key is ignored
                if (IsIgnored(message, chan))
                {
                    return true;
                }

                // split by parameters so we can easily get the arguments user provided
                List<string> Parameters = new List<string>(message.Split(' '));

                // check if key has some parameters or command
                if (Parameters.Count > 1)
                {
                    // someone want to create a new key
                    if (Parameters[1] == "is")
                    {
                        // check if they are approved to do that
                        if (chan.SystemUsers.IsApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                // check if we can deliver error message
                                if (!chan.SuppressWarnings)
                                {
                                    Core.irc.Queue.DeliverMessage(messages.Localize("db7", chan.Language), chan);
                                }
                                return true;
                            }
                            // they can but there is only 1 parameter and we need at least 2
                            if (Parameters.Count < 3)
                            {
                                if (!chan.SuppressWarnings)
                                {
                                    Core.irc.Queue.DeliverMessage(messages.Localize("key", chan.Language), chan);
                                }
                                return true;
                            }
                            // get a key name
                            string key = message.Substring(message.IndexOf(" is") + 4);
                            // check if there is pipe symbol in the key, which is not a valid symbol
                            if (Parameters[0].Contains("|"))
                            {
                                if (!chan.SuppressWarnings)
                                {
                                    Core.irc.Queue.DeliverMessage("Invalid symbol in the key", chan);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.SetKey(key, Parameters[0], user, chan);
                                return true;
                            }
                        }
                        else
                        {
                            if (!chan.SuppressWarnings)
                            {
                                Core.irc.Queue.DeliverMessage(messages.Localize("Authorization", chan.Language), chan);
                            }
                        }
                        return false;
                    }
                    // alias
                    bool force = false;
                    if (Parameters[1] == "alias" || Parameters[1] == "force-alias")
                    {
                        if (Parameters[1] == "force-alias")
                        {
                            force = true;
                        }
                        if (chan.SystemUsers.IsApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                if (!chan.SuppressWarnings)
                                {
                                    Core.irc.Queue.DeliverMessage(messages.Localize("db7", chan.Language), chan);
                                }
                                return true;
                            }
                            if (Parameters.Count < 3)
                            {
                                if (!chan.SuppressWarnings)
                                {
                                    Core.irc.Queue.DeliverMessage(messages.Localize("InvalidAlias", chan.Language), chan);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.aliasKey(message.Substring(message.IndexOf(" alias") + 7), Parameters[0], "", chan, force);
                                return true;
                            }
                        }
                        else
                        {
                            if (!chan.SuppressWarnings)
                            {
                                Core.irc.Queue.DeliverMessage(messages.Localize("Authorization", chan.Language), chan);
                            }
                        }
                        return false;
                    }
                    if (Parameters[1] == "unalias")
                    {
                        if (chan.SystemUsers.IsApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                if (!chan.SuppressWarnings)
                                {
                                    Core.irc.Queue.DeliverMessage(messages.Localize("db7", chan.Language), chan);
                                }
                                return true;
                            }
                            if (infobot != null)
                            {
                                lock (infobot)
                                {
                                    foreach (InfobotAlias b in infobot.Alias)
                                    {
                                        if (b.Name == Parameters[0])
                                        {
                                            infobot.Alias.Remove(b);
                                            Core.irc.Queue.DeliverMessage(messages.Localize("AliasRemoved", chan.Language), chan);
											this.StoreDB();
                                            return false;
                                        }
                                    }
                                }
                            }
                            return false;
                        }
                        if (!chan.SuppressWarnings)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("Authorization", chan.Language), chan);
                        }
                        return false;
                    }
                    // remove key
                    if (Parameters[1] == "del")
                    {
                        if (chan.SystemUsers.IsApproved(user, host, "info"))
                        {
                            if (!Allowed)
                            {
                                Core.irc.Queue.DeliverMessage(messages.Localize("db7", chan.Language), chan);
                                return true;
                            }
                            if (infobot != null)
                            {
                                infobot.rmKey(Parameters[0], "", chan);
                            }
                        }
                        else
                        {
                            if (!chan.SuppressWarnings)
                            {
                                Core.irc.Queue.DeliverMessage(messages.Localize("Authorization", chan.Language), chan);
                            }
                        }
                        return false;
                    }
                }
                if (!Allowed)
                {
                    return true;
                }

                InfobotKey Key = infobot.GetKey(Parameters[0]);
                // let's try to deliver this as a key
                if (DeliverKey(Key, message, chan))
                {
                    return true;
                }
                
                string lower = Parameters[0].ToLower();
                // there is no key with this name, let's check if there is an alias for such a key
                lock (infobot)
                {
                    foreach (InfobotAlias alias in infobot.Alias)
                    {
                        if (Sensitive)
                        {
                            if (alias.Name == Parameters[0])
                            {
                                // let's try to get a target key
                                InfobotKey Key_ = infobot.GetKey(alias.Key);
                                if (DeliverKey(Key_, message, chan))
                                {
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            if (alias.Name.ToLower() == lower)
                            {
                                // let's try to get a target key
                                InfobotKey Key_ = infobot.GetKey(alias.Key);
                                if (DeliverKey(Key_, message, chan))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                if (Module.GetConfig(chan, "Infobot.auto-complete", false))
                {
                    if (infobot != null)
                    {
                        List<string> results = new List<string>();
                        lock (infobot)
                        {
                            foreach (InfobotKey f in infobot.Keys)
                            {
                                if (!results.Contains(f.Key) && f.Key.StartsWith(Parameters[0]))
                                {
                                    results.Add(f.Key);
                                }
                            }
                            foreach (InfobotAlias f in infobot.Alias)
                            {
                                if (!results.Contains(f.Key) && f.Key.StartsWith(Parameters[0]))
                                {
                                    results.Add(f.Key);
                                }
                            }
                        }

                        if (results.Count == 1)
                        {
                            InfobotKey Key_ = infobot.GetKey(results[0]);
                            if (DeliverKey(Key_, message, chan))
                            {
                                return true;
                            }
                            lock (infobot)
                            {
                                foreach (InfobotAlias alias in infobot.Alias)
                                {
                                    if (alias.Name == results[0])
                                    {
                                        Key_ = infobot.GetKey(alias.Name);
                                        if (DeliverKey(Key_, message, chan))
                                        {
                                            return true;
                                        }
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
                            Core.irc.Queue.DeliverMessage(messages.Localize("infobot-c-e", chan.Language, new List<string>() { x }), chan);
                            return true;
                        }
                    }
                }

                if (Module.GetConfig(chan, "Infobot.Help", false) && infobot != null)
                {
                    List<string> Sugg = new List<string>();
                    string key = Parameters[0].ToLower();
                    lock (infobot)
                    {
                        foreach (InfobotKey f in infobot.Keys)
                        {
                            if (!Sugg.Contains(f.Key) && (f.Text.ToLower().Contains(key) || f.Key.ToLower().Contains(key)))
                            {
                                Sugg.Add(f.Key);
                            }
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
                        Core.irc.Queue.DeliverMessage(messages.Localize("infobot-help", chan.Language, new List<string>() { x }), chan.Name);
                        return true;
                    }
                }
            }
            catch (Exception b)
            {
                Parent.HandleException(b);
            }
            return true;
        }

        private void StartSearch()
        {
            Regex value = new Regex(search_key, RegexOptions.Compiled);
            Channel _channel = Core.GetChannel(pChannel.Name);
            string results = "";
            int count = 0;
            lock (this)
            {
                foreach (InfobotKey data in Keys)
                {
                    if (data.Key == search_key || value.Match(data.Text).Success)
                    {
                        count++;
                        results = results + data.Key + ", ";
                    }
                }
            }
            if (results == "")
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("ResultsWereNotFound", ReplyChan.Language), ReplyChan.Name);
            }
            else
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("Results", _channel.Language, new List<string> { count.ToString() }) + results, ReplyChan.Name);
            }
            RegularModule.running = false;
        }

        /// <summary>
        /// Search
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="Chan"></param>
        public void RSearch(string key, Channel Chan)
        {
            if (!key.StartsWith("@regsearch"))
            {
                return;
            }
            if (!misc.IsValidRegex(key))
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("Error1", Chan.Language), Chan.Name);
                return;
            }
            if (key.Length < 11)
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("Search1", Chan.Language), Chan.Name);
                return;
            }
            Channel data = RetrieveMasterDBChannel(Chan);
            bool Allowed = (data != null);
            if (!Allowed)
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("db7", Chan.Language), Chan.Name);
                return;
            }
            Infobot infobot = (Infobot)data.RetrieveObject("Infobot");
            if (infobot == null)
            {
                Syslog.Log("Unable to perform regsearch because the Infobot doesn't exist in " + Chan.Name, true);
                return;
            }
            infobot.search_key = key.Substring(11);
            RegularModule.running = true;
            ReplyChan = Chan;
            tSearch = new Thread(infobot.StartSearch);
            tSearch.Start();
            int check = 1;
            while (RegularModule.running)
            {
                check++;
                Thread.Sleep(100);
                if (check > 8)
                {
                    tSearch.Abort();
                    Core.irc.Queue.DeliverMessage(messages.Localize("Error2", Chan.Language), Chan.Name);
                    RegularModule.running = false;
                    return;
                }
            }
        }

        public void Find(string key, Channel Chan)
        {
            if (Chan == null)
            {
                return;
            }
            if (!key.StartsWith("@search"))
            {
                return;
            }
            Channel data = RetrieveMasterDBChannel(Chan);
            bool Allowed = (data != null);
            if (!Allowed)
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("db7", Chan.Language), Chan.Name);
                return;
            }
            if (key.Length < 9)
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("Error1", Chan.Language), Chan.Name);
                return;
            }
            key = key.Substring(8);
            int count = 0;
            Infobot infobot = (Infobot)data.RetrieveObject("Infobot");
            if (infobot == null)
            {
                Syslog.Log("Unable to perform regsearch because the Infobot doesn't exist in " + Chan.Name, true);
                return;
            }
            string results = "";
            lock (infobot)
            {
                foreach (InfobotKey Data in infobot.Keys)
                {
                    if (Data.Key == key || Data.Text.Contains(key))
                    {
                        results = results + Data.Key + ", ";
                        count++;
                    }
                }
            }
            if (results == "")
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("ResultsWereNotFound", Chan.Language), Chan.Name);
            }
            else
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("Results", Chan.Language, new List<string> { count.ToString() }) + results, Chan.Name);
            }
        }

        /// <summary>
        /// Retrieves the master DB channel
        /// </summary>
        /// <returns>
        /// The master DB channel.
        /// </returns>
        /// <param name='chan'>
        /// Chan.
        /// </param>
        private Channel RetrieveMasterDBChannel(Channel chan)
        {
            bool Allowed;
            Channel data = null;
            if (chan == null)
            {
                return chan;
            }
            if (chan.SharedDB == "local" || chan.SharedDB == "")
            {
                data = chan;
                Allowed = true;
            }
            else
            {
                Allowed = Linkable(Core.GetChannel(chan.SharedDB), chan);
                if (Allowed != false)
                {
                    data = Core.GetChannel(chan.SharedDB);
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

        public void SetRaw(string key, string user, Channel chan)
        {
            InfobotKey Key = GetKey(key, Sensitive);
            if (Key == null)
            {
                Core.irc.Queue.DeliverMessage("There is no such a key, " + user, chan.Name);
                return;
            }
            Key.Raw = true;
            Core.irc.Queue.DeliverMessage("This key will be displayed with no extra styling, variables and will ignore all symbols", chan.Name);
			this.StoreDB();
        }

        public void UnsetRaw(string key, string user, Channel chan)
        {
            InfobotKey Key = GetKey(key, Sensitive);
            if (Key == null)
            {
                Core.irc.Queue.DeliverMessage("There is no such a key, " + user, chan.Name);
                return;
            }
            Key.Raw = false;
            Core.irc.Queue.DeliverMessage("This key will be displayed normally", chan.Name);
			this.StoreDB();
        }

        /// <summary>
        /// Save a new key
        /// </summary>
        /// <param name="Text">Text</param>
        /// <param name="key">Key</param>
        /// <param name="user">User who created it</param>
        public void SetKey(string Text, string key, string user, Channel chan)
        {
            lock (this)
            {
                try
                {
                    if (KeyExists(key, Sensitive))
                    {
                        if (!chan.SuppressWarnings)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("Error3", chan.Language), chan);
                        }
                        return;
                    }
                    Keys.Add(new InfobotKey(key, Text, user, "false"));
                    Core.irc.Queue.DeliverMessage(messages.Localize("infobot6", chan.Language), chan);
                    Infobot infobot = (Infobot)pChannel.RetrieveObject("Infobot");
                    if (infobot == null)
                    {
                        Syslog.Log("Unable to save the key because the Infobot doesn't exist in " + pChannel.Name, true);
                        return;
                    }
					infobot.StoreDB();
                }
                catch (Exception b)
                {
                    Core.HandleException(b, "infobot");
                }
            }
        }

        public void SnapshotStart()
        {
            try
            {
                while (!this.stored)
                {
                    Thread.Sleep(100);
                }
                lock (this)
                {
                    DateTime creationdate = DateTime.Now;
                    Syslog.Log("Creating snapshot " + temporary_data);
                    File.Copy(datafile_xml, temporary_data);
                    Core.irc.Queue.DeliverMessage("Snapshot " + temporary_data + " was created for current database as of " + creationdate.ToString(), pChannel);
                }
            }
            catch (Exception fail)
            {
                Syslog.Log("Unable to create a snapshot for " + pChannel.Name, true);
                Core.HandleException(fail, "infobot");
            }
        }

        public void RecoverStart()
        {
            try
            {
                while (!this.stored)
                {
                    Thread.Sleep(100);
                }
                lock (this)
                {
                    Syslog.Log("Recovering snapshot " + temporary_data);
                    File.Copy(temporary_data, datafile_xml, true);
                    this.Keys.Clear();
                    this.Alias.Clear();
                    Parent.Log("Loading snapshot of " + pChannel.Name);
                    LoadData();
                    Core.irc.Queue.DeliverMessage("Snapshot " + temporary_data + " was loaded and previous database was permanently deleted", pChannel);
                }
            }
            catch (Exception fail)
            {
                Parent.Log("Unable to recover a snapshot for " + pChannel.Name + " the db is likely broken now", true);
                Parent.HandleException(fail);
            }
        }

        public bool IsValid(string name)
        {
            if (name == "")
            {
                return false;
            }
            foreach (char i in name)
            {
                if (i == '\0')
                {
                    continue;
                }
                if (((int)i) < 48)
                {
                    return false;
                }
                if (((int)i) > 122)
                {
                    return false;
                }
                if (((int)i) > 90)
                {
                    if (((int)i) < 97)
                    {
                        return false;
                    }
                }
                if (((int)i) > 57)
                {
                    if (((int)i) < 65)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void RecoverSnapshot(Channel chan, string name)
        {
            try
            {
                lock (this)
                {
                    if (!IsValid(name))
                    {
                        Core.irc.Queue.DeliverMessage("This is not a valid name for tsnapsho, you can only use a-zA-Z and 0-9 chars", chan.Name);
                        return;
                    }
                    if (SnapshotManager != null)
                    {
                        if (SnapshotManager.ThreadState == ThreadState.Running)
                        {
                            Core.irc.Queue.DeliverMessage("There is already another snapshot operation running for this channel", chan.Name);
                            return;
                        }
                    }
                    string datafile = RegularModule.SnapshotsDirectory + Path.DirectorySeparatorChar + pChannel.Name + Path.DirectorySeparatorChar + name;
                    if (!File.Exists(datafile))
                    {
                        Core.irc.Queue.DeliverMessage("The requested datafile " + name + " was not found", chan.Name, IRC.priority.low);
                        return;
                    }

                    SnapshotManager = new Thread(RecoverStart);
                    temporary_data = datafile;
                    SnapshotManager.Name = "Module:Infobot/Snapshot";
                    Core.ThreadManager.RegisterThread(SnapshotManager);
                    SnapshotManager.Start();
                    RegularModule.SetConfig(chan, "HTML.Update", true);
                }
            }
            catch (Exception fail)
            {
                Parent.HandleException(fail);
            }
        }

		/// <summary>
		/// Stores all data to database delayed using different thread
		/// </summary>
		public void StoreDB()
		{
			this.stored = false;
		}

        public void CreateSnapshot(Channel chan, string name)
        {
            try
            {
                if (!IsValid(name))
                {
                    Core.irc.Queue.DeliverMessage("This is not a valid name for snapshot, you can only use a-zA-Z and 0-9 chars", chan.Name);
                    return;
                }
                if (SnapshotManager != null)
                {
                    if (SnapshotManager.ThreadState == ThreadState.Running)
                    {
                        Core.irc.Queue.DeliverMessage("There is already another snapshot operation running for this channel", chan.Name);
                        return;
                    }
                }
                string datafile = RegularModule.SnapshotsDirectory + Path.DirectorySeparatorChar + pChannel.Name + Path.DirectorySeparatorChar + name;
                if (File.Exists(datafile))
                {
                    Core.irc.Queue.DeliverMessage("The requested snapshot " + name + " already exist", chan.Name, IRC.priority.low);
                    return;
                }
                SnapshotManager = new Thread(SnapshotStart);
                temporary_data = datafile;
                SnapshotManager.Name = "Snapshot";
                SnapshotManager.Start();
            }
            catch (Exception fail)
            {
                Parent.HandleException(fail);
            }
        }

        /// <summary>
        /// Alias
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="al">Alias</param>
        /// <param name="user">User</param>
        public void aliasKey(string key, string al, string user, Channel chan, bool enforced = false)
        {
            lock (this)
            {
                foreach (InfobotAlias stakey in Alias)
                {
                    if (stakey.Name == al)
                    {
                        if (!chan.SuppressWarnings)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("infobot7", chan.Language), chan.Name);
                        }
                        return;
                    }
                }
                if (!KeyExists(key))
                {
                    if (!enforced)
                    {
                        if (AliasExists(key))
                        {
                            Core.irc.Queue.DeliverMessage("Unable to create alias for " + key + " because the target is alias, but not a key, if you really want to create this broken alias do !" + al + " force-alias " + key, chan.Name);
                            return;
                        }
                        Core.irc.Queue.DeliverMessage("Unable to create alias for " + key + " because there is no such key, if you really want to create this broken alias do !" + al + " force-alias " + key, chan.Name);
                        return;
                    }
                }
                Alias.Add(new InfobotAlias(al, key));
            }
            Core.irc.Queue.DeliverMessage(messages.Localize("infobot8", chan.Language), chan.Name);
			this.StoreDB();
        }

        public void rmKey(string key, string user, Channel _ch)
        {
            lock (this)
            {
                foreach (InfobotKey keys in Keys)
                {
                    if (Sensitive)
                    {
                        if (keys.Key == key)
                        {
                            Keys.Remove(keys);
                            Core.irc.Queue.DeliverMessage(messages.Localize("infobot9", _ch.Language) + key, _ch.Name);
							this.StoreDB();
                            return;
                        }
                    }
                    else
                    {
                        if (keys.Key.ToLower() == key.ToLower())
                        {
                            Keys.Remove(keys);
                            Core.irc.Queue.DeliverMessage(messages.Localize("infobot9", _ch.Language) + key, _ch.Name);
							this.StoreDB();
                            return;
                        }
                    }
                }
            }
            Core.irc.Queue.DeliverMessage(messages.Localize("infobot10", _ch.Language), _ch.Name);
        }
    }
}
