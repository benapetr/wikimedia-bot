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
using System.Web;
using System.Collections.Generic;
using System.IO;

namespace wmib.Extensions
{
    public class ModuleRC : Module
    {
        public static Module ptrModule = null;
        public override void Hook_Channel(Channel channel)
        {
            if (channel.RetrieveObject("RC") == null)
            {
                channel.RegisterObject(new RecentChanges(channel), "RC");
            }
        }

        public override bool Hook_OnRegister()
        {
            RecentChanges.Server = Configuration.RetrieveConfig("xmlrcs_host", RecentChanges.Server);
            RecentChanges.InsertSite();
            XmlRcs.Configuration.Server = RecentChanges.Server;
            lock (Configuration.Channels)
            {
                foreach (Channel channel in Configuration.Channels)
                {
                    channel.RegisterObject(new RecentChanges(channel), "RC");
                }
            }
            RegisterCommand(new GenericCommand("rc-ping", LastPing));
            return true;
        }

        public override void RegisterPermissions()
        {
            if (Security.Roles.ContainsKey("admin") && Security.Roles.ContainsKey("trusted"))
            {
                Security.Roles["trusted"].Grant("recentchanges-remove");
                Security.Roles["trusted"].Grant("recentchanges-add");
                Security.Roles["admin"].Grant("recentchanges-manage");
            }
            if (Security.Roles.ContainsKey("operator"))
            {
                Security.Roles["operator"].Grant("recentchanges-manage");
            }
        }

        public override string Extension_DumpHtml(Channel channel)
        {
            string HTML = "";
            DebugLog("Getting html for " + channel.Name);
            try
            {
                if (GetConfig(channel, "RC.Enabled", false))
                {
                    RecentChanges rc = (RecentChanges)channel.RetrieveObject("RC");
                    if (rc != null)
                    {
                        HTML = rc.ToTable();
                    }
                    else
                    {
                        Syslog.ErrorLog("NULL rc for " + channel.Name);
                    }
                }
                else
                {
                    DebugLog("RC is disabled for " + channel.Name, 2);
                }
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
            return HTML;
        }

        public override void Hook_ChannelDrop(Channel chan)
        {
            RecentChanges rc = (RecentChanges)chan.RetrieveObject("RC");
            if (rc != null && RecentChanges.recentChangesList.Contains(rc))
                RecentChanges.recentChangesList.Remove(rc);
            if (File.Exists(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + chan.Name + ".list"))
            {
                Log("Removing db of " + chan.Name + " RC feed");
                File.Delete(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + chan.Name + ".list");
            }
        }

        public override bool Hook_OnUnload()
        {
            bool ok = true;
            lock (Configuration.Channels)
            {
                foreach (Channel channel in Configuration.Channels)
                {
                    if (!channel.UnregisterObject("RC"))
                    {
                        ok = false;
                    }
                }
            }
            UnregisterCommand("rc-ping");
            RecentChanges.recentChangesList.Clear();
            return ok;
        }

        private void LastPing(CommandParams info)
        {
            IRC.DeliverMessage(RecentChanges.LastMessage.ToString(), info.SourceChannel);
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (message.StartsWith(Configuration.System.CommandPrefix + "RC-"))
            {
                if (channel.SystemUsers.IsApproved(invoker, "recentchanges-remove"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        string[] a = message.Split(' ');
                        if (a.Length < 3)
                        {
                            IRC.DeliverMessage(messages.Localize("Feed8", channel.Language, new List<string> { invoker.Nick }), channel.Name);
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
                    IRC.DeliverMessage(messages.Localize("Feed3", channel.Language), channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "recentchanges- "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "root"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        if (!message.Contains(" "))
                        {
                            if (!channel.SuppressWarnings)
                            {
                                IRC.DeliverMessage(messages.Localize("InvalidWiki", channel.Language), channel);
                            }
                            return;
                        }
                        string _channel = message.Substring(message.IndexOf(" ") + 1);
                        if (RecentChanges.DeleteChannel(channel, _channel))
                        {
                            IRC.DeliverMessage(messages.Localize("Wiki-", channel.Language), channel);
                        }
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("Feed3", channel.Language), channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "RC+ "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "recentchanges-add"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        string[] a = message.Split(' ');
                        if (a.Length < 3)
                        {
                            IRC.DeliverMessage(messages.Localize("Feed4", channel.Language) + invoker.Nick + messages.Localize("Feed5", channel.Language), channel);
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
                    IRC.DeliverMessage(messages.Localize("Feed3", channel.Language), channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "recentchanges-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, "recentchanges-manage"))
                {
                    if (!GetConfig(channel, "RC.Enabled", false))
                    {
                        IRC.DeliverMessage(messages.Localize("Feed6", channel.Language), channel);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("Feed7", channel.Language), channel);
                    SetConfig(channel, "RC.Enabled", false);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "recentchanges-on")
            {
                if (channel.SystemUsers.IsApproved(invoker, "recentchanges-manage"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        IRC.DeliverMessage(messages.Localize("Feed1", channel.Language), channel);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("Feed2", channel.Language), channel);
                    SetConfig(channel, "RC.Enabled", true);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "recentchanges+"))
            {
                if (channel.SystemUsers.IsApproved(invoker, "recentchanges-manage"))
                {
                    if (GetConfig(channel, "RC.Enabled", false))
                    {
                        if (!message.Contains(" "))
                        {
                            if (!channel.SuppressWarnings)
                            {
                                IRC.DeliverMessage(messages.Localize("InvalidWiki", channel.Language), channel);
                            }
                            return;
                        }
                        string _channel = message.Substring(message.IndexOf(" ") + 1);
                        if (RecentChanges.InsertChannel(channel, _channel))
                        {
                            IRC.DeliverMessage(messages.Localize("Wiki+", channel.Language), channel);
                        }
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("Feed3", channel.Language), channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name);
                }
            }
        }

        public override bool Construct()
        {
            ptrModule = this;
            Version = new Version(1, 2, 0, 6);
            return true;
        }

        public string Format(string name_url, string url, string page, string username, string link, string summary, Channel chan, bool bot, bool New, bool minor)
        {
            if (GetConfig(chan, "RC.Template", "") == "")
            {
                if (!New)
                {
                    return messages.Localize("fl", chan.Language, new List<string> { "12" + name_url + "", "" + page + "", "modified", "" + username + "", url + "?diff=" + link, summary });
                }
                return messages.Localize("fl", chan.Language, new List<string> { "12" + name_url + "", "" + page + "", "created", "" + username + "", url + "?title=" + HttpUtility.UrlEncode(page), summary });
            }
            string action = "modified";
            string flags = "";
            if (minor)
                flags += "minor edit, ";

            if (New)
            {
                flags += "new page, ";
                action = "created";
            }

            if (bot)
                flags += "bot edit";

            if (flags.EndsWith(", "))
                flags = flags.Substring(0, flags.Length - 2);

            string fu = url + "?diff=" + link;
            if (New)
                fu = url + "?title=" + HttpUtility.UrlEncode(page);

            return GetConfig(chan, "RC.Template", "").Replace("$wiki", name_url)
                   .Replace("$encoded_wiki_page", HttpUtility.UrlEncode(page).Replace("+", "_").Replace("%3a", ":").Replace("%2f", "/").Replace("%23", "#").Replace("%28", "(").Replace("%29", ")"))
                   .Replace("$encoded_wiki_username", HttpUtility.UrlEncode(username).Replace("+", "_").Replace("%3a", ":").Replace("%2f", "/").Replace("%23", "#").Replace("%28", "(").Replace("%29", ")"))
                   .Replace("$encoded_page", HttpUtility.UrlEncode(page))
                   .Replace("$encoded_username", HttpUtility.UrlEncode(username))
                   .Replace("$url", url)
                   .Replace("$link", link)
                   .Replace("$fullurl", fu)
                   .Replace("$username", username)
                   .Replace("$page", page)
                   .Replace("$summary", summary)
                   .Replace("$flags", flags)
                   .Replace("$action", action);
        }

        private void OnError(object sender, XmlRcs.ErrorEventArgs ex)
        {
            if (!string.IsNullOrEmpty(Configuration.System.DebugChan))
            {
                if (ex.Fatal)
                    IRC.DeliverMessage("DEBUG XmlRcs FATAL: " + ex.Message, Configuration.System.DebugChan);
                else
                    IRC.DeliverMessage("DEBUG XmlRcs ERROR: " + ex.Message, Configuration.System.DebugChan);
            }
        }

        private void OnChange(object sender, XmlRcs.EditEventArgs ex)
        {
            ex.Change.EmptyNulls();
            XmlRcs.RecentChange edit = ex.Change;
            List<RecentChanges> recentChanges = new List<RecentChanges>();
            lock (RecentChanges.recentChangesList)
            {
                recentChanges.AddRange(RecentChanges.recentChangesList);
            }
            foreach (RecentChanges curr in recentChanges)
            {
                if (GetConfig(curr.channel, "RC.Enabled", false))
                {
                    lock (curr.MonitoredPages)
                    {
                        foreach (RecentChanges.IWatch iwatch in curr.MonitoredPages)
                        {
                            if (iwatch.Site == null)
                                throw new WmibException("iwatch.Site must not be null");
                            RecentChanges.wiki wiki_ = iwatch.Site;
                            if (iwatch.Site.channel == null)
                                throw new WmibException("iwatch.Site.channel must not be null");
                            if (iwatch.Site.channel == edit.ServerName || iwatch.Site.channel == "all")
                            {
                                if (iwatch.Site.channel == "all")
                                    wiki_ = RecentChanges.WikiFromChannelID(edit.ServerName);
                                if (edit.Title == iwatch.Page)
                                {
                                    if (edit.LengthNew != 0 || edit.LengthOld != 0)
                                    {
                                        int size = edit.LengthNew - edit.LengthOld;
                                        string sx = size.ToString();
                                        if (size >= 0)
                                            sx = "+" + sx;
                                        edit.Summary = "[" + sx + "] " + edit.Summary;
                                    }
                                    if (iwatch.Site == null)
                                        DebugLog("NULL pointer on idata 1", 2);
                                    IRC.DeliverMessage(Format(wiki_.name, wiki_.url, edit.Title, edit.User, edit.RevID.ToString(), edit.Summary,
                                        curr.channel, edit.Bot, edit.Type == XmlRcs.RecentChange.ChangeType.New, edit.Minor), curr.channel.Name, libirc.Defs.Priority.Low);
                                }
                                else if (iwatch.Page.EndsWith("*") && edit.Title.StartsWith(iwatch.Page.Replace("*", "")))
                                {
                                    if (iwatch.Site == null)
                                    {
                                        DebugLog("NULL pointer on idata 2", 2);
                                    }
                                    IRC.DeliverMessage(Format(wiki_.name, wiki_.url, edit.Title, edit.User, edit.RevID.ToString(), edit.Summary, curr.channel, edit.Bot,
                                            edit.Type == XmlRcs.RecentChange.ChangeType.New, edit.Minor), curr.channel.Name, libirc.Defs.Priority.Low);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Load()
        {
            try
            {
                RecentChanges.channels = new List<string>();
                if (!File.Exists(RecentChanges.WikiFile))
                {
                    File.WriteAllText(RecentChanges.WikiFile, "mediawiki.org");
                }
                string[] list = File.ReadAllLines(RecentChanges.WikiFile);
                Log("Loading feed");
                lock (RecentChanges.channels)
                {
                    foreach (string chan in list)
                    {
                        RecentChanges.channels.Add(chan);
                    }
                }
                RecentChanges.Connect();
                RecentChanges.Provider.On_Error += OnError;
                RecentChanges.Provider.On_Change += OnChange;
                while (Core.IsRunning)
                {
                    System.Threading.Thread.Sleep(200);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        public override bool Hook_GetConfig(Channel chan, libirc.UserInfo invoker, string config)
        {
            switch (config)
            {
                case "recent-changes-template":
                    IRC.DeliverMessage("Value of " + config + " is: " + GetConfig(chan, "RC.Template", "<default value>"), chan);
                    return true;
            }
            return false;
        }

        public override bool Hook_SetConfig(Channel chan, libirc.UserInfo invoker, string config, string value)
        {
            switch (config)
            {
                case "recent-changes-template":
                    if (value != "null")
                    {
                        SetConfig(chan, "RC.Template", value);
                        IRC.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string> { value, config }), chan);
                        chan.SaveConfig();
                        return true;
                    }
                    SetConfig(chan, "RC.Template", "");
                    IRC.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string> { "null", config }), chan);
                    chan.SaveConfig();
                    return true;
            }
            return false;
        }
    }
}
