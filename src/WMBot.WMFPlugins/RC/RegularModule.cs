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
using System.Threading;
using System.Xml;
using System.Web;

namespace wmib
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
            RecentChanges.InsertSite();
            lock (Configuration.Channels)
            {
                foreach (Channel channel in Configuration.Channels)
                {
                    channel.RegisterObject(new RecentChanges(channel), "RC");
                }
            }
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
            RecentChanges.recentChangesList.Clear();
            return ok;
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

        public static Change String2Change(string text)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(text);
            string name = xml.DocumentElement.Name;
            if (name == "ok")
                return null;
            if (xml.DocumentElement.Name != "edit")
            {
                ModuleRC.ptrModule.Log("Invalid node: " + xml.DocumentElement.Name, true);
                return null;
            }
            if (xml.DocumentElement.Attributes["type"].Value != "edit" && xml.DocumentElement.Attributes["type"].Value != "new")
                return null;
            Change change = new Change(xml.DocumentElement.Attributes["title"].Value, xml.DocumentElement.Attributes["summary"].Value, xml.DocumentElement.Attributes["user"].Value);
            change.New = xml.DocumentElement.Attributes["type"].Value == "new";
            change.Site = xml.DocumentElement.Attributes["server_name"].Value;
            change.Minor = bool.Parse(xml.DocumentElement.Attributes["minor"].Value);
            change.Bot = bool.Parse(xml.DocumentElement.Attributes["bot"].Value);
            if (xml.DocumentElement.Attributes["oldid"] != null)
                change.oldid = xml.DocumentElement.Attributes["oldid"].Value;
            if (xml.DocumentElement.Attributes["revid"] != null)
                change.diff = xml.DocumentElement.Attributes["revid"].Value;

            change.Size = "unknown";

            if (!change.New && xml.DocumentElement.Attributes["length_old"] != null && xml.DocumentElement.Attributes["length_new"] != null)
            {
                int size = int.Parse(xml.DocumentElement.Attributes["length_new"].Value) - int.Parse(xml.DocumentElement.Attributes["length_old"].Value);
                change.Size = size.ToString();
                if (size > 0)
                    change.Size = "+" + change.Size;
            }
            change.Special = change.Page.StartsWith("Special:");
            return change;
        }

        private void Loop()
        {
            while (!RecentChanges.streamReader.EndOfStream)
            {
                string message = RecentChanges.streamReader.ReadLine();
                Change edit = String2Change(message);
                if (edit == null)
                {
                    Thread.Sleep(200);
                    continue;
                }
                List<RecentChanges> recentChanges = new List<RecentChanges>();
                lock (RecentChanges.recentChangesList)
                {
                    recentChanges.AddRange(RecentChanges.recentChangesList);
                }
                foreach (RecentChanges curr in recentChanges)
                {
                    if (edit.Special && !GetConfig(curr.channel, "RC.Special", false))
                    {
                        continue;
                    }
                    if (GetConfig(curr.channel, "RC.Enabled", false))
                    {
                        lock (curr.MonitoredPages)
                        {
                            foreach (RecentChanges.IWatch iwatch in curr.MonitoredPages)
                            {
                                RecentChanges.wiki wiki_ = iwatch.URL;
                                if (iwatch.Channel == edit.Site || iwatch.Channel == "all")
                                {
                                    if (iwatch.Channel == "all")
                                        wiki_ = RecentChanges.WikiFromChannelID(edit.Site);

                                    if (edit.Page == iwatch.Page)
                                    {
                                        if (edit.Size != null)
                                            edit.Summary = "[" + edit.Size + "] " + edit.Summary;
                                        if (iwatch.URL == null)
                                            DebugLog("NULL pointer on idata 1", 2);
                                        IRC.DeliverMessage(Format(wiki_.name, wiki_.url, edit.Page, edit.User, edit.diff, edit.Summary,
                                            curr.channel, edit.Bot, edit.New, edit.Minor), curr.channel.Name, libirc.Defs.Priority.Low);
                                    }
                                    else if (iwatch.Page.EndsWith("*") && edit.Page.StartsWith(iwatch.Page.Replace("*", "")))
                                    {
                                        if (iwatch.URL == null)
                                        {
                                            DebugLog("NULL pointer on idata 2", 2);
                                        }
                                        IRC.DeliverMessage(Format(wiki_.name, wiki_.url, edit.Page, edit.User, edit.diff, edit.Summary, curr.channel, edit.Bot,
                                                edit.New, edit.Minor), curr.channel.Name, libirc.Defs.Priority.Low);
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
                RecentChanges.channels = new List<string>();
                if (!File.Exists(RecentChanges.WikiFile))
                {
                    File.WriteAllText(RecentChanges.WikiFile, "mediawiki.org");
                }
                try
                {
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
                    while (Core.IsRunning)
                    {
                        try
                        {
                            this.Loop();
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
                        catch (Exception fail)
                        {
                            HandleException(fail);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception fail)
                {
                    HandleException(fail);
                    // abort
                }
            }
            catch (ThreadAbortException)
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
