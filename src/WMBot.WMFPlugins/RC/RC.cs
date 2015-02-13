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
using System.IO;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;

namespace wmib.Extensions
{
    public class RecentChanges
    {
        public class IWatch
        {
            public string Page;
            public wiki Site;

            public IWatch(wiki site, string page)
            {
                Page = page;
                Site = site;
            }
        }

        public class wiki
        {
            public string name;
            public string channel;
            public string url;

            public wiki(string _channel, string _url, string _name)
            {
                url = _url;
                name = _name;
                channel = _channel;
            }
        }

        public static readonly wiki all = new wiki("all", "all", "unknown");
        public static string Server = "huggle-rc.wmflabs.org";
        public List<IWatch> MonitoredPages = new List<IWatch>();
        private static readonly List<wiki> Wikis = new List<wiki>();
        public static List<string> channels = new List<string>();
        public static List<RecentChanges> recentChangesList = new List<RecentChanges>();
        public static DateTime LastMessage = DateTime.Now;
        public static string WikiFile = Variables.ConfigurationDirectory + "/feed";
        public static XmlRcs.Provider Provider = null;

        public Channel channel;

        public string ToTable()
        {
            string output = "<h2>Recent changes</h2>\n\n<table align=\"left\" border=1>\n";
            lock (MonitoredPages)
            {
                foreach (IWatch b in MonitoredPages)
                {
                    string wiki;
                    if (b.Site == null || b.Site.channel == null)
                        wiki = "unknown";
                    else
                        wiki = b.Site.channel;
                    output += "<tr><td>" + wiki + "</td><td>" + HttpUtility.HtmlEncode(b.Page) + "</td></tr>\n";
                }
                output = output + "</table>";
            }
            return output;
        }

        public RecentChanges(Channel _channel)
        {
            channel = _channel;
            Load();
            lock (recentChangesList)
            {
                recentChangesList.Add(this);
            }
        }

        public static wiki WikiFromChannelID(string channel)
        {
            lock (Wikis)
            {
                foreach (wiki w in Wikis)
                {
                    if (w.channel == channel)
                    {
                        return w;
                    }
                }
            }
            return all;
        }

        /// <summary>
        /// New channel to watch by a bot
        /// </summary>
        /// <param name="target">Object to send output to</param>
        /// <param name="name">Name of wiki</param>
        /// <returns></returns>
        public static bool InsertChannel(Channel target, string name)
        {
            try
            {
                wiki web = null;
                foreach (wiki site in Wikis)
                {
                    if (name == site.name)
                    {
                        web = site;
                        break;
                    }
                }
                if (web == null)
                {
                    IRC.DeliverMessage(messages.Localize("rcfeed1", target.Language), target.Name);
                    return false;
                }
                if (channels.Contains(web.channel))
                {
                    IRC.DeliverMessage(messages.Localize("rcfeed2", target.Language), target.Name);
                    return false;
                }
                channels.Add(web.channel);
                RecentChanges.Provider.Subscribe(web.channel);
                File.WriteAllText(WikiFile, "");
                foreach (string x in channels)
                {
                    File.AppendAllText(WikiFile, x + "\n");
                }
            }
            catch (Exception f)
            {
                Core.HandleException(f, "RC");
            }
            return true;
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="target">Object to get output back to</param>
        /// <param name="WikiName">Name of site</param>
        /// <returns></returns>
        public static bool DeleteChannel(Channel target, string WikiName)
        {
            wiki W = null;
            try
            {
                foreach (wiki site in Wikis)
                {
                    if (WikiName == site.name)
                    {
                        W = site;
                        break;
                    }
                }
                if (W == null)
                {
                    IRC.DeliverMessage(messages.Localize("rcfeed1", target.Language), target);
                    return false;
                }
                if (!channels.Contains(W.channel))
                {
                    IRC.DeliverMessage(messages.Localize("rcfeed3", target.Language), target);
                    return false;
                }
                channels.Remove(W.channel);
                RecentChanges.Provider.Unsubscribe(W.channel);
                File.WriteAllText(WikiFile, "");
                foreach (string x in channels)
                {
                    File.AppendAllText(WikiFile, x + "\n");
                }
            }
            catch (Exception f)
            {
                Core.HandleException(f, "RC");
            }
            return true;
        }

        /// <summary>
        /// Connect to huggle XmlRcs
        /// </summary>
        public static void Connect()
        {
            ModuleRC.ptrModule.Log("Connecting to huggle XmlRcs");
            RecentChanges.Provider = new XmlRcs.Provider(true, true);
            lock (channels)
            {
                foreach (string b in channels)
                {
                    RecentChanges.Provider.Subscribe(b);
                }
            }
            ModuleRC.ptrModule.Log("Connected to feed - OK");
        }

        /// <summary>
        /// get the wiki from a name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private static wiki getWiki(string Name)
        {
            if (Name == "all" || Name == "unknown")
                return all;

            foreach (wiki curr in Wikis)
            {
                if (curr.name == Name)
                {
                    return curr;
                }
            }
            ModuleRC.ptrModule.Log("There is no wiki " + Name + " known by me");
            return null;
        }

        /// <summary>
        /// Load the list
        /// </summary>
        public void Load()
        {
            string name = Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".list";
            Core.RecoverFile(name, channel.Name);
            if (!File.Exists(name))
                return;
            string[] content = File.ReadAllLines(name);
            lock (MonitoredPages)
            {
                MonitoredPages.Clear();
                foreach (string value in content)
                {
                    string[] values = value.Split('|');
                    if (values.Length > 2)
                    {
                        wiki Wiki = getWiki(values[0]);
                        if (Wiki == null)
                        {
                            Syslog.WarningLog("Unknown wiki: " + values[0] + " dropping subscription from " + this.channel.Name);
                            continue;
                        }
                        MonitoredPages.Add(new IWatch(Wiki, values[1].Replace("<separator>", "|")));
                    }
                }
            }
        }

        /// <summary>
        /// Save the list
        /// </summary>
        public void Save()
        {
            string dbn = Variables.ConfigurationDirectory + "/" + channel.Name + ".list";
            try
            {
                string content = "";
                Core.BackupData(dbn);
                lock (MonitoredPages)
                {
                    foreach (IWatch values in MonitoredPages)
                    {
                        if (values == null || values.Site == null || values.Page == null)
                            continue;
                        content = content + values.Site.name + "|" + values.Page.Replace("|", "<separator>") + "\n";
                    }
                }
                File.WriteAllText(dbn, content);
                File.Delete(Configuration.TempName(dbn));
            }
            catch (Exception er)
            {
                ModuleRC.ptrModule.Log("Error while saving to: " + channel.Name + ".list");
                Core.HandleException(er, "RC");
                Core.RecoverFile(dbn, channel.Name);
            }
        }

        public bool removeString(string WS, string Page)
        {
            Page = Page.Replace("_", " ");
            wiki site = getWiki(WS);
            if (site != null)
            {
                if (WS == "all" || channels.Contains(site.channel))
                {
                    IWatch currpage = null;
                    lock (MonitoredPages)
                    {
                        foreach (IWatch iw in MonitoredPages)
                        {
                            if (iw.Page == Page && site.channel == iw.Site.channel)
                            {
                                currpage = iw;
                                break;
                            }
                        }
                        if (MonitoredPages.Contains(currpage))
                        {
                            MonitoredPages.Remove(currpage);
                            Module.SetConfig(channel, "HTML.Update", true);
                            Save();
                            IRC.DeliverMessage(messages.Localize("rcfeed4", channel.Language), channel);
                            return true;
                        }
                    }
                    IRC.DeliverMessage(messages.Localize("rcfeed5", channel.Language), channel);
                    return true;
                }
                IRC.DeliverMessage(messages.Localize("rcfeed6", channel.Language), channel);
                return false;
            }
            IRC.DeliverMessage(messages.Localize("rcfeed7", channel.Language), channel);
            return false;
        }

        public static int InsertSite()
        {
            if (File.Exists("sites"))
            {
                string[] content = File.ReadAllLines("sites");
                foreach (string a in content)
                {
                    string[] values = a.Split('|');
                    if (values.Length == 3)
                    {
                        Wikis.Add(new wiki(values[0], values[1], values[2]));
                    }
                }
                ModuleRC.ptrModule.Log("Loaded wiki " + content.Length);
            }
            else
            {
                ModuleRC.ptrModule.Log("There is no sites file, skipping load", true);
            }
            Wikis.Add(new wiki("mediawiki.org", "https://www.mediawiki.org/w/index.php", "mediawiki"));
            Wikis.Add(new wiki("test.wikipedia.org", "https://test.wikipedia.org/w/index.php", "test_wikipedia"));
            return 0;
        }

        public bool insertString(string WS, string Page)
        {
            wiki site = getWiki(WS);
            Page = Page.Replace("_", " ");
            if (site != null)
            {
                if (WS == "all" || channels.Contains(site.channel))
                {
                    IWatch currpage = null;
                    lock (MonitoredPages)
                    {
                        foreach (IWatch iw in MonitoredPages)
                        {
                            if (iw.Site.channel == site.channel && iw.Page == Page)
                            {
                                currpage = iw;
                                break;
                            }
                        }
                    }
                    if (Page.Contains("*"))
                    {
                        if (!Page.EndsWith("*") || Page.Replace("*", "") == "")
                        {
                            IRC.DeliverMessage(messages.Localize("rcfeed8", channel.Language), channel);
                            return true;
                        }
                    }
                    if (MonitoredPages.Contains(currpage))
                    {
                        IRC.DeliverMessage(messages.Localize("rcfeed9", channel.Language), channel);
                        return true;
                    }
                    lock (MonitoredPages)
                    {
                        MonitoredPages.Add(new IWatch(site, Page));
                    }
                    IRC.DeliverMessage(messages.Localize("rcfeed10", channel.Language), channel);
                    Module.SetConfig(channel, "HTML.Update", true);
                    Save();
                    return true;
                }
                IRC.DeliverMessage(messages.Localize("rcfeed11", channel.Language), channel);
                return false;
            }
            IRC.DeliverMessage(messages.Localize("rcfeed12", channel.Language), channel);
            return false;
        }
    }
}
