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
using System.Threading;

namespace wmib.Extensions.RssFeed
{
    public class RSS : Module
    {
        public static Module m = null;

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (message.StartsWith(Configuration.System.CommandPrefix + "rss- "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "trust"))
                {
                    string item = message.Substring("@rss+ ".Length);
                    Feed feed = (Feed)channel.RetrieveObject("rss");
                    if (feed != null)
                    {
                        feed.RemoveItem(item);
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel.Name);
                }
            }
            if (message.StartsWith(Configuration.System.CommandPrefix + "rss-setstyle "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "trust"))
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
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(Localization.Localize("Rss5", channel.Language), channel.Name);
                    }
                }
                else
                {
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel.Name);
                    }
                }
            }
            if (message.StartsWith(Configuration.System.CommandPrefix + "rss-search+ "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "trust"))
                {
                    string item = message.Substring("@rss-search+ ".Length);
                    Feed feed = (Feed)channel.RetrieveObject("rss");
                    if (feed != null)
                    {
                        lock (feed.ScannerMatches)
                        {
                            if (feed.ScannerMatches.Contains(item))
                            {
                                IRC.DeliverMessage("This item is already being searched", channel);
                                return;
                            }
                            IRC.DeliverMessage("This item is now searched", channel);
                            feed.ScannerMatches.Add(item);
                            return;
                        }
                    }
                    IRC.DeliverMessage("Error, this channel doesn't have RC feed", channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
            }
            if (message.StartsWith(Configuration.System.CommandPrefix + "rss-search- "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "trust"))
                {
                    string item = message.Substring("@rss-search+ ".Length);
                    Feed feed = (Feed)channel.RetrieveObject("rss");
                    if (feed != null)
                    {
                        lock (feed.ScannerMatches)
                        {
                            if (feed.ScannerMatches.Contains(item))
                            {
                                feed.ScannerMatches.Remove(item);
                                IRC.DeliverMessage("This item was removed", channel);
                                return;
                            }
                            IRC.DeliverMessage("This item was not being searched", channel);
                            return;
                        }
                    }
                    IRC.DeliverMessage("Error, this channel doesn't have RC feed", channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel);
                }
            }
            if (message.StartsWith(Configuration.System.CommandPrefix + "rss+ "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "trust"))
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
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(Localization.Localize("Rss5", channel.Language), channel.Name);
                    }
                }
                else
                {
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel.Name);
                    }
                }
            }
            if (message.StartsWith("@rss-scanner+ "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "trust"))
                {
                    string item = message.Substring("@rss-scanner+ ".Length);
                    if (item.Contains(" "))
                    {
                        string id = item.Substring(0, item.IndexOf(" "));
                        string ur = item.Substring(item.IndexOf(" ") + 1);
                        Feed feed = (Feed)channel.RetrieveObject("rss");
                        if (feed != null)
                        {
                            feed.InsertItem(id, ur, true);
                        }
                        return;
                    }
                    if (item != "")
                    {
                        Feed feed = (Feed)channel.RetrieveObject("rss");
                        if (feed != null)
                        {
                            feed.InsertItem(item, "", true);
                        }
                        return;
                    }
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(Localization.Localize("Rss5", channel.Language), channel.Name);
                    }
                }
                else
                {
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel.Name);
                    }
                }
            }
            if (message.StartsWith("@rss-scanner- "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "trust"))
                {
                    string item = message.Substring("@rss-scannerx ".Length);
                    Feed feed = (Feed)channel.RetrieveObject("rss");
                    if (feed != null)
                    {
                        feed.RemoveItem(item);
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel.Name);
                }
            }
            if (message == "@rss-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "Rss.Enable", false))
                    {
                        IRC.DeliverMessage(Localization.Localize("Rss1", channel.Language), channel.Name);
                        return;
                    }
                    SetConfig(channel, "Rss.Enable", false);
                    IRC.DeliverMessage(Localization.Localize("Rss2", channel.Language), channel.Name);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel.Name);
                }
                return;
            }
            if (message == "@rss-on")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "Rss.Enable", false))
                    {
                        IRC.DeliverMessage(Localization.Localize("Rss3", channel.Language), channel.Name);
                        return;
                    }
                    IRC.DeliverMessage(Localization.Localize("Rss4", channel.Language), channel.Name);
                    SetConfig(channel, "Rss.Enable", true);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(Localization.Localize("PermissionDenied", channel.Language), channel.Name);
                }
            }
        }

        public override string Extension_DumpHtml(Channel channel)
        {
            string HTML = "";
            if (GetConfig(channel, "Rss.Enable", false))
            {
                Feed list = (Feed)channel.RetrieveObject("rss");
                if (list == null)
                {
                    return "";
                }
                if (GetConfig(channel, "Rss.Enable", false) != true)
                {
                    return HTML;
                }
                HTML += "<h4>Rss feed</h4><br />";
                HTML += "\n<br />\n<h4>Rss</h4>\n<br />\n\n<table class=\"infobot\" width=100% border=1>";
                HTML += "<tr><th>Name</th><th>URL</th><th>Text</th><th>Enabled</th></tr>";
                lock (list.RssProviders)
                {
                    foreach (Feed.Subscription feed in list.RssProviders)
                    {
                        HTML += "\n<tr><td>" + feed.Name + "</td><td><a href=\"" + feed.URL + "\">" + feed.URL + "</a></td><td>" + feed.template + "</td><td>" + (!feed.disabled) + "</td></tr>";
                    }
                }
                HTML += "</table>\n";
            }
            return HTML;
        }

        public override void Hook_BeforeSysWeb(ref string html)
        {
            html += "\n<br /><br />Rss feeds: " + Feed.Subscription.Count;
        }

        public override bool Hook_SetConfig(Channel chan, libirc.UserInfo invoker, string config, string value)
        {
            if (config == "style-rss")
            {
                if (value != "")
                {
                    SetConfig(chan, "Rss.Style", value);
                    chan.SaveConfig();
                    IRC.DeliverMessage(Localization.Localize("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                    return true;
                }
                IRC.DeliverMessage(Localization.Localize("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                return true;
            }
            return false;
        }

        public override void Hook_Channel(Channel channel)
        {
            if (channel.RetrieveObject("rss") == null)
            {
                channel.RegisterObject(new Feed(channel), "rss");
            }
        }

        public override bool Hook_OnRegister()
        {
            bool done = true;
            lock (Configuration.Channels)
            {
                foreach (Channel chan in Configuration.Channels)
                {
                    if (!chan.RegisterObject(new Feed(chan), "rss"))
                    {
                        done = false;
                    }
                }
            }

            return done;
        }

        public override bool Hook_OnUnload()
        {
            bool done = true;
            lock (Configuration.Channels)
            {
                foreach (Channel chan in Configuration.Channels)
                {
                    if (!chan.UnregisterObject("rss"))
                    {
                        done = false;
                    }
                }
            }
            Core.Help.Unregister("rss-on");
            Core.Help.Unregister("rss-off");
            Core.Help.Unregister("rss+");
            Core.Help.Unregister("rss-");
            return done;
        }

        public override bool Construct()
        {
            m = this;
            Version = new Version(1, 0, 20, 0);
            return true;
        }

        public override void Load()
        {
            try
            {
                Core.Help.Register("rss-on", null);
                Core.Help.Register("rss-off", null);
                Core.Help.Register("rss+", null);
                Core.Help.Register("rss-", null);
                while (IsWorking)
                {
                    foreach (Channel channel in Configuration.ChannelList)
                    {
                        try
                        {
                            if (GetConfig(channel, "Rss.Enable", false))
                            {
                                Feed feed = (Feed)channel.RetrieveObject("rss");
                                if (feed != null)
                                {
                                    feed.Fetch();
                                }
                                else
                                {
                                    Log("WARNING: Feed is enabled but object is not present in " + channel.Name, true);
                                }
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            return;
                        }
                        catch (Exception fail)
                        {
                            HandleException(fail);
                        }
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                HandleException(fail);
                Log("Rss feed is permanently down", true);
            }
        }
    }
}
