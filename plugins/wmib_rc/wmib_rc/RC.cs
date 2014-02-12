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
using System.IO;
using System.Text.RegularExpressions;

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

        public static wiki all = new wiki("all", "all", "all");

        /// <summary>
        /// List of pages
        /// </summary>
        public List<IWatch> pages = new List<IWatch>();

        /// <summary>
        /// Wiki
        /// </summary>
        public static List<wiki> wikiinfo = new List<wiki>();

        /// <summary>
        /// Nickname in feed
        /// </summary>
        public static string nick;

        public static bool Loaded = false;

        /// <summary>
        /// Channels
        /// </summary>
        public static List<string> channels = new List<string>();

        public bool changed;

        public bool writable = true;

        public static bool terminated = false;

        /// <summary>
        /// feed
        /// </summary>
        public static List<RecentChanges> rc = new List<RecentChanges>();

        /// <summary>
        /// Stream reader
        /// </summary>
        public static StreamReader RD;

        public static string channeldata = Variables.ConfigurationDirectory + "/feed";
        public static StreamWriter WD;
        public static System.Net.Sockets.NetworkStream stream;

        public static Regex line =
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
                lock (pages)
                {
                    writable = false;
                    foreach (IWatch b in pages)
                    {
                        output = output + "<tr><td>" + b.Channel + "</td><td>" + System.Web.HttpUtility.HtmlEncode(b.Page) + "</td></tr>\n";
                    }
                    output = output + "</table>";
                    writable = true;
                }
                return output;
            }
            catch (Exception fail)
            {
                Core.HandleException(fail, "RC");
                writable = true;
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
            changed = false;
            Load();
            lock (rc)
            {
                rc.Add(this);
            }
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
                    Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed13", target.Language), target.Name);
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
                    Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed1", target.Language), target.Name);
                    return false;
                }
                if (channels.Contains(web.channel))
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed2", target.Language), target.Name);
                    return false;
                }
                channels.Add(web.channel);
                WD.WriteLine("JOIN " + web.channel);
                WD.Flush();
                File.WriteAllText(channeldata, "");
                foreach (string x in channels)
                {
                    File.AppendAllText(channeldata, x + "\n");
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
                Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed13", target.Language), target.Name);
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
                    Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed1", target.Language), target.Name);
                    return false;
                }
                if (!channels.Contains(W.channel))
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed3", target.Language), target.Name);
                    return false;
                }
                channels.Remove(W.channel);
                Send("PART " + W.channel);
                WD.Flush();
                File.WriteAllText(channeldata, "");
                foreach (string x in channels)
                {
                    File.AppendAllText(channeldata, x + "\n");
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
				nick = "wm-bot" + System.DateTime.Now.ToString().Replace("/", "").Replace(":", "").Replace("\\", "").Replace(".", "").Replace(" ", "") + random_number.ToString();
				Syslog.Log("Connecting to wikimedia recent changes feed as " + nick + ", hold on");
				stream = new System.Net.Sockets.TcpClient("irc.wikimedia.org", 6667).GetStream();
				WD = new StreamWriter(stream);
				RD = new StreamReader(stream, System.Text.Encoding.UTF8);
				Thread pinger = new Thread(Pong);
				Send("USER " + "wm-bot" + " 8 * :" + "wm-bot");
				Send("NICK " + nick);
				WD.Flush();
				pinger.Start();
				foreach (string b in channels)
				{
					System.Threading.Thread.Sleep(800);
					Send("JOIN " + b);
					WD.Flush();
				}
				Syslog.Log("Connected to feed - OK");
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
            Syslog.Log("There is no wiki " + Name + " known by me");
            return null;
        }

        /// <summary>
        /// Load the list
        /// </summary>
        public void Load()
        {
            string name = Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".list";
            writable = false;
            Core.RecoverFile(name, channel.Name);
            if (File.Exists(name))
            {
                string[] content = File.ReadAllLines(name);
                lock (pages)
                {
                    pages.Clear();
                    foreach (string value in content)
                    {
                        string[] values = value.Split('|');
                        if (values.Length == 3)
                        {
                            pages.Add(new IWatch(getWiki(values[0]), values[1].Replace("<separator>", "|"), values[2]));
                        }
                    }
                }
            }
            writable = true;
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
                lock (pages)
                {
                    foreach (IWatch values in pages)
                    {
                        content = content + values.URL.name + "|" + values.Page.Replace("|", "<separator>") + "|" +
                                  values.Channel + "\n";
                    }
                }
                File.WriteAllText(dbn, content);
                File.Delete(Configuration.TempName(dbn));
            }
            catch (Exception er)
            {
                Syslog.Log("Error while saving to: " + channel.Name + ".list");
                Core.HandleException(er, "RC");
                Core.RecoverFile(dbn, channel.Name);
            }
        }

        private static void Pong()
        {
            try
            {
                while (true)
                {
                    Send("PING irc.wikimedia.org");
                    WD.Flush();
                    Thread.Sleep(12000);
                }
            }
            catch (IOException)
            {
                Thread.CurrentThread.Abort();
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                Core.HandleException(fail, "RC");
            }
        }

        public bool removeString(string WS, string Page)
        {
            Page = Page.Replace("_", " ");
            wiki site = null;
            site = getWiki(WS);
            if (site != null)
            {
                if (WS == "all" || channels.Contains(site.channel))
                {
                    IWatch currpage = null;
                    lock (pages)
                    {
                        foreach (IWatch iw in pages)
                        {
                            if (iw.Page == Page && site.channel == iw.Channel)
                            {
                                currpage = iw;
                                break;
                            }
                        }
                    }
                    if (pages.Contains(currpage))
                    {
                        while (!writable)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        pages.Remove(currpage);
                        Module.SetConfig(channel, "HTML.Update", true);
                        Save();
                        Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed4", channel.Language), channel.Name);
                        return true;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed5", channel.Language), channel.Name);
                    return true;
                }
                Core.irc.Queue.DeliverMessage(
                    messages.Localize("rcfeed6", channel.Language), channel.Name);
                return false;
            }
            Core.irc.Queue.DeliverMessage(
                messages.Localize("rcfeed7", channel.Language),
                channel.Name);
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
                Syslog.Log("Loaded wiki " + content.Length.ToString());
            }
            else
            {
                Syslog.Log("There is no sites file, skipping load", true);
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
                    lock (pages)
                    {
                        foreach (IWatch iw in pages)
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
                            Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed8", channel.Language), channel.Name);
                            return true;
                        }
                    }
                    if (pages.Contains(currpage))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed9", channel.Language),
                                                     channel.Name);
                        return true;
                    }
                    lock (pages)
                    {
                        pages.Add(new IWatch(site, Page, site.channel));
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed10", channel.Language), channel.Name);
                    Module.SetConfig(channel, "HTML.Update", true);
                    Save();
                    return true;
                }
                Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed11", channel.Language), channel.Name);
                return false;
            }
            Core.irc.Queue.DeliverMessage(messages.Localize("rcfeed12", channel.Language), channel.Name);
            return false;
        }
    }
}
