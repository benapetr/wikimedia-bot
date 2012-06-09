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

        /// <summary>
        /// List of pages
        /// </summary>
        private List<IWatch> pages = new List<IWatch>();

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
        private static List<string> channels = new List<string>();
        
        public bool changed;
        
        private bool writable = true;

        /// <summary>
        /// feed
        /// </summary>
        private static List<RecentChanges> rc = new List<RecentChanges>();

        /// <summary>
        /// Stream reader
        /// </summary>
        private static StreamReader RD;

        private static string channeldata = variables.config + "/feed";
        public static StreamWriter WD;
        public static System.Net.Sockets.NetworkStream stream;

        public static Regex line =
            new Regex(":rc-pmtpa!~rc-pmtpa@[^ ]* PRIVMSG #[^:]*:14\\[\\[07([^]*)14\\]\\]4 (M?)(B?)10 02.*di" +
                      "ff=([^&]*)&oldid=([^]*) 5\\* 03([^]*) 5\\* \\(?([^]*)?\\) 10([^]*)?");

        public config.channel channel;

        ~RecentChanges()
        {
            try
            {
                rc.Remove(this);
            }
            catch (Exception)
            {
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
                        output = output + "<tr><td>" + b.Channel + "</td><td>" + HtmlDump.Encode(b.Page) + "</td></tr>\n";
                    }
                    output = output + "</table>";
                    writable = true;
                }
                return output;
            }
            catch (Exception)
            {
                writable = true;
                return "";
            }
        }

        public RecentChanges(config.channel _channel)
        {
            channel = _channel;
            changed = false;
            Load();
            rc.Add(this);
        }

        /// <summary>
        /// New channel to watch by a bot
        /// </summary>
        /// <param name="target">Object to send output to</param>
        /// <param name="name">Name of wiki</param>
        /// <returns></returns>
        public static bool InsertChannel(config.channel target, string name)
        {
            try
            {
                wiki web = null;
                if (Loaded == false)
                {
                    core.irc.Message(messages.get("rcfeed13", target.Language), target.Name);
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
                    core.irc.Message(messages.get("rcfeed1", target.Language), target.Name);
                    return false;
                }
                if (channels.Contains(web.channel))
                {
                    core.irc.Message(messages.get("rcfeed2", target.Language), target.Name);
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
                Console.WriteLine(f.Message);
            }
            return true;
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="target">Object to get output back to</param>
        /// <param name="WikiName">Name of site</param>
        /// <returns></returns>
        public static bool DeleteChannel(config.channel target, string WikiName)
        {
            wiki W = null;
            if (Loaded == false)
            {
                core.irc.Message(messages.get("rcfeed13", target.Language), target.Name);
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
                    core.irc.Message(messages.get("rcfeed1", target.Language), target.Name);
                    return false;
                }
                if (!channels.Contains(W.channel))
                {
                    core.irc.Message(messages.get("rcfeed3", target.Language), target.Name);
                    return false;
                }
                channels.Remove(W.channel);
                WD.WriteLine("PART " + W.channel);
                WD.Flush();
                File.WriteAllText(channeldata, "");
                foreach (string x in channels)
                {
                    File.AppendAllText(channeldata, x + "\n");
                }
            }
            catch (Exception f)
            {
                Console.WriteLine(f.Message);
            }
            return true;
        }

        /// <summary>
        /// Connect to wm irc
        /// </summary>
        public static void Connect()
        {
            try
            {
                Program.Log("Connecting to wikimedia recent changes feed");
                stream = new System.Net.Sockets.TcpClient("irc.wikimedia.org", 6667).GetStream();
                WD = new StreamWriter(stream);
                RD = new StreamReader(stream, System.Text.Encoding.UTF8);
                nick = "wm-bot" + System.DateTime.Now.ToString().Replace("/", "").Replace(":", "").Replace("\\", "").Replace(".", "");
                Thread pinger = new Thread(Pong);
                WD.WriteLine("USER " + "wm-bot" + " 8 * :" + "wm-bot");
                WD.WriteLine("NICK " + nick);
                WD.Flush();
                pinger.Start();
                foreach (string b in channels)
                {
                    System.Threading.Thread.Sleep(800);
                    WD.WriteLine("JOIN " + b);
                    WD.Flush();
                }
                Program.Log("Connected to feed - OK");
                Loaded = true;
            }
            catch (Exception)
            {
                Console.WriteLine("error in Feed.Connect() call");
            }
        }

        /// <summary>
        /// get the wiki from a name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private static wiki getWiki(string Name)
        {
            foreach (wiki curr in wikiinfo)
            {
                if (curr.name == Name)
                {
                    return curr;
                }
            }
            return null;
        }

        /// <summary>
        /// Load the list
        /// </summary>
        public void Load()
        {
            string name = variables.config + "/" + channel.Name + ".list";
            writable = false;
            if (File.Exists(name))
            {
                string[] content = File.ReadAllLines(name);
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
            writable = true;
        }

        /// <summary>
        /// Save the list
        /// </summary>
        public void Save()
        {
            string dbn = variables.config + "/" + channel.Name + ".list";
            string content = "";
            foreach (IWatch values in pages)
            {
                content = content + values.URL.name + "|" + values.Page.Replace("|", "<separator>") + "|" +
                          values.Channel + "\n";
            }
            File.WriteAllText(dbn, content);
        }

        private static void Pong()
        {
            try
            {
                while (true)
                {
                    WD.WriteLine("PING irc.wikimedia.org");
                    WD.Flush();
                    Thread.Sleep(12000);
                }
            }
            catch (IOException)
            {
                Thread.CurrentThread.Abort();
            }
            catch (Exception)
            {
            }
        }

        public bool removeString(string WS, string Page)
        {
            Page = Page.Replace("_", " ");
            wiki site = null;
            foreach (wiki Site in wikiinfo)
            {
                if (Site.name == WS)
                {
                    site = Site;
                }
            }
            if (site != null)
            {
                if (channels.Contains(site.channel))
                {
                    IWatch currpage = null;
                    foreach (IWatch iw in pages)
                    {
                        if (iw.Page == Page)
                        {
                            currpage = iw;
                            break;
                        }
                    }
                    if (pages.Contains(currpage))
                    {
                        while (!writable)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        pages.Remove(currpage);
                        channel.Keys.update = true;
                        Save();
                        core.irc._SlowQueue.DeliverMessage(messages.get( "rcfeed4", channel.Language ), channel.Name);
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get( "rcfeed5", channel.Language ), channel.Name);
                    return true;
                }
                core.irc._SlowQueue.DeliverMessage(
                    messages.get( "rcfeed6", channel.Language ), channel.Name);
                return false;
            }
            core.irc._SlowQueue.DeliverMessage(
                messages.get( "rcfeed7", channel.Language ),
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
            }
            wikiinfo.Add(new wiki("#mediawiki.wikipedia", "https://www.mediawiki.org/w/index.php", "mediawiki"));
            wikiinfo.Add(new wiki("#test.wikipedia", "https://test.wikipedia.org/w/index.php", "test_wikipedia"));
            return 0;
        }

        public bool insertString(string WS, string Page)
        {
            wiki site = null;
            Page = Page.Replace("_", " ");
            foreach (wiki Site in wikiinfo)
            {
                if (Site.name == WS)
                {
                    site = Site;
                }
            }
            if (site != null)
            {
                if (channels.Contains(site.channel))
                {
                    IWatch currpage = null;
                    foreach (IWatch iw in pages)
                    {
                        if (iw.Page == Page)
                        {
                            currpage = iw;
                            break;
                        }
                    }
                    if (Page.Contains("*"))
                    {
                        if (!Page.EndsWith("*") || Page.Replace("*", "") == "")
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get( "rcfeed8", channel.Language ), channel.Name);
                            return true;
                        }
                    }
                    if (pages.Contains(currpage))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("rcfeed9", channel.Language),
                                                     channel.Name);
                        return true;
                    }
                    while (!writable)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                    pages.Add(new IWatch(site, Page, site.channel));
                    core.irc._SlowQueue.DeliverMessage(messages.get("rcfeed10", channel.Language), channel.Name);
                    channel.Keys.update = true;
                    Save();
                    return true;
                }
                core.irc._SlowQueue.DeliverMessage(
                    messages.get( "rcfeed11", channel.Language ), channel.Name);
                return false;
            }
            core.irc._SlowQueue.DeliverMessage(
                messages.get( "rcfeed12", channel.Language ),
                channel.Name);
            return false;
        }

        public static void Start()
        {
            channels = new List<string>();
            if (!File.Exists(channeldata))
            {
                File.WriteAllText(channeldata, "#mediawiki.wikipedia");
            }
            try
            {
                string[] list = System.IO.File.ReadAllLines(channeldata);
                Program.Log("Loading feed");
                foreach (string chan in list)
                {
                    channels.Add(chan);
                }
                Connect();
                Program.Log("Loaded feed");
                while (true)
                {
                    try
                    {
                        string message;
                        while (!RD.EndOfStream)
                        {
                            message = RD.ReadLine();
                            Match Edit = line.Match(message);
                            if (line.IsMatch(message))
                            {
                                string _channel = message.Substring(message.IndexOf("PRIVMSG"));
                                _channel = _channel.Substring(_channel.IndexOf("#"));
                                _channel = _channel.Substring(0, _channel.IndexOf(" "));
                                string username = Edit.Groups[6].Value;
                                string change = Edit.Groups[7].Value;
                                string page = Edit.Groups[1].Value;
                                string link = Edit.Groups[4].Value;
                                string summary = Edit.Groups[8].Value;

                                foreach (RecentChanges curr in rc)
                                {
                                    if (curr.channel.Feed)
                                    {
                                        foreach (IWatch w in curr.pages)
                                        {
                                            if (w.Channel == _channel)
                                            {
                                                if (page == w.Page)
                                                {
                                                    core.irc._SlowQueue.DeliverMessage(
                                                        //messages.get("rfeedline1", curr.channel.Language) + "12" + w.URL.name + "" + messages.get("rfeedline2", curr.channel.Language) + "" + page +
                                                        //"" + messages.get("rfeedline3", curr.channel.Language) + "" + username +
                                                        //"" + messages.get("rfeedline4", curr.channel.Language) + w.URL.url + "?diff=" + link + messages.get("rfeedline5", curr.channel.Language) + summary, curr.channel.Name);
                                                        messages.get("fl", curr.channel.Language, new List<string> { "12" + w.URL.name + "", "" + page + "", "" + username + "", w.URL.url + "?diff=" + link, summary }), curr.channel.Name, IRC.priority.normal);
                                                }
                                                else
                                                    if (w.Page.EndsWith("*"))
                                                    {
                                                        if (page.StartsWith(w.Page.Replace("*", "")))
                                                        {
                                                            core.irc._SlowQueue.DeliverMessage(
                                                            messages.get("fl", curr.channel.Language, new List<string> { "12" + w.URL.name + "", "" + page + "", "" + username + "", w.URL.url + "?diff=" + link, summary }), curr.channel.Name, IRC.priority.normal);
                                                        }
                                                    }
                                            }
                                        }
                                    }
                                }
                            }
                            Thread.Sleep(10);
                        }
                        Thread.Sleep(100);
                    }
                    catch (IOException)
                    {
                        Connect();
                    }
                    catch (Exception x)
                    {
                        Console.WriteLine(x.Message);
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                // abort
            }
        }
    }
}
