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

namespace wmib.Extensions
{
    public class RecentChanges
    {
        public class IWatch
        {
            public string Page;
            public string Site;

            public IWatch(string site, string page)
            {
                Page = page;
                Site = site;
            }
        }

        public static string Server = "huggle-rc.wmflabs.org";
        public List<IWatch> MonitoredPages = new List<IWatch>();
        public static List<RecentChanges> recentChangesList = new List<RecentChanges>();
        public static DateTime LastMessage = DateTime.Now;
        public static XmlRcs.Provider Provider = null;

        public Channel channel;

        public string ToTable()
        {
            string output = "<h2>Recent changes</h2>\n\n<table align=\"left\" border=1>\n";
            lock (MonitoredPages)
            {
                foreach (IWatch b in MonitoredPages)
                {
                    output += "<tr><td>" + b.Site + "</td><td>" + HttpUtility.HtmlEncode(b.Page) + "</td></tr>\n";
                }
                output += "</table>";
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

        /// <summary>
        /// Connect to huggle XmlRcs
        /// </summary>
        public static void Connect()
        {
            ModuleRC.ptrModule.Log("Connecting to huggle XmlRcs");
            RecentChanges.Provider = new XmlRcs.Provider(true, true);
            RecentChanges.Provider.Subscribe("all");
            ModuleRC.ptrModule.Log("Connected to feed - OK");
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
                    if (values.Length > 1)
                    {
                        MonitoredPages.Add(new IWatch(values[0], values[1]));
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
                        content = content + values.Site + "|" + values.Page.Replace("|", "<separator>") + "\n";
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

        public bool RemovePage(string wiki, string page)
        {
            page = page.Replace("_", " ");
            IWatch currpage = null;
            lock (MonitoredPages)
            {
                foreach (IWatch iw in MonitoredPages)
                {
                    if (iw.Page == page && wiki == iw.Site)
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

        public bool MonitorPage(string wiki, string Page)
        {
            Page = Page.Replace("_", " ");
            IWatch currpage = null;
            lock (MonitoredPages)
            {
                foreach (IWatch iw in MonitoredPages)
                {
                    if (iw.Site == wiki && iw.Page == Page)
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
                MonitoredPages.Add(new IWatch(wiki, Page));
            }
            IRC.DeliverMessage(messages.Localize("rcfeed10", channel.Language), channel);
            Module.SetConfig(channel, "HTML.Update", true);
            Save();
            return true;
        }
    }
}
