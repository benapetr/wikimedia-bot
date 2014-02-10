using System;
using System.Collections.Generic;
using System.Net;
using System.Data;
using System.Threading;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text;

namespace wmib
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
        public class Item
        {
            public static int Count = 0;
            public string name = "";
            public string URL = "";
            public List<RssFeedItem> data = null;
            public bool disabled = false;
            public string template = "";
            public bool ScannerOnly = false;
            public int retries = 0;
            public void reset()
            {
                retries = 2;
            }
            public Item()
            {
                Count++;
                reset();
                disabled = false;
            }
            ~Item()
            {
                Count = Count - 1;
            }
        }

        public List<string> ScannerMatches = new List<string>();
        public List<Item> Content = new List<Item>();

        public string DB = "";

        private config.channel owner = null;

        public bool contains(string name)
        {
            foreach (Item Item in Content)
            {
                if (Item.name == name)
                {
                    return true;
                }
            }
            return false;
        }

        public void Load()
        {
            try
            {
                if (File.Exists(DB))
                {
                    System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                    data.Load(DB);
                    lock (Content)
                    {
                        Content.Clear();
                        foreach (System.Xml.XmlNode xx in data.ChildNodes[0].ChildNodes)
                        {
                            Item i = new Item();
							try
							{
								foreach (System.Xml.XmlAttribute property in xx.Attributes)
								{
									switch (property.Name)
									{
									case "name":
										i.name = property.Value;
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
                            catch (Exception)
                            {
                                RSS.m.DebugLog("unable to load item for feed item name: " + i.name + " channel name " + owner.Name + " item was removed");
                                i.disabled = false;
                            }
                            Content.Add(i);
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                RSS.m.handleException(fail, "Feed");
            }
        }

        public void Save()
        {
            try
            {
                if (File.Exists(DB))
                {
                    core.backupData(DB);
                    if (!File.Exists(config.tempName(DB)))
                    {
                        core.Log("Unable to create backup file for " + owner.Name);
                    }
                }
                System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                System.Xml.XmlNode xmlnode = data.CreateElement("database");

                lock (Content)
                {
                    foreach (Item key in Content)
                    {
                        XmlAttribute name = data.CreateAttribute("name");
                        name.Value = key.name;
                        XmlAttribute url = data.CreateAttribute("url");
                        url.Value = key.URL;
                        XmlAttribute disabled = data.CreateAttribute("disb");
						disabled.Value = key.disabled.ToString();
                        XmlAttribute template = data.CreateAttribute("template");
                        template.Value = key.template;
                        XmlAttribute scan = data.CreateAttribute("so");
                        template.Value = key.ScannerOnly.ToString();
                        System.Xml.XmlNode db = data.CreateElement("data");
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
                if (System.IO.File.Exists(config.tempName(DB)))
                {
                    System.IO.File.Delete(config.tempName(DB));
                }
            }
            catch (Exception fail)
            {
                RSS.m.handleException(fail);
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
            try
            {
                lock (Content)
                {
                    foreach (Item curr in Content)
                    {
                        if (!curr.disabled && curr.data == null)
                        {
                            curr.data = RssManager.ReadFeed(curr.URL, curr, owner.Name);
                            continue;
                        }
                        if (!curr.disabled)
                        {
                            List<RssFeedItem> feed = RssManager.ReadFeed(curr.URL, curr, owner.Name);
                            if (feed == null)
                            {
                                core.DebugLog("NULL feed for " + curr.name, 6);
                                continue;
                            }
                            if (feed.Count == 0)
                            {
                                core.DebugLog("0 items for " + curr.name, 6);
                                continue;
                            }
                            core.DebugLog("there are " + feed.Count.ToString() + "feed:" + curr.name, 6);
                            if (!RssManager.CompareLists(curr.data, feed))
                            {
                                List<RssFeedItem> diff = new List<RssFeedItem>();
                                foreach (RssFeedItem item in feed)
                                {
                                    if (!RssManager.ContainsItem(curr.data, item))
                                    {
                                        diff.Add(item);
                                    }
                                }
                                curr.data = feed;
                                diff.Reverse();
                                foreach (RssFeedItem di in diff)
                                {
                                    string message = "";
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

                                    message = temp.Replace("$link", di.Link)
                                        .Replace("$title", di.Title)
                                        .Replace("$name", curr.name)
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
                                    core.irc._SlowQueue.DeliverMessage(message, owner.Name, IRC.priority.low);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                RSS.m.Log("Unable to handle rss in " + owner.Name, true);
                RSS.m.handleException(fail, "Feed");
            }
            return true;
        }

        public void StyleItem(string Name, string temp)
        {
            if (!contains(Name))
            {
                core.irc._SlowQueue.DeliverMessage("I don't have this item in a db", owner.Name);
                return;
            }
            Item rm = null;
            lock (Content)
            {
                foreach (Item Item in Content)
                {
                    if (Item.name == Name)
                    {
                        rm = Item;
                        break;
                    }
                }
                if (rm != null)
                {
                    rm.template = temp;
                    Save();
                    core.irc._SlowQueue.DeliverMessage("Item now has a different style you can restore the default style by removing this value", owner.Name);
                    return;
                }
            }
        }

        public void RemoveItem(string Name)
        {
            if (!contains(Name))
            {
                core.irc._SlowQueue.DeliverMessage("I don't have this item in a db", owner.Name);
                return;
            }
            Item rm = null;
            lock (Content)
            {
                foreach (Item Item in Content)
                {
                    if (Item.name == Name)
                    {
                        rm = Item;
                        break;
                    }
                }
                if (rm != null)
                {
                    Content.Remove(rm);
                    Save();
                    core.irc._SlowQueue.DeliverMessage("Item was removed from db", owner.Name);
                    return;
                }
            }
            core.irc._SlowQueue.DeliverMessage("Unable to remove this item from db", owner.Name);
        }

        public void InsertItem(string name, string url, bool scan = false)
        {
            if (url == "")
            {
                if (contains(name))
                {
                    foreach (Item curr in Content)
                    {
                        if (curr.name == name)
                        {
                            core.irc._SlowQueue.DeliverMessage("This item was enabled now", owner.Name);
                            curr.reset();
                            curr.disabled = false;
                            return;
                        }
                    }
                }
                core.irc._SlowQueue.DeliverMessage("There is no such item, if you want to define new item, please use 2 parameters", owner.Name);
                return;
            }
            if (!contains(name))
            {
                Item Item = new Item();
                Item.name = name;
                Item.ScannerOnly = scan;
                Item.URL = url;
				Item.template = "";
                lock (Content)
                {
                    Content.Add(Item);
                }
                Save();
                core.irc._SlowQueue.DeliverMessage("Item was inserted to feed", owner.Name);
                return;
            }
            core.irc._SlowQueue.DeliverMessage("This item already exist", owner.Name);
        }

        public bool Delete()
        {
            if (System.IO.File.Exists(DB))
            {
                System.IO.File.Delete(DB);
                return true;
            }
            return false;
        }

        public Feed(config.channel _owner)
        {
            DB = variables.config + "/" + _owner.Name + "_feed.xml";
            owner = _owner;
            Load();
        }
    }
}
