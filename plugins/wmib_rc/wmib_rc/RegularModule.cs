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
    public class ModuleRC : Module
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
            if (message.StartsWith(config.CommandPrefix + "RC-"))
            {
                if (channel.Users.IsApproved(invoker, "trust"))
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

            if (message.StartsWith(config.CommandPrefix + "recentchanges- "))
            {
                if (channel.Users.IsApproved(invoker, "root"))
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

            if (message.StartsWith(config.CommandPrefix + "RC+ "))
            {
                if (channel.Users.IsApproved(invoker, "trust"))
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

            if (message == config.CommandPrefix + "recentchanges-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
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

            if (message == config.CommandPrefix + "recentchanges-on")
            {
                if (channel.Users.IsApproved(invoker, "recentchanges-manage"))
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

            if (message.StartsWith(config.CommandPrefix + "recentchanges+"))
            {
                if (channel.Users.IsApproved(invoker, "recentchanges-manage"))
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
                            core.irc._SlowQueue.DeliverMessage(messages.get("Wiki+", channel.Language), channel.Name);
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
            Version = "1.2.0.2";
            return true;
        }

        public string Format(string name_url, string url, string page, string username, string link, string summary, config.channel chan, bool bot, bool New, bool minor)
        {


            if (GetConfig(chan, "RC.Template", "") == "")
            {
                if (!New)
                {
                    return messages.get("fl", chan.Language, new List<string> { "12" + name_url + "", "" + page + "", "" + username + "", url + "?diff=" + link, summary });
                }
                return messages.get("fl", chan.Language, new List<string> { "12" + name_url + "", "" + page + "", "" + username + "", url + "?title=" + name_url, summary });
            }

            string action = "modified";
            string flags = "";

            if (minor)
            {
                flags += "minor edit, ";
            }

            if (New)
            {
                flags += "new page, ";
                action = "created";
            }

            if (bot)
            {
                flags += "bot edit";
            }

            if (flags.EndsWith(", "))
            {
                flags = flags.Substring(0, flags.Length - 2);
            }

            string fu = url + "?diff=" + link;
            if (New)
            {
                fu = url + "?title=" + System.Web.HttpUtility.UrlEncode(page).Replace("+", "_");
            }

            return GetConfig(chan, "RC.Template", "").Replace("$wiki", name_url)
                   .Replace("$encoded_wiki_page", System.Web.HttpUtility.UrlEncode(page).Replace("+", "_").Replace("%3a", ":").Replace("%2f", "/").Replace("%28", "(").Replace("%29", ")"))
                   .Replace("$encoded_wiki_username", System.Web.HttpUtility.UrlEncode(username).Replace("+", "_").Replace("%3a", ":").Replace("%2f", "/").Replace("%28", "(").Replace("%29", ")"))
                   .Replace("$encoded_page", System.Web.HttpUtility.UrlEncode(page))
                   .Replace("$encoded_username", System.Web.HttpUtility.UrlEncode(username))
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
            // get a page
            if (!text.Contains(variables.color + "14[["))
            {
                core.DebugLog("Parser error #1", 6);
                return null;
            }
            Change change = new Change("", "", "");

            if (text.Contains(variables.color + "4 "))
            {
                string flags = text.Substring(text.IndexOf(variables.color + "4 ") + 3);
                if (flags.Contains(variables.color))
                {
                    flags = flags.Substring(0, flags.IndexOf(variables.color));
                }
                if (flags.Contains("N"))
                {
                    change.New = true;
                }

                if (flags.Contains("M"))
                {
                    change.Minor = true;
                }
                if (flags.Contains("B"))
                {
                    change.Bot = true;
                }
            }
            
            change.Page = text.Substring(text.IndexOf(variables.color + "14[[") + 5);
            change.Page = change.Page.Substring(3);
            if (!change.Page.Contains(variables.color + "14]]"))
            {
                core.DebugLog("Parser error #2", 6);
                return null;
            }

            change.Page = change.Page.Substring(0, change.Page.IndexOf(variables.color + "14"));

            text = text.Substring(text.IndexOf(variables.color + "14]]") + 5);

            if (text.Contains("?oldid="))
            {
                change.oldid = text.Substring(text.IndexOf("?oldid=") + 7);

                if (!change.oldid.Contains("&") && !change.oldid.Contains(" "))
                {
                    core.DebugLog("Parser error #4", 6);
                    return null;
                }

                if (change.oldid.Contains(" "))
                {
                    change.oldid = change.oldid.Substring(0, change.oldid.IndexOf(" "));
                }

                if (change.oldid.Contains("&"))
                {
                    change.oldid = change.oldid.Substring(0, change.oldid.IndexOf("&"));
                }
            }

            if (text.Contains("?diff="))
            {
                change.diff = text.Substring(text.IndexOf("?diff=") + 6);

                if (!change.diff.Contains("&"))
                {
                    core.DebugLog("Parser error #4", 6);
                    return null;
                }
                change.diff = change.diff.Substring(0, change.diff.IndexOf("&"));
            }


            text = text.Substring(text.IndexOf("?diff=") + 6);

            if (!text.Contains(variables.color + "03"))
            {
                core.DebugLog("Parser error #5", 6);
                return null;
            }

            change.User = text.Substring(text.IndexOf(variables.color + "03") + 3);

            if (!change.User.Contains(variables.color + " " + variables.color + "5*"))
            {
                core.DebugLog("Parser error #6", 6);
                return null;
            }

            change.User = change.User.Substring(0, change.User.IndexOf(variables.color + " " + variables.color + "5*"));

            if (!text.Contains(variables.color + "5"))
            {
                core.DebugLog("Parser error #7", 6);
                return null;
            }

            text = text.Substring(text.IndexOf(variables.color + "5"));

            if (text.Contains("("))
            {
                change.Size = text.Substring(text.IndexOf("(") + 1);

                if (!change.Size.Contains(")"))
                {
                    core.DebugLog("Parser error #10", 6);
                    return null;
                }

                change.Size = change.Size.Substring(0, change.Size.IndexOf(")"));
            }

            if (!text.Contains(variables.color + "10"))
            {
                core.DebugLog("Parser error #14", 6);
                return null;
            }

            change.Description = text.Substring(text.IndexOf(variables.color + "10") + 3);

            change.Special = change.Page.StartsWith("Special:");

            return change;
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
                    Log("Loading feed", false);
                    lock (RecentChanges.channels)
                    {
                        foreach (string chan in list)
                        {
                            RecentChanges.channels.Add(chan);
                        }
                    }
                    RecentChanges.Connect();
                    Log("Loaded feed", false);
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
                                if (!message.Contains(" PRIVMSG "))
                                {
                                    continue;
                                }
                                Change edit = String2Change(message);
                                //Match Edit = RecentChanges.line.Match(message);
                                if (edit != null)
                                {
                                    string _channel = message.Substring(message.IndexOf("PRIVMSG"));
                                    _channel = _channel.Substring(_channel.IndexOf("#"));
                                    _channel = _channel.Substring(0, _channel.IndexOf(" "));
                                    List<RecentChanges> R = new List<RecentChanges>();
                                    lock (RecentChanges.rc)
                                    {
                                        R.AddRange(RecentChanges.rc);
                                    }
                                    foreach (RecentChanges curr in R)
                                    {
                                        if (curr != null)
                                        {
                                            if (edit.Special && !GetConfig(curr.channel, "RC.Special", false))
                                            {
                                                continue;
                                            }
                                            if (GetConfig(curr.channel, "RC.Enabled", false))
                                            {
                                                lock (curr.pages)
                                                {
                                                    foreach (RecentChanges.IWatch w in curr.pages)
                                                    {
                                                        if (w != null)
                                                        {
                                                            if (w.Channel == _channel || w.Channel == "all")
                                                            {
                                                                if (edit.Page == w.Page)
                                                                {
                                                                    if (edit.Size != null)
                                                                    {
                                                                        edit.Description = "[" + edit.Size + "] " + edit.Description;
                                                                    }
                                                                    if (w.URL == null)
                                                                    {
                                                                        DebugLog("NULL pointer on idata 1", 2);
                                                                    }
                                                                    core.irc._SlowQueue.DeliverMessage(
                                                                       Format(w.URL.name, w.URL.url, edit.Page, edit.User, edit.diff, edit.Description, curr.channel, edit.Bot, edit.New, edit.Minor), curr.channel.Name, IRC.priority.low);
                                                                }
                                                                else
                                                                    if (w.Page.EndsWith("*"))
                                                                    {
                                                                        if (edit.Page.StartsWith(w.Page.Replace("*", "")))
                                                                        {
                                                                            if (w.URL == null)
                                                                            {
                                                                                DebugLog("NULL pointer on idata 2", 2);
                                                                            }
                                                                            core.irc._SlowQueue.DeliverMessage(
                                                                            Format(w.URL.name, w.URL.url, edit.Page, edit.User, edit.diff, edit.Description, curr.channel, edit.Bot, edit.New, edit.Minor), curr.channel.Name, IRC.priority.low);
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
                                else
                                {
                                    DebugLog("Error on: " + message);
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
                        catch (Exception fail)
                        {
                            core.LastText = message;
                            handleException(fail);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception fail)
                {
                    handleException(fail);
                    // abort
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                handleException(fail);
            }
        }

        public override bool Hook_GetConfig(config.channel chan, User invoker, string config)
        {
            switch (config)
            {
                case "recent-changes-template":
                    core.irc._SlowQueue.DeliverMessage("Value of " + config + " is: " + GetConfig(chan, "RC.Template", "<default value>"), chan);
                    return true;
            }
            return false;
        }

        public override bool Hook_SetConfig(config.channel chan, User invoker, string config, string value)
        {
            switch (config)
            {
                case "recent-changes-template":
                    if (value != "null")
                    {
                        Module.SetConfig(chan, "RC.Template", value);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan);
                        chan.SaveConfig();
                        return true;
                    }
                    else
                    {
                        Module.SetConfig(chan, "RC.Template", "");
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { "null", config }), chan);
                        chan.SaveConfig();
                        return true;
                    }
            }
            return false;
        }
    }
}
