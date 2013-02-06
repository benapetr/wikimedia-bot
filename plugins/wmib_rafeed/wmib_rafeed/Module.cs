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
    public class RegularModule : Module
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
                    if (!GetConfig(channel, "Rss.Enable", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss1", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        SetConfig(channel, "Rss.Enable", false);
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
                    if (GetConfig(channel, "Rss.Enable", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss3", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("Rss4", channel.Language), channel.Name);
                        SetConfig(channel, "Rss.Enable", true);
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

        public override string Extension_DumpHtml(config.channel channel)
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
                HTML += "<h4>Rss feed</h4><br>";
                HTML += "\n<br>\n<h4>Rss</h4>\n<br>\n\n<table class=\"infobot\" width=100% border=1>";
                HTML += "<tr><th>Name</th><th>URL</th><th>Text</th><th>Enabled</th></tr>";
                lock (list.Content)
                {
                    foreach (Feed.item feed in list.Content)
                    {
                        HTML += "\n<tr><td>" + feed.name + "</td><td><a href=\"" + feed.URL + "\">" + feed.URL + "</a></td><td>" + feed.message + "</td><td>" + (!feed.disabled).ToString() + "</td></tr>";
                    }
                }
                HTML += "</table>\n";
            }
            return HTML;
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
                    SetConfig(chan, "Rss.Style", value);
                    chan.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                    return true;
                }
                core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                return true;
            }
            return false;
        }

        public override void Hook_Channel(config.channel channel)
        {
            if (channel.RetrieveObject("rss") == null)
            {
                channel.RegisterObject(new Feed(channel), "rss");
            }
        }

        public override bool Hook_OnRegister()
        {
            bool done = true;
            foreach (config.channel chan in config.channels)
            {
                if (!chan.RegisterObject(new Feed(chan), "rss"))
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
                if (!chan.UnregisterObject("rss"))
                {
                    done = false;
                }
            }
            core.Help.Unregister("rss-on");
            core.Help.Unregister("rss-off");
            core.Help.Unregister("rss+");
            core.Help.Unregister("rss-");
            return done;
        }

        public override bool Construct()
        {
            start = true;
            Name = "Feed";
            Version = "1.0.12.20";
            return true;
        }

        public override void Load()
        {
            try
            {
                core.Help.Register("rss-on", null);
                core.Help.Register("rss-off", null);
                core.Help.Register("rss+", null);
                core.Help.Register("rss-", null);
                while (true)
                {
                    List<config.channel> chan = new List<config.channel>();
                    lock (config.channels)
                    {
                        chan.AddRange(config.channels);
                    }
                    foreach (config.channel channel in chan)
                    {
                        if (GetConfig(channel, "Rss.Enable", false))
                        {
                            Feed feed = (Feed)channel.RetrieveObject("rss");
                            if (feed != null)
                            {
                                feed.Recreate();
                            }
                            else
                            {
                                core.Log("WARNING: Feed is enabled but object is not present in " + channel.Name, true);
                            }
                        }
                    }
                    chan.Clear();
                    System.Threading.Thread.Sleep(10000);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                core.handleException(fail);
                core.Log("RC feed is permanently down", true);
            }
        }
    }
}
