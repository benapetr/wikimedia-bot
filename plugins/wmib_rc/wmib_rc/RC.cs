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
    public class RegularModule : Module
    {
        public override bool Hook_OnRegister()
        {
            lock (config.channels)
            {
                foreach (config.channel channel in config.channels)
                {
                    channel.RegisterObject(new RecentChanges(channel), "RC");
                }
            }
            return true;
        }

        public override string Extension_DumpHtml(config.channel channel)
        {
            string HTML = "";
            RecentChanges rc = (RecentChanges)channel.RetrieveObject("RC");
            if (rc != null)
            {
                HTML = rc.ToTable();
            }
            return HTML;
        }

        public override void Hook_ChannelDrop(config.channel chan)
        {
            
        }
        
        public override bool Hook_OnUnload()
        {
            bool ok = true;
            lock (config.channels)
            {
                foreach (config.channel channel in config.channels)
                {
                    if (!channel.UnregisterObject("RC"))
                    {
                        ok = false;
                    }
                }
            }
            return ok;
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (message.StartsWith("@RC-"))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "trust"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        string[] a = message.Split(' ');
                        if (a.Length < 3)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Feed8", channel.Language, new List<string> { invoker.Nick }), channel.Name);
                            return;
                        }
                        string wiki = a[1];
                        string Page = a[2];
                        RecentChanges rc = (RecentChanges)channel.RetrieveObject("RecentChanges");
                        if (rc != null)
                        {
                            rc.removeString(wiki, Page);
                        }
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Feed3", channel.Language), channel.Name);
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith("@recentchanges- "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "root"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        if (!message.Contains(" "))
                        {
                            if (!channel.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("InvalidWiki", channel.Language), channel.Name);
                            }
                            return;
                        }
                        string _channel = message.Substring(message.IndexOf(" ") + 1);
                        if (RecentChanges.DeleteChannel(channel, _channel))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Wiki-", channel.Language), channel.Name, IRC.priority.high);
                        }
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Feed3", channel.Language), channel.Name);
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith("@RC+ "))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "trust"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        string[] a = message.Split(' ');
                        if (a.Length < 3)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("Feed4", channel.Language) + invoker.Nick + messages.get("Feed5", channel.Language), channel.Name);
                            return;
                        }
                        string wiki = a[1];
                        string Page = a[2];
                        RecentChanges rc = (RecentChanges)channel.RetrieveObject("RecentChanges");
                        if (rc != null)
                        {
                            rc.insertString(wiki, Page);
                        }
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Feed3", channel.Language), channel.Name);
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@recentchanges-off")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!GetConfig(channel, "RC.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Feed6", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Feed7", channel.Language), channel.Name);
                        SetConfig(channel, "RC.Enabled", false);
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

            if (message == "@recentchanges-on")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "recentchanges-manage"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Feed1", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Feed2", channel.Language), channel.Name);
                        SetConfig(channel, "RC.Enabled", true);
                        channel.SaveConfig();
                        config.Save();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith("@recentchanges+"))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "recentchanges-manage"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        if (!message.Contains(" "))
                        {
                            if (!channel.suppress_warnings)
                            {
                                core.irc._SlowQueue.DeliverMessage(messages.get("InvalidWiki", channel.Language), channel.Name);
                            }
                            return;
                        }
                        string _channel = message.Substring(message.IndexOf(" ") + 1);
                        if (RecentChanges.InsertChannel(channel, _channel))
                        {
                            core.irc.Message(messages.get("Wiki+", channel.Language), channel.Name);
                        }
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Feed3", channel.Language), channel.Name);
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
        public override bool Construct()
        {
            base.Create("RC", true);
            Version = "1.0.8.1";
            return true;
        }

        public override void Load()
        {
            RecentChanges.channels = new List<string>();
            if (!File.Exists(RecentChanges.channeldata))
            {
                File.WriteAllText(RecentChanges.channeldata, "#mediawiki.wikipedia");
            }
            string message = "";
            string laststep = "Open";
            try
            {
                string[] list = System.IO.File.ReadAllLines(RecentChanges.channeldata);
                core.Log("Loading feed");
                lock (RecentChanges.channels)
                {
                    foreach (string chan in list)
                    {
                        RecentChanges.channels.Add(chan);
                    }
                }
                laststep = "Connect";
                RecentChanges.Connect();
                core.Log("Loaded feed");
                laststep = "Parse";
                while (true)
                {
                    try
                    {
                        if (RecentChanges.RD == null)
                        {
                            laststep = "RD was null";
                            return;
                        }
                        laststep = "fetching stream";
                        while (!RecentChanges.RD.EndOfStream)
                        {
                            message = RecentChanges.RD.ReadLine();
                            laststep = "reading line";
                            Match Edit = RecentChanges.line.Match(message);
                            if (RecentChanges.line.IsMatch(message) && Edit != null)
                            {
                                laststep = "parsing line";
                                string _channel = message.Substring(message.IndexOf("PRIVMSG"));
                                _channel = _channel.Substring(_channel.IndexOf("#"));
                                _channel = _channel.Substring(0, _channel.IndexOf(" "));
                                if (Edit.Groups.Count > 7)
                                {
                                    string page = Edit.Groups[1].Value;
                                    string link = Edit.Groups[4].Value;
                                    string username = Edit.Groups[6].Value;
                                    string change = Edit.Groups[7].Value;
                                    string summary = Edit.Groups[8].Value;

                                    laststep = "reading rc";

                                    lock (RecentChanges.rc)
                                    {
                                        foreach (RecentChanges curr in RecentChanges.rc)
                                        {
                                            if (curr != null)
                                            {
                                                if (GetConfig(curr.channel, "RC.Enabled", false))
                                                {
                                                    lock (curr.pages)
                                                    {
                                                        foreach (RecentChanges.IWatch w in curr.pages)
                                                        {
                                                            if (w != null)
                                                            {
                                                                if (w.Channel == _channel)
                                                                {
                                                                    if (page == w.Page)
                                                                    {
                                                                        core.irc._SlowQueue.DeliverMessage(
                                                                            //messages.get("rfeedline1", curr.channel.Language) + "12" + w.URL.name + "" + messages.get("rfeedline2", curr.channel.Language) + "" + page +
                                                                            //"" + messages.get("rfeedline3", curr.channel.Language) + "" + username +
                                                                            //"" + messages.get("rfeedline4", curr.channel.Language) + w.URL.url + "?diff=" + link + messages.get("rfeedline5", curr.channel.Language) + summary, curr.channel.Name);
                                                                            messages.get("fl", curr.channel.Language, new List<string> { "12" + w.URL.name + "", "" + page + "", "" + username + "", w.URL.url + "?diff=" + link, summary }), curr.channel.Name, IRC.priority.low);
                                                                    }
                                                                    else
                                                                        if (w.Page.EndsWith("*"))
                                                                        {
                                                                            if (page.StartsWith(w.Page.Replace("*", "")))
                                                                            {
                                                                                core.irc._SlowQueue.DeliverMessage(
                                                                                messages.get("fl", curr.channel.Language, new List<string> { "12" + w.URL.name + "", "" + page + "", "" + username + "", w.URL.url + "?diff=" + link, summary }), curr.channel.Name, IRC.priority.low);
                                                                            }
                                                                        }
                                                                }
                                                            }
                                                        }
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
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (IOException)
                    {
                        RecentChanges.Connect();
                    }
                    catch (Exception x)
                    {
                        core.Log("Exception while doing " + laststep, true);
                        core.LastText = message;
                        core.handleException(x);
                    }
                }
            }
            catch (Exception x)
            {
                core.Log("Exception while doing " + laststep, true);
                core.handleException(x);
                // abort
            }
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

        public static string channeldata = variables.config + "/feed";
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
                lock (rc)
                {
                    rc.Remove(this);
                }
            }
            catch (Exception er)
            {
                core.handleException(er);
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
                core.handleException(f);
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
                core.handleException(f);
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
                try
                {
                    Random rand = new Random();
                    int random_number = rand.Next(10000);
                    nick = "wm-bot" + System.DateTime.Now.ToString().Replace("/", "").Replace(":", "").Replace("\\", "").Replace(".", "").Replace(" ", "") + random_number.ToString();
                    core.Log("Connecting to wikimedia recent changes feed as " + nick + ", hold on");
                    stream = new System.Net.Sockets.TcpClient("irc.wikimedia.org", 6667).GetStream();
                    WD = new StreamWriter(stream);
                    RD = new StreamReader(stream, System.Text.Encoding.UTF8);
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
                    core.Log("Connected to feed - OK");
                    Loaded = true;
                }
                catch (Exception)
                {
                    Console.WriteLine("error in Feed.Connect() call");
                }
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
            string name = variables.config + Path.DirectorySeparatorChar + channel.Name + ".list";
            writable = false;
            core.recoverFile(name, channel.Name);
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
            string dbn = variables.config + "/" + channel.Name + ".list";
            try
            {
                string content = "";
                core.backupData(dbn);
                lock (pages)
                {
                    foreach (IWatch values in pages)
                    {
                        content = content + values.URL.name + "|" + values.Page.Replace("|", "<separator>") + "|" +
                                  values.Channel + "\n";
                    }
                }
                File.WriteAllText(dbn, content);
                File.Delete(config.tempName(dbn));
            }
            catch (Exception)
            {
                core.Log("Error while saving to: " + channel.Name + ".list");
                core.recoverFile(dbn, channel.Name);
            }
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
                        core.irc._SlowQueue.DeliverMessage(messages.get("rcfeed4", channel.Language), channel.Name);
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("rcfeed5", channel.Language), channel.Name);
                    return true;
                }
                core.irc._SlowQueue.DeliverMessage(
                    messages.get("rcfeed6", channel.Language), channel.Name);
                return false;
            }
            core.irc._SlowQueue.DeliverMessage(
                messages.get("rcfeed7", channel.Language),
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
                            core.irc._SlowQueue.DeliverMessage(messages.get("rcfeed8", channel.Language), channel.Name);
                            return true;
                        }
                    }
                    if (pages.Contains(currpage))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("rcfeed9", channel.Language),
                                                     channel.Name);
                        return true;
                    }
                    lock (pages)
                    {
                        pages.Add(new IWatch(site, Page, site.channel));
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("rcfeed10", channel.Language), channel.Name);
                    Module.SetConfig(channel, "HTML.Update", true);
                    Save();
                    return true;
                }
                core.irc._SlowQueue.DeliverMessage(
                    messages.get("rcfeed11", channel.Language), channel.Name);
                return false;
            }
            core.irc._SlowQueue.DeliverMessage(
                messages.get("rcfeed12", channel.Language),
                channel.Name);
            return false;
        }
    }
}
