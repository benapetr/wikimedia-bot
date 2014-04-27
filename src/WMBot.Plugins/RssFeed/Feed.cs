//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;

namespace wmib.Extensions.RssFeed
{
    public class RssFeedItem
    {
        public string bugzilla_product = "";
        public string bugzilla_component = "";
        public string bugzilla_assignee = "";
        public string bugzilla_reporter = "";
        public string bugzilla_status = "";
        public string bugzilla_reso = "";
        public string bugzilla_priority = "";
        public string bugzilla_severity = "";
        public string bugzilla_target = "";
        public string bugzilla_creation = "";
        /// <summary>
        /// Gets or sets the title
        /// </summary>
        public string Title = "";
        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description = "";
        /// <summary>
        /// Gets or sets the link
        /// </summary>
        public string Link = "";
        /// <summary>
        /// Gets or sets the item id
        /// </summary>
        public string ItemId = "";
        /// <summary>
        /// Gets or sets the publish date
        /// </summary>
        public string PublishDate = "";
        /// <summary>
        /// Gets or sets the channel id
        /// </summary>
        public string ChannelId = "";
        public string Author = "";
    }

    public class Feed
    {
        public class Subscription
        {
            public static int Count = 0;
            public string Name = "";
            public string URL = "";
            public List<RssFeedItem> data = null;
            public bool disabled = false;
            public string template = "";
            public bool ScannerOnly = false;
            public int retries = 0;

            public Subscription()
            {
                Count++;
                Reset();
            }
            ~Subscription()
            {
                Count--;
            }
            public void Reset()
            {
                retries = 2;
                disabled = false;
            }
        }

        public List<string> ScannerMatches = new List<string>();
        public List<Subscription> RssProviders = new List<Subscription>();
        private readonly string DB = "";
        private readonly Channel owner;

        private bool Contains(string name)
        {
            foreach (Subscription Item in RssProviders)
            {
                if (Item.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Read a configuration file
        /// </summary>
        private void Load()
        {
            try
            {
                if (File.Exists(DB))
                {
                    XmlDocument data = new XmlDocument();
                    data.Load(DB);
                    lock (RssProviders)
                    {
                        RssProviders.Clear();
                        foreach (XmlNode xx in data.ChildNodes[0].ChildNodes)
                        {
                            Subscription i = new Subscription();
                            try
                            {
                                foreach (XmlAttribute property in xx.Attributes)
                                {
                                    switch (property.Name)
                                    {
                                        case "name":
                                            i.Name = property.Value;
                                            break;
                                        case "url":
                                            i.URL = property.Value;
                                            break;
                                        case "disb":
                                        case "disabled":
                                            i.disabled = bool.Parse(property.Value);
                                            break;
                                        case "template":
                                            i.template = property.Value;
                                            break;
                                        case "so":
                                            i.ScannerOnly = bool.Parse(property.Value);
                                            break;
                                    }
                                }
                            }
                            catch (Exception fail)
                            {
                                RSS.m.HandleException(fail);
                                RSS.m.DebugLog("unable to load item for feed item name: " + i.Name + " channel name " + owner.Name + " item was removed");
                                i.disabled = false;
                            }
                            RssProviders.Add(i);
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                RSS.m.HandleException(fail);
            }
        }

        private void Save()
        {
            try
            {
                if (File.Exists(DB))
                {
                    Core.BackupData(DB);
                    if (!File.Exists(Configuration.TempName(DB)))
                    {
                        Syslog.Log("Unable to create backup file for " + owner.Name);
                    }
                }
                XmlDocument data = new XmlDocument();
                XmlNode xmlnode = data.CreateElement("database");
                lock (RssProviders)
                {
                    foreach (Subscription key in RssProviders)
                    {
                        XmlAttribute name = data.CreateAttribute("name");
                        name.Value = key.Name;
                        XmlAttribute url = data.CreateAttribute("url");
                        url.Value = key.URL;
                        XmlAttribute disabled = data.CreateAttribute("disabled");
                        disabled.Value = key.disabled.ToString();
                        XmlAttribute template = data.CreateAttribute("template");
                        template.Value = key.template;
                        XmlAttribute scan = data.CreateAttribute("so");
                        scan.Value = key.ScannerOnly.ToString();
                        XmlNode db = data.CreateElement("data");
                        db.Attributes.Append(name);
                        db.Attributes.Append(url);
                        db.Attributes.Append(disabled);
                        db.Attributes.Append(template);
                        db.Attributes.Append(scan);
                        xmlnode.AppendChild(db);
                    }
                }
                data.AppendChild(xmlnode);
                data.Save(DB);
                if (File.Exists(Configuration.TempName(DB)))
                {
                    File.Delete(Configuration.TempName(DB));
                }
            }
            catch (Exception fail)
            {
                RSS.m.HandleException(fail);
            }
        }

        private bool Matches(string text)
        {
            text = text.ToLower();
            lock (ScannerMatches)
            {
                foreach (string curr in ScannerMatches)
                {
                    if (text.Contains(curr.ToLower()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Fetch()
        {
            lock (RssProviders)
            {
                foreach (Subscription curr in RssProviders)
                {
                    if (curr.disabled)
                        continue;
                    if (curr.data == null)
                    {
                        // we didn't retrieve any list of items so far so let's get a first one and later compare it
                        curr.data = RssManager.ReadFeed(curr.URL, curr, owner.Name);
                        continue;
                    }
                    List<RssFeedItem> feed = RssManager.ReadFeed(curr.URL, curr, owner.Name);
                    if (feed == null)
                    {
                        Syslog.DebugLog("NULL feed for " + curr.Name, 6);
                        continue;
                    }
                    if (feed.Count == 0)
                    {
                        Syslog.DebugLog("0 items for " + curr.Name, 6);
                        continue;
                    }
                    // now we retrieved a new list of items
                    Syslog.DebugLog("there are " + feed.Count + "feed:" + curr.Name, 6);
                    if (!RssManager.CompareLists(curr.data, feed))
                    {
                        List<RssFeedItem> diff = new List<RssFeedItem>();
                        foreach (RssFeedItem item in feed)
                            if (!RssManager.ContainsItem(curr.data, item))
                                diff.Add(item);
                        curr.data = feed;
                        diff.Reverse();
                        foreach (RssFeedItem di in diff)
                        {
                            string description = di.Description.Replace("\n", " ");
                            if (description.Length > 200)
                            {
                                description = description.Substring(0, 200);
                            }
                            if (curr.ScannerOnly)
                            {
                                if (!Matches(di.Title) && !Matches(di.Description))
                                {
                                    continue;
                                }
                            }
                            string temp = Module.GetConfig(owner, "Rss.Style", "[$name] $title: $description $link");
                            if (curr.template != "")
                            {
                                temp = curr.template;
                            }
                            string message = temp.Replace("$link", di.Link)
                                .Replace("$title", di.Title)
                                .Replace("$name", curr.Name)
                                .Replace("$author", di.Author)
                                .Replace("$description", description)
                                .Replace("$bugzilla_assignee", di.bugzilla_assignee)
                                .Replace("$bugzilla_component", di.bugzilla_component)
                                .Replace("$bugzilla_creation", di.bugzilla_creation)
                                .Replace("$bugzilla_priority", di.bugzilla_priority)
                                .Replace("$bugzilla_product", di.bugzilla_product)
                                .Replace("$bugzilla_reporter", di.bugzilla_reporter)
                                .Replace("$bugzilla_resolution", di.bugzilla_reso)
                                .Replace("$bugzilla_severity", di.bugzilla_severity)
                                .Replace("$bugzilla_status", di.bugzilla_status)
                                .Replace("$bugzilla_target", di.bugzilla_target);
                            Core.irc.Queue.DeliverMessage(message, owner.Name, IRC.priority.low);
                        }
                    }
                }
            }
            return true;
        }

        public void StyleItem(string Name, string temp)
        {
            if (!Contains(Name))
            {
                Core.irc.Queue.DeliverMessage("I don't have this item in a db", owner.Name);
                return;
            }
            Subscription rm = null;
            lock (RssProviders)
            {
                foreach (Subscription Item in RssProviders)
                {
                    if (Item.Name == Name)
                    {
                        rm = Item;
                        break;
                    }
                }
                if (rm != null)
                {
                    rm.template = temp;
                    Save();
                    Core.irc.Queue.DeliverMessage("Item now has a different style you can restore the default style by removing this value", owner.Name);
                }
            }
        }

        public void RemoveItem(string Name)
        {
            if (!Contains(Name))
            {
                Core.irc.Queue.DeliverMessage("I don't have this item in a db", owner.Name);
                return;
            }
            Subscription rm = null;
            lock (RssProviders)
            {
                foreach (Subscription Item in RssProviders)
                {
                    if (Item.Name == Name)
                    {
                        rm = Item;
                        break;
                    }
                }
                if (rm != null)
                {
                    RssProviders.Remove(rm);
                    Save();
                    Core.irc.Queue.DeliverMessage("Item was removed from db", owner.Name);
                    return;
                }
            }
            Core.irc.Queue.DeliverMessage("Unable to remove this item from db", owner.Name);
        }

        public void InsertItem(string name, string url, bool scan = false)
        {
            if (url == "")
            {
                if (Contains(name))
                {
                    foreach (Subscription curr in RssProviders)
                    {
                        if (curr.Name == name)
                        {
                            Core.irc.Queue.DeliverMessage("This item was enabled now", owner.Name);
                            curr.Reset();
                            return;
                        }
                    }
                }
                Core.irc.Queue.DeliverMessage("There is no such item, if you want to define new item, please use 2 parameters", owner.Name);
                return;
            }
            if (!Contains(name))
            {
                Subscription item = new Subscription { Name = name, ScannerOnly = scan, URL = url, template = "" };
                lock (RssProviders)
                {
                    RssProviders.Add(item);
                }
                Save();
                Core.irc.Queue.DeliverMessage("Item was inserted to feed", owner.Name);
                return;
            }
            Core.irc.Queue.DeliverMessage("This item already exist", owner.Name);
        }

        public Feed(Channel _owner)
        {
            DB = Variables.ConfigurationDirectory + "/" + _owner.Name + "_feed.xml";
            owner = _owner;
            Load();
        }
    }
}
