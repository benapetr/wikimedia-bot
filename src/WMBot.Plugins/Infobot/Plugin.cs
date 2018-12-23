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

namespace wmib.Extensions
{
    public class InfobotModule : wmib.Module
    {
        public static readonly string PermissionAdd = "infobot_add";
        public static readonly string PermissionDel = "infobot_del";
        public static readonly string PermissionSnaphot = "infobot_create_snapshot";
        public static readonly string PermissionRestoreSnapshot = "infobot_restore_snapshot";
        public static readonly string PermissionDeleteSnapshot = "infobot_delete_snapshot";
        public static readonly string PermissionShare = "infobot_share";
        public static readonly string PermissionIgnore = "infobot_manage_ignore";
        public static readonly string PermissionManage = "infobot_manage";
        private readonly List<Infobot.InfoItem> jobs = new List<Infobot.InfoItem>();
        public static bool running;
        private bool Unwritable;
        public static bool Snapshots = true;
        public readonly static string SnapshotsDirectory = "snapshots";
        private InfobotWriter writer;

        public override bool Construct()
        {
            RestartOnModuleCrash = true;
            Version = new Version(1, 8, 0, 2);
            return true;
        }

        public override void RegisterPermissions()
        {
            if (!Security.Roles.ContainsKey("infobot") && !Security.Roles.ContainsKey("infobot_admin"))
            {
                Security.Roles.Add("infobot", new Security.Role(1));
                Security.Roles["infobot"].Grant(PermissionAdd);
                Security.Roles["infobot"].Grant(PermissionDel);
                Security.Roles["infobot"].Grant(PermissionSnaphot);
                Security.Roles.Add("infobot_admin", new Security.Role(1));
                Security.Roles["infobot_admin"].Grant(Security.Roles["infobot"]);
                Security.Roles["infobot_admin"].Grant(PermissionRestoreSnapshot);
                Security.Roles["infobot_admin"].Grant(PermissionDeleteSnapshot);
                Security.Roles["infobot_admin"].Grant(PermissionManage);
                Security.Roles["infobot_admin"].Grant(PermissionShare);
            }
            if (Security.Roles.ContainsKey("operator"))
            {
                Security.Roles["operator"].Grant(Security.Roles["infobot_admin"]);
            }
            if (Security.Roles.ContainsKey("trusted"))
            {
                Security.Roles["trusted"].Grant(Security.Roles["infobot"]);
            }
            if (Security.Roles.ContainsKey("admin"))
            {
                Security.Roles["admin"].Grant(Security.Roles["infobot_admin"]);
            }
        }

        public string getDB(ref Channel chan)
        {
            return GetConfig(chan, "Infobot.Keydb", Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + chan.Name + ".db");
        }

        public override void Hook_ChannelDrop(Channel chan)
        {
            try
            {
                if (Directory.Exists(SnapshotsDirectory + Path.DirectorySeparatorChar + chan.Name))
                {
                    Syslog.Log("Removing snapshots for " + chan.Name);
                    Directory.Delete(SnapshotsDirectory + Path.DirectorySeparatorChar + chan.Name, true);
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail, "infobot");
            }
        }

        public override void Hook_Channel(Channel channel)
        {
            Syslog.Log("Loading " + channel.Name);
            if (channel == null)
            {
                Syslog.Log("NULL");
            }
            if (Snapshots)
            {
                try
                {
                    if (Directory.Exists(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name) == false)
                    {
                        Syslog.Log("Creating directory for infobot for " + channel.Name);
                        Directory.CreateDirectory(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name);
                    }
                }
                catch (Exception fail)
                {
                    Core.HandleException(fail, "infobot");
                }
            }
            if (channel.RetrieveObject("Infobot") == null)
            {
                // sensitivity
                bool cs = GetConfig(channel, "Infobot.Case", true);
                channel.RegisterObject(new Infobot(getDB(ref channel), channel, this, cs), "Infobot");
            }
        }

        public override bool Hook_OnRegister()
        {
            bool success = true;
            DebugLog("Registering channels");
            try
            {
                if (!Directory.Exists(SnapshotsDirectory))
                {
                    Syslog.Log("Creating snapshot directory for infobot");
                    Directory.CreateDirectory(SnapshotsDirectory);
                }
            }
            catch (Exception fail)
            {
                Snapshots = false;
                Core.HandleException(fail, "infobot");
            }
            writer = new InfobotWriter();
            writer.Init();
            lock (Configuration.Channels)
            {
                foreach (Channel channel in Configuration.Channels)
                {
                    Channel curr = channel;
                    bool cs = GetConfig(curr, "Infobot.Case", true);
                    if (!channel.RegisterObject(new Infobot(getDB(ref curr), channel, this, cs), "Infobot"))
                    {
                        success = false;
                    }
                    if (Snapshots)
                    {
                        try
                        {
                            if (Directory.Exists(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name) == false)
                            {
                                Syslog.Log("Creating directory for infobot for " + channel.Name);
                                Directory.CreateDirectory(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name);
                            }
                        }
                        catch (Exception fail)
                        {
                            Core.HandleException(fail, "infobot");
                        }
                    }
                }
            }
            RegisterCommand(new GenericCommand("list-keys", this.list_keys));
            if (!success)
            {
                Syslog.Log("Failed to register infobot objects in some channels", true);
            }
            return success;
        }

        public override bool Hook_OnUnload()
        {
            bool success = true;
            UnregisterCommand("list-keys");
            if (writer != null)
            {
                writer.thread.Abort();
                writer = null;
            }
            lock (Configuration.Channels)
            {
                foreach (Channel channel in Configuration.Channels)
                {
                    if (!channel.UnregisterObject("Infobot"))
                    {
                        success = false;
                    }
                }
            }
            if (!success)
            {
                Syslog.Log("Failed to unregister infobot objects in some channels", true);
            }
            return success;
        }

        public override string Extension_DumpHtml(Channel channel)
        {
            string HTML = "";
            Infobot info = (Infobot)channel.RetrieveObject("Infobot");
            if (info != null)
            {
                List<Infobot.InfobotKey> list = new List<Infobot.InfobotKey>();
                List<Infobot.InfobotAlias> aliases = new List<Infobot.InfobotAlias>();
                lock (info)
                {
                    if (GetConfig(channel, "Infobot.Sorted", false))
                    {
                        list = info.SortedItem();
                    }
                    else
                    {
                        list.AddRange(info.Keys);
                    }
                    aliases.AddRange(info.Aliases);
                }
                string JSON_blob = Newtonsoft.Json.JsonConvert.SerializeObject(list);
                JSON_blob += "\n\n" + Newtonsoft.Json.JsonConvert.SerializeObject(aliases);
                string JSON_file = Configuration.Paths.DumpDir + "/" + channel.Name + "_dump.js";
                File.WriteAllText(JSON_file, JSON_blob);
                HTML += "JSON blob: <a href=\"" + System.Web.HttpUtility.UrlEncode(channel.Name) + "_dump.js\">open</a>";
                HTML += "\n<table border=1 class=\"infobot\" width=100%>\n<tr><th width=10%>Key</th><th>Value</th></tr>\n";
                if (list.Count > 0)
                {
                    foreach (Infobot.InfobotKey Key in list)
                    {
                        HTML += Core.HTML.AddKey(Key.Key, Key.Text);
                    }
                }
                HTML += "</table>\n";
                HTML += "<h4>Aliases</h4>\n<table class=\"infobot\" border=1 width=100%>\n";
                lock (info)
                {
                    foreach (Infobot.InfobotAlias data in info.Aliases)
                    {
                        HTML += Core.HTML.AddLink(data.Name, data.Key);
                    }
                }
                HTML += "</table><br />\n";
            }
            return HTML;
        }

        private void list_keys(CommandParams p)
        {
            Infobot info = (Infobot)p.SourceChannel.RetrieveObject("Infobot");
            if (info == null)
                return;

            string result = "";
            foreach (Infobot.InfobotKey key in info.Keys)
            {
                result += key.Key + ", ";
            }

            if (result.EndsWith(", ", StringComparison.InvariantCulture))
            {
                result = result.Substring(0, result.Length - 2);
            }

            if (result.Length > 450)
            {
                result = result.Substring(0, 450) + "...";
            }
            IRC.DeliverMessage(result, p.SourceChannel);
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            // "\uff01" is the full-width version of "!".
            if ((message.StartsWith("!", StringComparison.InvariantCulture) || message.StartsWith("\uff01", StringComparison.InvariantCulture)) && GetConfig(channel, "Infobot.Enabled", true))
            {
                while (Unwritable)
                {
                    Thread.Sleep(10);
                }
                Unwritable = true;
                Infobot.InfoItem item = new Infobot.InfoItem
                {
                    Channel = channel,
                    Name = "!" + message.Substring(1),
                    User = invoker,
                };
                jobs.Add(item);
                Unwritable = false;
            }

            Infobot infobot = null;

            if (message.StartsWith(Configuration.System.CommandPrefix, StringComparison.InvariantCulture))
            {
                infobot = (Infobot)channel.RetrieveObject("Infobot");
                if (infobot == null)
                {
                    Syslog.Log("Object Infobot in " + channel.Name + " doesn't exist", true);
                }
                if (GetConfig(channel, "Infobot.Enabled", true))
                {
                    if (infobot != null)
                    {
                        infobot.Find(message, channel);
                        infobot.RSearch(message, channel);
                    }
                }
            }

            if (Snapshots)
            {
                if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-recovery ", StringComparison.InvariantCulture))
                {
                    if (channel.SystemUsers.IsApproved(invoker, PermissionRestoreSnapshot))
                    {
                        string name = message.Substring("@infobot-recovery ".Length);
                        if (!GetConfig(channel, "Infobot.Enabled", true))
                        {
                            IRC.DeliverMessage("Infobot is not enabled in this channel", channel, libirc.Defs.Priority.Low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.RecoverSnapshot(channel, name);
                        }
                        return;
                    }
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                    }
                    return;
                }

                if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-snapshot ", StringComparison.InvariantCulture))
                {
                    if (channel.SystemUsers.IsApproved(invoker, PermissionSnaphot))
                    {
                        string name = message.Substring("@infobot-snapshot ".Length);
                        if (!GetConfig(channel, "Infobot.Enabled", true))
                        {
                            IRC.DeliverMessage("Infobot is not enabled in this channel", channel, libirc.Defs.Priority.Low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.CreateSnapshot(channel, name);
                        }
                        return;
                    }
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                    }
                    return;
                }

                if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-set-raw ", StringComparison.InvariantCulture))
                {
                    if (channel.SystemUsers.IsApproved(invoker, PermissionAdd))
                    {
                        string name = message.Substring("@infobot-set-raw ".Length);
                        if (!GetConfig(channel, "Infobot.Enabled", true))
                        {
                            IRC.DeliverMessage("Infobot is not enabled in this channel", channel, libirc.Defs.Priority.Low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.SetRaw(name, invoker.Nick, channel);
                            return;
                        }
                    }
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                    }
                    return;
                }

                if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-unset-raw ", StringComparison.InvariantCulture))
                {
                    if (channel.SystemUsers.IsApproved(invoker, PermissionAdd))
                    {
                        string name = message.Substring("@infobot-unset-raw ".Length);
                        if (!GetConfig(channel, "Infobot.Enabled", true))
                        {
                            IRC.DeliverMessage("Infobot is not enabled in this channel", channel, libirc.Defs.Priority.Low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.UnsetRaw(name, invoker.Nick, channel);
                            return;
                        }
                    }
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                    }
                    return;
                }

                if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-snapshot-rm ", StringComparison.InvariantCulture))
                {
                    if (channel.SystemUsers.IsApproved(invoker, PermissionDeleteSnapshot))
                    {
                        string name = message.Substring("@infobot-snapshot-rm ".Length);
                        name.Replace(".", "");
                        name.Replace("/", "");
                        name.Replace("\\", "");
                        name.Replace("*", "");
                        name.Replace("?", "");
                        if (name == "")
                        {
                            IRC.DeliverMessage("You should specify a file name", channel);
                            return;
                        }
                        if (!File.Exists(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name + Path.DirectorySeparatorChar + name))
                        {
                            IRC.DeliverMessage("File not found", channel);
                            return;
                        }
                        File.Delete(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name + Path.DirectorySeparatorChar + name);
                        IRC.DeliverMessage("Requested file was removed", channel);
                        return;
                    }
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                    }
                    return;
                }

                if (message == Configuration.System.CommandPrefix + "infobot-snapshot-ls")
                {
                    string files = "";
                    DirectoryInfo di = new DirectoryInfo(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name);
                    FileInfo[] rgFiles = di.GetFiles("*");
                    int curr = 0;
                    int displaying = 0;
                    foreach (FileInfo fi in rgFiles)
                    {
                        curr++;
                        if (files.Length < 200)
                        {
                            files += fi.Name + " ";
                            displaying++;
                        }
                    }
                    string response;
                    if (curr == displaying)
                    {
                        response = "There are " + displaying + " files: " + files;
                    }
                    else
                    {
                        response = "There are " + curr + " files, but displaying only " + displaying + " of them: " + files;
                    }
                    if (curr == 0)
                    {
                        response = "There is no snapshot so far, create one!:)";
                    }
                    IRC.DeliverMessage(response, channel.Name);
                    return;
                }
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-share-trust+ "))
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionShare))
                {
                    if (channel.SharedDB != "local")
                    {
                        IRC.DeliverMessage(messages.Localize("infobot16", channel.Language), channel);
                        return;
                    }
                    if (channel.SharedDB != "local" && channel.SharedDB != "")
                    {
                        IRC.DeliverMessage(messages.Localize("infobot15", channel.Language), channel);
                        return;
                    }
                    if (message.Length <= "@infobot-share-trust+ ".Length)
                    {
                        IRC.DeliverMessage(messages.Localize("db6", channel.Language), channel.Name);
                        return;
                    }
                    string name = message.Substring("@infobot-share-trust+ ".Length);
                    Channel guest = Core.GetChannel(name);
                    if (guest == null)
                    {
                        IRC.DeliverMessage(messages.Localize("db8", channel.Language), channel.Name);
                        return;
                    }
                    if (channel.SharedLinkedChan.Contains(guest))
                    {
                        IRC.DeliverMessage(messages.Localize("db14", channel.Language), channel.Name);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("db1", channel.Language, new List<string> { name }), channel.Name);
                    lock (channel.SharedLinkedChan)
                    {
                        channel.SharedLinkedChan.Add(guest);
                    }
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-ignore- "))
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionIgnore))
                {
                    string item = message.Substring("@infobot-ignore+ ".Length);
                    if (item != "")
                    {
                        if (!channel.Infobot_IgnoredNames.Contains(item))
                        {
                            IRC.DeliverMessage(messages.Localize("infobot-ignore-found", channel.Language, new List<string> { item }), channel);
                            return;
                        }
                        channel.Infobot_IgnoredNames.Remove(item);
                        IRC.DeliverMessage(messages.Localize("infobot-ignore-rm", channel.Language, new List<string> { item }), channel);
                        channel.SaveConfig();
                        return;
                    }
                }
                else
                {
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                    }
                }
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-ignore+ "))
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionIgnore))
                {
                    string item = message.Substring("@infobot-ignore+ ".Length);
                    if (item != "")
                    {
                        if (channel.Infobot_IgnoredNames.Contains(item))
                        {
                            IRC.DeliverMessage(messages.Localize("infobot-ignore-exist", channel.Language, new List<string> { item }), channel);
                            return;
                        }
                        channel.Infobot_IgnoredNames.Add(item);
                        IRC.DeliverMessage(messages.Localize("infobot-ignore-ok", channel.Language, new List<string> { item }), channel);
                        channel.SaveConfig();
                        return;
                    }
                }
                else
                {
                    if (!channel.SuppressWarnings)
                    {
                        IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                    }
                }
            }

            if (message == Configuration.System.CommandPrefix + "infobot-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionManage))
                {
                    if (!GetConfig(channel, "Infobot.Enabled", true))
                    {
                        IRC.DeliverMessage(messages.Localize("infobot1", channel.Language), channel);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("infobot2", channel.Language), channel, libirc.Defs.Priority.High);
                    SetConfig(channel, "Infobot.Enabled", false);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-share-trust- "))
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionShare))
                {
                    if (channel.SharedDB != "local")
                    {
                        IRC.DeliverMessage(messages.Localize("infobot16", channel.Language), channel);
                        return;
                    }
                    if (message.Length <= "@infobot-share-trust+ ".Length)
                    {
                        IRC.DeliverMessage(messages.Localize("db6", channel.Language), channel);
                        return;
                    }
                    string name = message.Substring("@infobot-share-trust- ".Length);
                    Channel target = Core.GetChannel(name);
                    if (target == null)
                    {
                        IRC.DeliverMessage(messages.Localize("db8", channel.Language), channel);
                        return;
                    }
                    if (channel.SharedLinkedChan.Contains(target))
                    {
                        channel.SharedLinkedChan.Remove(target);
                        IRC.DeliverMessage(messages.Localize("db2", channel.Language, new List<string> { name }), channel);
                        channel.SaveConfig();
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("db4", channel.Language), channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-detail "))
            {
                if ((message.Length) <= "@infobot-detail ".Length)
                {
                    IRC.DeliverMessage(messages.Localize("db6", channel.Language), channel);
                    return;
                }
                if (GetConfig(channel, "Infobot.Enabled", true))
                {
                    if (channel.SharedDB == "local" || channel.SharedDB == "")
                    {
                        if (infobot != null)
                        {
                            infobot.InfobotDetail(message.Substring(16), channel);
                        }
                        return;
                    }
                    if (channel.SharedDB != "")
                    {
                        Channel db = Core.GetChannel(channel.SharedDB);
                        if (db == null)
                        {
                            IRC.DeliverMessage("Error, null pointer to shared channel", channel, libirc.Defs.Priority.Low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.InfobotDetail(message.Substring(16), channel);
                        }
                        return;
                    }
                    return;
                }
                IRC.DeliverMessage("Infobot is not enabled on this channel", channel, libirc.Defs.Priority.Low);
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "infobot-link "))
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionShare))
                {
                    if (channel.SharedDB == "local")
                    {
                        IRC.DeliverMessage(messages.Localize("infobot17", channel.Language), channel);
                        return;
                    }
                    if (channel.SharedDB != "")
                    {
                        IRC.DeliverMessage(messages.Localize("infobot18", channel.Language, new List<string> { channel.SharedDB }), channel);
                        return;
                    }
                    if ((message.Length - 1) < "@infobot-link ".Length)
                    {
                        IRC.DeliverMessage(messages.Localize("db6", channel.Language), channel);
                        return;
                    }
                    string name = message.Substring("@infobot-link ".Length);
                    Channel db = Core.GetChannel(name);
                    if (db == null)
                    {
                        IRC.DeliverMessage(messages.Localize("db8", channel.Language), channel);
                        return;
                    }
                    if (!Infobot.Linkable(db, channel))
                    {
                        IRC.DeliverMessage(messages.Localize("db9", channel.Language), channel);
                        return;
                    }
                    channel.SharedDB = name.ToLower();
                    IRC.DeliverMessage(messages.Localize("db10", channel.Language), channel);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "infobot-share-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionShare))
                {
                    if (channel.SharedDB == "")
                    {
                        IRC.DeliverMessage(messages.Localize("infobot14", channel.Language), channel);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("infobot13", channel.Language), channel);
                    foreach (Channel curr in Configuration.ChannelList)
                    {
                        if (curr.SharedDB == channel.Name.ToLower())
                        {
                            curr.SharedDB = "";
                            curr.SaveConfig();
                            IRC.DeliverMessage(messages.Localize("infobot19", curr.Language, new List<string> { invoker.Nick }), curr);
                        }
                    }
                    channel.SharedDB = "";
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "infobot-on")
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionManage))
                {
                    if (GetConfig(channel, "Infobot.Enabled", true))
                    {
                        IRC.DeliverMessage(messages.Localize("infobot3", channel.Language), channel);
                        return;
                    }
                    SetConfig(channel, "Infobot.Enabled", true);
                    channel.SaveConfig();
                    IRC.DeliverMessage(messages.Localize("infobot4", channel.Language), channel, libirc.Defs.Priority.High);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "infobot-share-on")
            {
                if (channel.SystemUsers.IsApproved(invoker, PermissionShare))
                {
                    if (channel.SharedDB == "local")
                    {
                        IRC.DeliverMessage(messages.Localize("infobot11", channel.Language), channel, libirc.Defs.Priority.High);
                        return;
                    }
                    if (channel.SharedDB != "local" && channel.SharedDB != "")
                    {
                        IRC.DeliverMessage(messages.Localize("infobot15", channel.Language), channel, libirc.Defs.Priority.High);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("infobot12", channel.Language), channel);
                    channel.SharedDB = "local";
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
            }
        }

        public override bool Hook_SetConfig(Channel chan, libirc.UserInfo invoker, string config, string value)
        {
            bool _temp_a;
            switch (config)
            {
                case "infobot-trim-white-space-in-name":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        SetConfig(chan, "Infobot.Trim-white-space-in-name", _temp_a);
                        IRC.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    IRC.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-auto-complete":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        SetConfig(chan, "Infobot.auto-complete", _temp_a);
                        IRC.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    IRC.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-sorted":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        SetConfig(chan, "Infobot.Sorted", _temp_a);
                        IRC.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    IRC.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-help":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        SetConfig(chan, "Infobot.Help", _temp_a);
                        IRC.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    IRC.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-case":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        SetConfig(chan, "Infobot.Case", _temp_a);
                        IRC.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        Infobot infobot = (Infobot)chan.RetrieveObject("Infobot");
                        if (infobot != null)
                        {
                            infobot.Sensitive = _temp_a;
                        }
                        return true;
                    }
                    IRC.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
            }
            return false;
        }

        public override void Hook_ReloadConfig(Channel chan)
        {
            if (chan.ExtensionObjects.ContainsKey("Infobot"))
            {
                chan.ExtensionObjects["Infobot"] = new Infobot(getDB(ref chan), chan, this);
            }
        }

        public override void Load()
        {
            try
            {
                Unwritable = false;
                while (Core.IsRunning && IsWorking)
                {
                    if (Unwritable)
                    {
                        Thread.Sleep(200);
                    }
                    else if (jobs.Count > 0)
                    {
                        Unwritable = true;
                        List<Infobot.InfoItem> list = new List<Infobot.InfoItem>();
                        list.AddRange(jobs);
                        jobs.Clear();
                        Unwritable = false;
                        foreach (Infobot.InfoItem item in list)
                        {
                            Infobot infobot = (Infobot)item.Channel.RetrieveObject("Infobot");
                            if (infobot != null)
                            {
                                infobot.InfobotExec(item.Name, item.User, item.Channel);
                            }
                        }
                    }
                    Thread.Sleep(200);
                }
            }
            catch (Exception b)
            {
                Unwritable = false;
                Console.WriteLine(b.InnerException);
            }
        }
    }
}
