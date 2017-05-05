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

        public override bool Construct()
        {
            ptrModule = this;
            Version = new Version(1, 2, 0, 8);
            return true;
        }

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
            XmlRcs.Configuration.Server = RecentChanges.Server;
            lock (Configuration.Channels)
            {
                foreach (Channel channel in Configuration.Channels)
                {
                    channel.RegisterObject(new RecentChanges(channel), "RC");
                }
            }
            RegisterCommand(new GenericCommand("recentchanges-on", cmd_on, true, "recentchanges-manage"));
            RegisterCommand(new GenericCommand("recentchanges-off", cmd_off, true, "recentchanges-manage"));
            RegisterCommand(new GenericCommand("recentchanges-minor-on", minor_on, true, "recentchanges-manage"));
            RegisterCommand(new GenericCommand("recentchanges-minor-off", minor_off, true, "recentchanges-manage"));
            RegisterCommand(new GenericCommand("recentchanges-bot-on", bot_on, true, "recentchanges-manage"));
            RegisterCommand(new GenericCommand("recentchanges-bot-off", bot_off, true, "recentchanges-manage"));
            // Some aliases for easy typing
            RegisterAlias("rc-on", "recentchanges-on");
            RegisterAlias("rc-off", "recentchanges-off");
            RegisterAlias("rc-minor-on", "recentchanges-minor-on");
            RegisterAlias("rc-minor-off", "recentchanges-minor-off");
            RegisterAlias("rc-bot-on", "recentchanges-bot-on");
            RegisterAlias("rc-bot-off", "recentchanges-bot-off");
            // Maintenance commands
            RegisterCommand(new GenericCommand("rc-ping", LastPing));
            RegisterCommand(new GenericCommand("rc-restart", cmd_restart, true, "root"));
            return true;
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
            UnregisterCommand("rc-restart");
            UnregisterCommand("recentchanges-on");
            UnregisterCommand("recentchanges-off");
            UnregisterCommand("recentchanges-minor-on");
            UnregisterCommand("recentchanges-minor-off");
            UnregisterCommand("recentchanges-bot-on");
            UnregisterCommand("recentchanges-bot-off");
            // Aliases
            UnregisterAlias("rc-on");
            UnregisterAlias("rc-off");
            UnregisterAlias("rc-minor-on");
            UnregisterAlias("rc-minor-off");
            UnregisterAlias("rc-bot-on");
            UnregisterAlias("rc-bot-off");
            RecentChanges.recentChangesList.Clear();
            return ok;
        }

        private void cmd_restart(CommandParams pm)
        {
            IRC.DeliverMessage("Reconnecting to RC feed", pm.SourceChannel);
            RecentChanges.Provider.Disconnect();
            RecentChanges.Provider.Connect();
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
                            rc.RemovePage(wiki, Page);
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
                        if (wiki.Contains("_"))
                        {
                            IRC.DeliverMessage("Underscore in wiki name is not supported, new format is for example: en.wikipedia.org instead of en_wikipedia", channel);
                            return;
                        }
                        RecentChanges rc = (RecentChanges)channel.RetrieveObject("RC");
                        if (rc != null)
                        {
                            rc.MonitorPage(wiki, Page);
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
        }

        private void cmd_on(CommandParams pm)
        {
            if (GetConfig(pm.SourceChannel, "RC.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("Feed1", pm.SourceChannel.Language), pm.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("Feed2", pm.SourceChannel.Language), pm.SourceChannel);
            SetConfig(pm.SourceChannel, "RC.Enabled", true);
            pm.SourceChannel.SaveConfig();
        }

        private void cmd_off(CommandParams pm)
        {
            if (!GetConfig(pm.SourceChannel, "RC.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("Feed6", pm.SourceChannel.Language), pm.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("Feed7", pm.SourceChannel.Language), pm.SourceChannel);
            SetConfig(pm.SourceChannel, "RC.Enabled", false);
            pm.SourceChannel.SaveConfig();
        }

        private void minor_on(CommandParams pm)
        {
            if (GetConfig(pm.SourceChannel, "Minor.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("Feed13", pm.SourceChannel.Language), pm.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("Feed9", pm.SourceChannel.Language), pm.SourceChannel);
            SetConfig(pm.SourceChannel, "Minor.Enabled", true);
            pm.SourceChannel.SaveConfig();
        }

        private void minor_off(CommandParams pm)
        {
            if (!GetConfig(pm.SourceChannel, "Minor.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("Feed14", pm.SourceChannel.Language), pm.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("Feed10", pm.SourceChannel.Language), pm.SourceChannel);
            SetConfig(pm.SourceChannel, "Minor.Enabled", false);
            pm.SourceChannel.SaveConfig();
        }

        private void bot_on(CommandParams pm)
        {
            if (GetConfig(pm.SourceChannel, "Bot.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("Feed15", pm.SourceChannel.Language), pm.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("Feed11", pm.SourceChannel.Language), pm.SourceChannel);
            SetConfig(pm.SourceChannel, "Bot.Enabled", true);
            pm.SourceChannel.SaveConfig();
        }

        private void bot_off(CommandParams pm)
        {
            if (!GetConfig(pm.SourceChannel, "Bot.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("Feed16", pm.SourceChannel.Language), pm.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("Feed12", pm.SourceChannel.Language), pm.SourceChannel);
            SetConfig(pm.SourceChannel, "Bot.Enabled", false);
            pm.SourceChannel.SaveConfig();
        }

        public string Format(string name_url, string url, string page, string username, string link, string summary, Channel chan, bool bot, bool New, bool minor)
        {
            // this is a hack that adds /wiki or /w to full url, it does work only for wikis that use recommended settings
            // should there ever be a need to support some hand made url's we would need to make this user configureable
            string full_url = "http://" + url + "/w/";
            if (GetConfig(chan, "RC.Template", "") == "")
            {
                if (!New)
                {
                    return messages.Localize("fl", chan.Language, new List<string> { "12" + name_url + "", "" + page + "", "modified", "" + username + "", "https://" + url + "/w/index.php?diff=" + link, summary });
                }
                return messages.Localize("fl", chan.Language, new List<string> { "12" + name_url + "", "" + page + "", "created", "" + username + "", "https://" + url + "/wiki/" + Core.WikiEncode(page), summary });
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

            string fu = full_url + "/?diff=" + link;
            if (New)
                fu = full_url + "/?title=" + HttpUtility.UrlEncode(page);

            return GetConfig(chan, "RC.Template", "").Replace("$wiki", name_url)
                   .Replace("$encoded_wiki_page", Core.WikiEncode(page))
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
            ErrorLog(ex.Message);
        }

        private void OnChange(object sender, XmlRcs.EditEventArgs ex)
        {
            ex.Change.EmptyNulls();
            XmlRcs.RecentChange edit = ex.Change;
            if (edit.Type != XmlRcs.RecentChange.ChangeType.Edit && edit.Type != XmlRcs.RecentChange.ChangeType.New)
                return;
            if (edit.Type == XmlRcs.RecentChange.ChangeType.Edit && edit.RevID < 1)
            {
                Syslog.ErrorLog("RC: Invalid revid: " + edit.OriginalXml);
                return;
            }
            List<RecentChanges> recentChanges = new List<RecentChanges>();
            RecentChanges.LastMessage = DateTime.Now;
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
                            if (iwatch.Site == edit.ServerName || iwatch.Site == "*")
                            {
                                if ((!edit.Minor && edit.Bot && GetConfig(curr.channel, "Bot.Enabled", true)) || (!edit.Bot && edit.Minor && GetConfig(curr.channel, "Minor.Enabled", true)) || (!edit.Bot && !edit.Minor) || (edit.Minor && GetConfig(curr.channel, "Minor.Enabled", true) && edit.Bot && GetConfig(curr.channel, "Bot.Enabled", true)))
                                {
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
                                        IRC.DeliverMessage(Format(edit.ServerName, edit.ServerName, edit.Title, edit.User, edit.RevID.ToString(), edit.Summary,
                                            curr.channel, edit.Bot, edit.Type == XmlRcs.RecentChange.ChangeType.New, edit.Minor), curr.channel.Name, libirc.Defs.Priority.Low);
                                    }
                                    else if (iwatch.Page.EndsWith("*") && edit.Title.StartsWith(iwatch.Page.Replace("*", "")))
                                    {
                                        if (iwatch.Site == null)
                                        {
                                            DebugLog("NULL pointer on idata 2", 2);
                                        }
                                        IRC.DeliverMessage(Format(edit.ServerName, edit.ServerName, edit.Title, edit.User, edit.RevID.ToString(), edit.Summary, curr.channel, edit.Bot,
                                                edit.Type == XmlRcs.RecentChange.ChangeType.New, edit.Minor), curr.channel.Name, libirc.Defs.Priority.Low);
                                    }
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
                Log("Loading feed");
                RecentChanges.Connect();
                RecentChanges.Provider.On_Error += OnError;
                RecentChanges.Provider.On_Change += OnChange;
                RecentChanges.Provider.On_Timeout += Provider_On_Timeout;
                RecentChanges.Provider.On_Exception += Provider_On_Exception;
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

        void Provider_On_Exception(object sender, XmlRcs.ExEventArgs args)
        {
            HandleException(args.Exception);
        }

        void Provider_On_Timeout(object sender, EventArgs args)
        {
            ErrorLog("timed out");
        }

        public void ExceptionHandler(object sender, XmlRcs.ExEventArgs args)
        {
            ErrorLog(args.Exception.ToString());
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
