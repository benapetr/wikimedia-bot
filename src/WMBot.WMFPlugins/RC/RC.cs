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
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace wmib
{
    public class Change
    {
        public string Page;
        public string Summary;
        public string User;
        public bool Bot = false;
        public bool Minor = false;
        public string Size = null;
        public bool New = false;
        public string oldid = null;
        public string diff = null;
        public bool Special = true;

        public Change(string _Page, string _Description, string _User)
        {
            Summary = _Description;
            User = _User;
            Page = _Page;
        }
    }

    public class RecentChanges
    {
        public class IWatch
        {
            public string Channel;
            public string Page;
            public wiki URL;

            public IWatch(wiki site, string page, string channel)
            {
                Channel = channel;
                Page = page;
                URL = site;
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

        private static readonly wiki all = new wiki("all", "all", "unknown");

        /// <summary>
        /// List of pages
        /// </summary>
        public List<IWatch> MonitoredPages = new List<IWatch>();

        /// <summary>
        /// Wiki
        /// </summary>
        private static readonly List<wiki> wikiinfo = new List<wiki>();

        /// <summary>
        /// Nickname in feed
        /// </summary>
        private static string Nick;

        private static bool Loaded;

        /// <summary>
        /// Channels
        /// </summary>
        public static List<string> channels = new List<string>();

        public static bool terminated = false;

        /// <summary>
        /// feed
        /// </summary>
        public static List<RecentChanges> rc = new List<RecentChanges>();

        /// <summary>
        /// Stream reader
        /// </summary>
        public static StreamReader RD;

        public static string WikiFile = Variables.ConfigurationDirectory + "/feed";
        public static StreamWriter WD;
        public static NetworkStream stream;

        public static Regex RegexIrc =
            new Regex(":rc-pmtpa!~rc-pmtpa@[^ ]* PRIVMSG #[^:]*:14\\[\\[07([^]*)14\\]\\]4 N?(M?)(B?)10 02.*di" +
                      "ff=([^&]*)&oldid=([^]*) 5\\* 03([^]*) 5\\* \\(?([^]*)?\\) 10([^]*)?");

        public Channel channel;

        ~RecentChanges()
        {
            try
            {
                lock (rc)
                {
                    rc.Remove(this);
                }
            }
            catch (Exception er)
            {
                Core.HandleException(er, "RC");
            }
        }

        public string ToTable()
        {
            string output = "<table align=\"left\" border=1>\n";
            try
            {
                lock (MonitoredPages)
                {
                    foreach (IWatch b in MonitoredPages)
                    {
                        output = output + "<tr><td>" + b.Channel + "</td><td>" + HttpUtility.HtmlEncode(b.Page) + "</td></tr>\n";
                    }
                    output = output + "</table>";
                }
                return output;
            }
            catch (Exception fail)
            {
                Core.HandleException(fail, "RC");
                return "";
            }
        }

        public static bool Send(string _n)
        {
            WD.WriteLine(_n);
            Core.TrafficLog("RX >>>>>>" + _n);
            return true;
        }

        public RecentChanges(Channel _channel)
        {
            channel = _channel;
            Load();
            lock (rc)
            {
                rc.Add(this);
            }
        }

        public static wiki WikiFromChannelID(string channel)
        {
            lock(wikiinfo)
            {
                foreach (wiki w in wikiinfo)
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
                if (Loaded == false)
                {
                    IRC.DeliverMessage(messages.Localize("rcfeed13", target.Language), target.Name);
                    return false;
                }
                foreach (wiki site in wikiinfo)
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
                WD.WriteLine("JOIN " + web.channel);
                WD.Flush();
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
            if (Loaded == false)
            {
                IRC.DeliverMessage(messages.Localize("rcfeed13", target.Language), target);
                return false;
            }
            try
            {
                foreach (wiki site in wikiinfo)
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
                Send("PART " + W.channel);
                WD.Flush();
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
        /// Connect to wm irc
        /// </summary>
        public static void Connect()
        {
            if (!terminated)
            {
                Random rand = new Random(DateTime.Now.Millisecond);
                int random_number = rand.Next(10000);
                Nick = "wm-bot" + random_number;
                ModuleRC.ptrModule.Log("Connecting to wikimedia recent changes feed as " + Nick + ", hold on");
                stream = new TcpClient("irc.wikimedia.org", 6667).GetStream();
                WD = new StreamWriter(stream);
                RD = new StreamReader(stream, Encoding.UTF8);
                Thread pinger = new Thread(Pong) {Name = "Module:RC/Pinger"};
                Core.ThreadManager.RegisterThread(pinger);
                Send("USER " + "wm-bot" + " 8 * :" + "wm-bot");
                Send("NICK " + Nick);
                WD.Flush();
                pinger.Start();
                lock (channels)
                {
                    foreach (string b in channels)
                    {
                        Thread.Sleep(800);
                        Send("JOIN " + b);
                        WD.Flush();
                    }
                }
                ModuleRC.ptrModule.Log("Connected to feed - OK");
                Loaded = true;
            }
        }

        /// <summary>
        /// get the wiki from a name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private static wiki getWiki(string Name)
        {
            if (Name == "all")
            {
                return all;
            }
            foreach (wiki curr in wikiinfo)
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
            if (File.Exists(name))
            {
                string[] content = File.ReadAllLines(name);
                lock (MonitoredPages)
                {
                    MonitoredPages.Clear();
                    foreach (string value in content)
                    {
                        string[] values = value.Split('|');
                        if (values.Length == 3)
                        {
                            MonitoredPages.Add(new IWatch(getWiki(values[0]), values[1].Replace("<separator>", "|"), values[2]));
                        }
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
                        if (values == null || values.URL == null || values.Page == null || values.Channel == null)
                            continue;
                        content = content + values.URL.name + "|" + values.Page.Replace("|", "<separator>") + "|" +
                                  values.Channel + "\n";
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

        private static void Pong()
        {
            try
            {
                while (Core.IsRunning)
                {
                    Send("PING irc.wikimedia.org");
                    WD.Flush();
                    Thread.Sleep(12000);
                }
            }
            catch (IOException)
            {
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception fail)
            {
                Core.HandleException(fail, "RC");
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
                            if (iw.Page == Page && site.channel == iw.Channel)
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
                        wikiinfo.Add(new wiki(values[0], values[1], values[2]));
                    }
                }
                ModuleRC.ptrModule.Log("Loaded wiki " + content.Length);
            }
            else
            {
                ModuleRC.ptrModule.Log("There is no sites file, skipping load", true);
            }
            wikiinfo.Add(new wiki("#mediawiki.wikipedia", "https://www.mediawiki.org/w/index.php", "mediawiki"));
            wikiinfo.Add(new wiki("#test.wikipedia", "https://test.wikipedia.org/w/index.php", "test_wikipedia"));
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
                            if (iw.Channel == site.channel && iw.Page == Page)
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
                        MonitoredPages.Add(new IWatch(site, Page, site.channel));
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
