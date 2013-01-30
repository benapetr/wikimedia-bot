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
        public override void Hook_Channel(config.channel channel)
        {
            if (channel.RetrieveObject("RC") == null)
            {
                channel.RegisterObject(new RecentChanges(channel), "RC");
            }
        }

        public override bool Hook_OnRegister()
        {
            RecentChanges.InsertSite();
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

            if (GetConfig(channel, "RC.Enabled", false))
            {
                RecentChanges rc = (RecentChanges)channel.RetrieveObject("RC");
                if (rc != null)
                {
                    HTML = rc.ToTable();
                }
            }
            return HTML;
        }

        public override void Hook_ChannelDrop(config.channel chan)
        {
            if (File.Exists(variables.config + Path.DirectorySeparatorChar + chan.Name + ".list"))
            {
                core.Log("RC: Removing db of " + chan.Name + " RC feed");
                File.Delete(variables.config + Path.DirectorySeparatorChar + chan.Name + ".list");
            }
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
                        RecentChanges rc = (RecentChanges)channel.RetrieveObject("RC");
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
                        RecentChanges rc = (RecentChanges)channel.RetrieveObject("RC");
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
            Name = "RC";
            start = true;
            Version = "1.1.0.60";
            return true;
        }

        public override void Load()
        {
            try
            {
                RecentChanges.channels = new List<string>();
                if (!File.Exists(RecentChanges.channeldata))
                {
                    File.WriteAllText(RecentChanges.channeldata, "#mediawiki.wikipedia");
                }
                string message = "";
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
                    RecentChanges.Connect();
                    core.Log("Loaded feed");
                    while (true)
                    {
                        try
                        {
                            if (RecentChanges.RD == null)
                            {
                                return;
                            }
                            while (!RecentChanges.RD.EndOfStream)
                            {
                                message = RecentChanges.RD.ReadLine();
                                Match Edit = RecentChanges.line.Match(message);
                                if (RecentChanges.line.IsMatch(message) && Edit != null)
                                {
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
                                                                            if (w.URL == null)
                                                                            {
                                                                                core.Log("NULL pointer on idata 1", true);
                                                                            }
                                                                            core.irc._SlowQueue.DeliverMessage(
                                                                                messages.get("fl", curr.channel.Language, new List<string> { "12" + w.URL.name + "", "" + page + "", "" + username + "", w.URL.url + "?diff=" + link, summary }), curr.channel.Name, IRC.priority.low);
                                                                        }
                                                                        else
                                                                            if (w.Page.EndsWith("*"))
                                                                            {
                                                                                if (page.StartsWith(w.Page.Replace("*", "")))
                                                                                {
                                                                                    if (w.URL == null)
                                                                                    {
                                                                                        core.Log("NULL pointer on idata 2", true);
                                                                                    }
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
                            //core.Log("Exception while doing " + laststep, true);
                            core.LastText = message;
                            core.handleException(x);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception x)
                {
                    //core.Log("Exception while doing " + laststep, true);
                    core.handleException(x);
                    // abort
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }
    }
}
