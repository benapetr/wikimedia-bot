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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Web;
using System.Xml;

namespace wmib.Extensions.RssFeed
{
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
                                      SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Reads the relevant Rss feed and returns a list of RssFeedItems
        /// </summary>
        /// <param name="url"></param>
        /// <param name="item"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static List<RssFeedItem> ReadFeed(string url, Feed.Subscription item, string channel)
        {
            string temp = "No data were sent by server";
            try
            {
                //create a new list of the rss feed items to return
                List<RssFeedItem> rssFeedItems = new List<RssFeedItem>();

                //create a http request which will be used to retrieve the rss feed
                ServicePointManager.ServerCertificateValidationCallback = Validator;
                HttpWebRequest rssFeed = (HttpWebRequest)WebRequest.Create(url);

                XmlDocument rss = new XmlDocument();
                StreamReader xx = new StreamReader(rssFeed.GetResponse().GetResponseStream());
                temp = xx.ReadToEnd();
                rss.LoadXml(temp);

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
                                            string html = HttpUtility.HtmlDecode(data.InnerText);
                                            if (html.Contains("<table>"))
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
                    IRC.DeliverMessage("Unable to parse the feed from " + url + " this url is probably not a valid rss, the feed will be disabled, until you re-enable it by typing @rss+ " + item.Name, channel);
                    return null;
                }
                item.retries--;
                return null;
            }
            catch (ThreadAbortException)
            {
                // if we receive this here it means someone wants to terminate this thread so let's quit it
                Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
                return null;
            }
            catch (Exception fail)
            {
                RSS.m.Log("Unable to parse feed from " + url + " I will try to do that again " + item.retries + " times", true);
                RSS.m.HandleException(fail, "Feed");
                string dump = Path.GetTempFileName();
                File.WriteAllText(dump, temp);
                RSS.m.Log("Dumped the source to " + dump);
                if (item.retries < 1)
                {
                    item.disabled = true;
                    IRC.DeliverMessage("Unable to parse the feed from " + url + " this url is probably not a valid rss, the feed will be disabled, until you re-enable it by typing @rss+ " + item.Name, channel);
                    return null;
                }
                item.retries--;
                return null;
            }
        }
    }
}
