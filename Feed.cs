using System;
using System.Collections.Generic;
using System.Net;
using System.Data;
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

    public static class RssManager
    {
        public static bool CompareLists(List<RssFeedItem> source, List<RssFeedItem> target)
        {
            if (source == null || target == null)
            {
                return true;
            }
            if (target.Count != source.Count)
            {
                return false;
            }
            int curr = 0;
            foreach (RssFeedItem item in source)
            {
                if (item.Link != target[curr].Link || item.Description != target[curr].Description || target[curr].PublishDate != item.PublishDate)
                {
                    return false;
                }
                curr++;
            }
            return true;
        }

        public static bool ContainsItem(List<RssFeedItem> list, RssFeedItem item)
        {
            foreach (RssFeedItem Item in list)
            {
                if (Item.Link == item.Link && Item.Title == item.Title && item.Description == Item.Description && Item.PublishDate == item.PublishDate)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain,
                                      System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Reads the relevant Rss feed and returns a list of RssFeedItems
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<RssFeedItem> ReadFeed(string url, Feed.item item, string channel)
        {
            try
            {
                //create a new list of the rss feed items to return
                List<RssFeedItem> rssFeedItems = new List<RssFeedItem>();

                //create a http request which will be used to retrieve the rss feed
                ServicePointManager.ServerCertificateValidationCallback = Validator;
                HttpWebRequest rssFeed = (HttpWebRequest)WebRequest.Create(url);

                XmlDocument rss = new XmlDocument();
                rss.Load(rssFeed.GetResponse().GetResponseStream());

                if (url.StartsWith("http://bugzilla.wikimedia") || url.StartsWith("https://bugzilla.wikimedia"))
                {
                    if (rss.ChildNodes[1].Name.ToLower() == "feed")
                    {
                        foreach (XmlNode entry in rss.ChildNodes[1].ChildNodes)
                        {
                            if (entry.Name == "entry")
                            {
                                RssFeedItem curr = new RssFeedItem();
                                foreach (XmlNode data in entry.ChildNodes)
                                {
                                    switch (data.Name.ToLower())
                                    {
                                        case "title":
                                            curr.Title = data.InnerText;
                                            break;
                                        case "link":
                                            foreach (XmlAttribute attribute in data.Attributes)
                                            {
                                                if (attribute.Name == "href")
                                                {
                                                    curr.Link = attribute.Value;
                                                }
                                            }
                                            break;
                                        case "author":
                                            if (data.ChildNodes.Count > 0)
                                            {
                                                curr.Author = data.ChildNodes[0].InnerText;
                                            }
                                            break;
                                        case "summary":
                                            string html = System.Web.HttpUtility.HtmlDecode(data.InnerText);
                                            if (html.Contains("<table>"))
                                            {
                                                try
                                                {
                                                    XmlDocument summary = new XmlDocument();
                                                    summary.LoadXml(html);
                                                    foreach (XmlNode tr in summary.ChildNodes[0].ChildNodes)
                                                    {
                                                        bool type = true;
                                                        string st = "";
                                                        foreach (XmlNode td in tr.ChildNodes)
                                                        {
                                                            if (type)
                                                            {
                                                                st = td.InnerText;
                                                            }
                                                            else
                                                            {
                                                                switch (st.Replace(" ", ""))
                                                                {
                                                                    case "Product":
                                                                        curr.bugzilla_product = td.InnerText;
                                                                        break;
                                                                    case "Status":
                                                                        curr.bugzilla_status = td.InnerText;
                                                                        break;
                                                                    case "Component":
                                                                        curr.bugzilla_component = td.InnerText;
                                                                        break;
                                                                    case "Assignee":
                                                                        curr.bugzilla_assignee = td.InnerText;
                                                                        break;
                                                                    case "Reporter":
                                                                        curr.bugzilla_reporter = td.InnerText;
                                                                        break;
                                                                    case "Resolution":
                                                                        curr.bugzilla_reso = td.InnerText;
                                                                        break;
                                                                    case "Priority":
                                                                        curr.bugzilla_priority = td.InnerText;
                                                                        break;
                                                                    case "Severity":
                                                                        curr.bugzilla_severity = td.InnerText;
                                                                        break;
                                                                }
                                                            }
                                                            type = !type;
                                                        }
                                                    }
                                                }
                                                catch (Exception)
                                                {

                                                }
                                            }
                                            break;
                                        case "guid":
                                            curr.ItemId = data.InnerText;
                                            break;
                                        case "channelid":
                                            curr.ChannelId = data.InnerText;
                                            break;
                                        case "date":
                                            curr.PublishDate = data.Value;
                                            break;
                                    }
                                }
                                rssFeedItems.Add(curr);
                            }
                        }

                        return rssFeedItems;
                    }
                }

                if (rss.ChildNodes[1].Name.ToLower() == "feed")
                {
                    foreach (XmlNode entry in rss.ChildNodes[1].ChildNodes)
                    {
                        if (entry.Name == "entry")
                        {
                            RssFeedItem curr = new RssFeedItem();
                            foreach (XmlNode data in entry.ChildNodes)
                            {
                                switch (data.Name.ToLower())
                                {
                                    case "title":
                                        curr.Title = data.InnerText;
                                        break;
                                    case "link":
                                        foreach (XmlAttribute attribute in data.Attributes)
                                        {
                                            if (attribute.Name == "href")
                                            {
                                                curr.Link = attribute.Value;
                                            }
                                        }
                                        break;
                                    case "author":
                                        if (data.ChildNodes.Count > 0)
                                        {
                                            curr.Author = data.ChildNodes[0].InnerText;
                                        }
                                        break;
                                    case "summary":
                                        curr.Description = data.InnerText;
                                        break;
                                    case "guid":
                                        curr.ItemId = data.InnerText;
                                        break;
                                    case "channelid":
                                        curr.ChannelId = data.InnerText;
                                        break;
                                    case "date":
                                        curr.PublishDate = data.Value;
                                        break;
                                }
                            }
                            rssFeedItems.Add(curr);
                        }
                    }

                    return rssFeedItems;
                }

                foreach (XmlNode node in rss.ChildNodes)
                {
                    if (node.Name.ToLower() == "rss" || node.Name.ToLower() == "channel")
                    {
                        foreach (XmlNode entry in node.ChildNodes[0].ChildNodes)
                        {
                            if (entry.Name == "item")
                            {
                                RssFeedItem curr = new RssFeedItem();
                                foreach (XmlNode data in entry.ChildNodes)
                                {
                                    switch (data.Name.ToLower())
                                    {
                                        case "title":
                                            curr.Title = data.InnerText;
                                            break;
                                        case "link":
                                            curr.Link = data.InnerText;
                                            break;
                                        case "description":
                                            curr.Description = data.InnerText;
                                            break;
                                        case "guid":
                                            curr.ItemId = data.InnerText;
                                            break;
                                        case "channelid":
                                            curr.ChannelId = data.InnerText;
                                            break;
                                        case "date":
                                            curr.PublishDate = data.Value;
                                            break;
                                    }
                                }
                                rssFeedItems.Add(curr);
                            }
                        }

                        return rssFeedItems;
                    }
                }
                if (item.retries < 1)
                {
                    item.disabled = true;
                    core.irc._SlowQueue.DeliverMessage("Unable to parse the feed from " + url + " this url is probably not a valid rss, the feed will be disabled, until you re-enable it by typing @rss+ " + item.name, channel);
                    return null;
                }
                item.retries--;
                return null;
            }
            catch (Exception fail)
            {
                Program.Log("Unable to parse feed from " + url + " I will try to do that again " + item.retries.ToString() + " times", true);
                core.handleException(fail);
                if (item.retries < 1)
                {
                    item.disabled = true;
                    core.irc._SlowQueue.DeliverMessage("Unable to parse the feed from " + url + " this url is probably not a valid rss, the feed will be disabled, until you re-enable it by typing @rss+ " + item.name, channel);
                    return null;
                }
                item.retries--;
                return null;
            }
        }
    }

    public class module_feed : Module
    {
        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (message.StartsWith("@rss- "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "trust"))
                {
                    string item = message.Substring("@rss+ ".Length);
                    Feed feed = (Feed)channel.RetrieveObject("rss");
                    if (feed != null)
                    {
                        feed.RemoveItem(item);
                    }
                    return;
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
            }

            if (message.StartsWith("@rss-setstyle "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "trust"))
                {
                    string item = message.Substring("@rss-setstyle ".Length);
                    if (item.Contains(" "))
                    {
                        string id = item.Substring(0, item.IndexOf(" "));
                        string ur = item.Substring(item.IndexOf(" ") + 1);
                        Feed feed = (Feed)channel.RetrieveObject("rss");
                        if (feed != null)
                        {
                            feed.StyleItem(id, ur);
                        }
                        return;
                    }
                    if (item != "")
                    {
                        Feed feed = (Feed)channel.RetrieveObject("rss");
                        if (feed != null)
                        {
                            feed.StyleItem(item, "");
                        }
                        return;
                    }
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss5", channel.Language), channel.Name, IRC.priority.low);
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

            if (message.StartsWith("@rss+ "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "trust"))
                {
                    string item = message.Substring("@rss+ ".Length);
                    if (item.Contains(" "))
                    {
                        string id = item.Substring(0, item.IndexOf(" "));
                        string ur = item.Substring(item.IndexOf(" ") + 1);
                        Feed feed = (Feed)channel.RetrieveObject("rss");
                        if (feed != null)
                        {
                            feed.InsertItem(id, ur);
                        }
                        return;
                    }
                    if (item != "")
                    {
                        Feed feed = (Feed)channel.RetrieveObject("rss");
                        if (feed != null)
                        {
                            feed.InsertItem(item, "");
                        }
                        return;
                    }
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss5", channel.Language), channel.Name, IRC.priority.low);
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
            if (message == "@rss-off")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!GetConfig(channel, "RSS.Enable", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss1", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        SetConfig(channel, "RSS.Enable", false);
                        //channel.EnableRss = false;
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss2", channel.Language), channel.Name);
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

            if (message == "@rss-on")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (GetConfig(channel, "RSS.Enable", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss3", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss4", channel.Language), channel.Name);
                        SetConfig(channel, "RSS.Enable", true);
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

        public override void Hook_BeforeSysWeb(ref string html)
        {
            html += "\n<br><br>Rss feeds: " + Feed.item.Count.ToString() + "\n";
        }

        public override bool Hook_SetConfig(config.channel chan, User invoker, string config, string value)
        {
            if (config == "style-rss")
            {
                if (value != "")
                {
                    SetConfig(chan, "Feed.Style", value);
                    chan.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                    return true;
                }
                core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                return true;
            }
            return false;
        }

        public override bool Hook_OnRegister()
        {
            bool done = true;
            foreach (config.channel chan in config.channels)
            {
                if (!chan.RegisterObject(new Feed(chan), "Rss"))
                {
                    done = false;
                }
            }

            return done;
        }

        public override bool Hook_OnUnload()
        {
            bool done = true;
            foreach (config.channel chan in config.channels)
            {
                if (!chan.UnregisterObject("Rss"))
                {
                    done = false;
                }
            }

            return done;
        }

        public override bool Construct()
        {
            base.Create("Feed", true);
            Version = "1.0.0";
            return true;
        }

        public override void Load()
        {
            try
            {
                while (true)
                {
                    lock (config.channels)
                    {
                        foreach (config.channel channel in config.channels)
                        {
                            if (GetConfig(channel, "RSS.Enable", false))
                            {
                                Feed feed = (Feed)channel.RetrieveObject("rss");
                                if (feed != null)
                                {
                                        feed.Recreate();
                                }
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(10000);
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }
    }

    public class Feed
    {
        public class item
        {
            public static int Count = 0;
            public string name;
            public string URL;
            public List<RssFeedItem> data = null;
            public bool disabled;
            public string message = "";
            public int retries = 0;
            public void reset()
            {
                retries = 2;
            }
            public item()
            {
                Count++;
                reset();
                disabled = false;
            }
            ~item()
            {
                Count = Count - 1;
            }
        }

        public List<item> Content = new List<item>();

        public string DB = "";

        private config.channel owner = null;

        public bool contains(string name)
        {
            foreach (item Item in Content)
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
                            item i = new item();
                            i.name = xx.Attributes[0].Value;
                            i.URL = xx.Attributes[1].Value;
                            try
                            {
                                if (xx.Attributes.Count > 1)
                                {
                                    i.disabled = bool.Parse(xx.Attributes[2].Value);
                                }
                                if (xx.Attributes.Count > 2)
                                {
                                    i.message = xx.Attributes[3].Value;
                                }
                            }
                            catch (Exception)
                            {
                                i.disabled = false;
                            }
                            Content.Add(i);
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
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
                        Program.Log("Unable to create backup file for " + owner.Name);
                    }
                }
                System.Xml.XmlDocument data = new System.Xml.XmlDocument();
                System.Xml.XmlNode xmlnode = data.CreateElement("database");

                lock (Content)
                {
                    foreach (item key in Content)
                    {
                        XmlAttribute name = data.CreateAttribute("name");
                        name.Value = key.name;
                        XmlAttribute url = data.CreateAttribute("url");
                        url.Value = key.URL;
                        XmlAttribute rn = data.CreateAttribute("disb");
                        rn.Value = key.disabled.ToString();
                        XmlAttribute template = data.CreateAttribute("template");
                        template.Value = key.message;
                        System.Xml.XmlNode db = data.CreateElement("data");
                        db.Attributes.Append(name);
                        db.Attributes.Append(url);
                        db.Attributes.Append(rn);
                        db.Attributes.Append(template);
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
                core.handleException(fail);
            }
        }

        public bool Recreate()
        {
            try
            {
                lock (Content)
                {
                    foreach (item curr in Content)
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
                                continue;
                            }
                            if (feed.Count == 0)
                            {
                                continue;
                            }
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

                                    string temp = owner.Extension_GetConfig("Feed.Style");

                                    if (curr.message != "")
                                    {
                                        temp = curr.message;
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
                Program.Log("Unable to handle rss in " + owner.Name, true);
                core.handleException(fail);
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
            item rm = null;
            lock (Content)
            {
                foreach (item Item in Content)
                {
                    if (Item.name == Name)
                    {
                        rm = Item;
                        break;
                    }
                }
                if (rm != null)
                {
                    rm.message = temp;
                    Save();
                    core.irc._SlowQueue.DeliverMessage("Item now has a different style you can restore default style by removing this value", owner.Name);
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
            item rm = null;
            lock (Content)
            {
                foreach (item Item in Content)
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

        public void InsertItem(string name, string url)
        {
            if (url == "")
            {
                if (contains(name))
                {
                    foreach (item curr in Content)
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
                item Item = new item();
                Item.name = name;
                Item.URL = url;
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
