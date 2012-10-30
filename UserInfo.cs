//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Text.RegularExpressions;

namespace wmib
{
    public class User : IComparable
    {
        public string Host;
        public string Ident;
        public string Nick;
        public User(string _Nick, string host, string ident)
        {
            Nick = _Nick;
            Ident = ident;
            Host = host;
        }

        public int CompareTo(object obj)
        {
            if (obj is User)
            {
                return this.Nick.CompareTo((obj as User).Nick);
            }
            return 0;
        }
    }

    public class module_stat : Module
    {
        public override bool Construct()
        {
            base.Create("STATISTICS", true);
            return true;
        }

        public override void Load()
        {
            while (true)
            {
                try
                {
                    foreach (config.channel chan in config.channels)
                    {
                        if (chan.info.Stored == false)
                        {
                            chan.info.Save();
                        }
                        chan.info.Stored = true;
                    }
                    Thread.Sleep(8000);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception f)
                {
                    core.handleException(f);
                }
            }
        }
    }

    public class Statistics
    {
        public config.channel channel;
        public bool enabled = true;
        public bool changed = false;
        public bool Stored = false;
        public static Thread db;

        public class list : IComparable
        {
            public string user;
            public int messages = 0;
            public int longest_message = 0;
            public int average_message;
            public DateTime logging_since;
            public string URL = "";

            public int CompareTo(object O)
            {
                if (O is list)
                {
                    return this.messages.CompareTo((O as list).messages);
                }
                return 0;
            }
        }

        public List<list> data;

        public Statistics(config.channel _channel)
        {
            data = new List<list>();
            channel = _channel;
            Load();
        }

        public static string Host2Name(string host)
        {
            host = host.Replace("-", "_");
            host = host.Replace("pdpc.professional.", "");
            host = host.Replace("pdpc.active.", "");
            if (host.StartsWith("wikimedia/"))
            {
                host = host.Substring("wikipedia/".Length);
                return "https://meta.wikimedia.org/wiki/User:" + host;
            }
            else if (host.StartsWith("wikipedia/"))
            {
                host = host.Substring("wikipedia/".Length);
                return "https://en.wikipedia.org/wiki/User:" + host;
            }
            else if (host.StartsWith("mediawiki/"))
            {
                host = host.Substring("wikipedia/".Length);
                return "https://mediawiki.org/wiki/User:" + host;
            }
            return "";
        }

        public void Stat(string nick, string message, string host)
        {
            list user = null;
            lock (data)
            {
                foreach (list item in data)
                {
                    if (nick.ToUpper() == item.user.ToUpper())
                    {
                        user = item;
                        break;
                    }
                }
            }
            if (user == null)
            {
                user = new list();
                user.user = nick;
                user.logging_since = DateTime.Now;
                lock (data)
                {
                    data.Add(user);
                }
            }
            user.URL = Host2Name(host);
            user.messages++;
            changed = true;
            Stored = false;
        }

        public void Delete()
        {
            data = new List<list>();
            Save();
        }

        public bool Save()
        {
            XmlDocument stat = new XmlDocument();
            XmlNode xmlnode = stat.CreateElement("channel_stat");

            foreach (list curr in data)
            {
                XmlAttribute name = stat.CreateAttribute("username");
                name.Value = curr.user;
                XmlAttribute messages = stat.CreateAttribute("messages");
                messages.Value = curr.messages.ToString();
                XmlAttribute longest_message = stat.CreateAttribute("longest_message");
                longest_message.Value = "0";
                XmlAttribute logging_since = stat.CreateAttribute("logging_since");
                logging_since.Value = curr.logging_since.ToBinary().ToString();
                XmlAttribute link = stat.CreateAttribute("link");
                link.Value = curr.URL;
                XmlNode db = stat.CreateElement("user");
                db.Attributes.Append(name);
                db.Attributes.Append(messages);
                db.Attributes.Append(longest_message);
                db.Attributes.Append(logging_since);
                db.Attributes.Append(link);
                xmlnode.AppendChild(db);
            }
            stat.AppendChild(xmlnode);
            if (System.IO.File.Exists(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics"))
            {
                core.backupData(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics");
            }
            stat.Save(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics");
            if (System.IO.File.Exists(config.tempName(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics")))
            {
                System.IO.File.Delete(config.tempName(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics"));
            }
            return false;
        }

        public bool Load()
        {
            try
            {
                core.recoverFile(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics", channel.Name);
                if (System.IO.File.Exists(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics"))
                {
                    lock (data)
                    {
                        data = new List<list>();
                        XmlDocument stat = new XmlDocument();
                        stat.Load(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics");
                        if (stat.ChildNodes[0].ChildNodes.Count > 0)
                        {
                            foreach (XmlNode curr in stat.ChildNodes[0].ChildNodes)
                            {
                                list item = new list();
                                item.user = curr.Attributes[0].Value;
                                item.messages = int.Parse(curr.Attributes[1].Value);
                                item.logging_since = DateTime.FromBinary(long.Parse(curr.Attributes[3].Value));
                                if (curr.Attributes.Count > 4)
                                {
                                    item.URL = curr.Attributes[4].Value;
                                }
                                data.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception f)
            {
                core.handleException(f);
            }
            return false;
        }
    }
}