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
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;

namespace wmib
{
    [Serializable()]
    public class RegularModule : Module
    {
        public List<infobot_core.InfoItem> jobs = new List<infobot_core.InfoItem>();
        public static bool running;
        public bool Unwritable;
        public bool Disabled;
        public static bool Snapshots = true;
        public readonly static string SnapshotsDirectory = "snapshots";
        public infobot_writer writer = null;

        public override bool Hook_OnUnload()
        {
            bool success = true;
            if (writer != null)
            {
                writer.Exit();
                writer = null;
            }
            lock (config.channels)
            {
                foreach (config.channel channel in config.channels)
                {
                    if (!channel.UnregisterObject("Infobot"))
                    {
                        success = false;
                    }
                }
            }
            if (!success)
            {
                core.Log("Failed to unregister infobot objects in some channels", true);
            }
            return success;
        }

        public string getDB(ref config.channel chan)
        {
            return Module.GetConfig(chan, "Infobot.Keydb", (string)variables.config + Path.DirectorySeparatorChar + chan.Name + ".db");
        }

        public override void Hook_ChannelDrop(config.channel chan)
        {
            try
            {
                if (Directory.Exists(SnapshotsDirectory + Path.DirectorySeparatorChar + chan.Name))
                {
                    core.Log("Removing snapshots for " + chan.Name);
                    Directory.Delete(SnapshotsDirectory + Path.DirectorySeparatorChar + chan.Name, true);
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }

        public override void Hook_Channel(config.channel channel)
        {
            core.Log("Loading " + channel.Name);
            if (channel == null)
            {
                core.Log("NULL");
            }
            if (Snapshots)
            {
                try
                {
                    if (Directory.Exists(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name) == false)
                    {
                        core.Log("Creating directory for infobot for " + channel.Name);
                        Directory.CreateDirectory(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name);
                    }
                }
                catch (Exception fail)
                {
                    core.handleException(fail);
                }
            }
            if (channel.RetrieveObject("Infobot") == null)
            {
                // sensitivity
                bool cs = Module.GetConfig(channel, "Infobot.Case", true);
                channel.RegisterObject(new infobot_core(getDB(ref channel), channel.Name, cs), "Infobot");
            }
        }

        public override bool Hook_OnRegister()
        {
            bool success = true;
            try
            {
                if (!Directory.Exists(SnapshotsDirectory))
                {
                    core.Log("Creating snapshot directory for infobot");
                    Directory.CreateDirectory(SnapshotsDirectory);
                }
            }
            catch (Exception fail)
            {
                Snapshots = false;
                core.handleException(fail);
            }
            writer = new infobot_writer();
            writer.Construct();
            core.InitialiseMod(writer);
            lock (config.channels)
            {
                foreach (config.channel channel in config.channels)
                {
                    config.channel curr = channel;
                    bool cs = Module.GetConfig(curr, "Infobot.Case", true);
                    if (!channel.RegisterObject(new infobot_core(getDB(ref curr), channel.Name, cs), "Infobot"))
                    {
                        success = false;
                    }
                    if (Snapshots)
                    {
                        try
                        {
                            if (Directory.Exists(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name) == false)
                            {
                                core.Log("Creating directory for infobot for " + channel.Name);
                                Directory.CreateDirectory(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name);
                            }
                        }
                        catch (Exception fail)
                        {
                            core.handleException(fail);
                        }
                    }
                }
            }
            if (!success)
            {
                core.Log("Failed to register infobot objects in some channels", true);
            }
            return success;
        }

        public override string Extension_DumpHtml(config.channel channel)
        {
            string HTML = "";
            infobot_core info = (infobot_core)channel.RetrieveObject("Infobot");
            if (info != null)
            {
                HTML += "\n<table border=1 class=\"infobot\" width=100%>\n<tr><th width=10%>Key</th><th>Value</th></tr>\n";
                List<infobot_core.InfobotKey> list = new List<infobot_core.InfobotKey>();
                lock (info)
                {
                    if (Module.GetConfig(channel, "Infobot.Sorted", false) != false)
                    {
                        list = info.SortedItem();
                    }
                    else
                    {
                        list.AddRange(info.Keys);
                    }
                }
                if (info.Keys.Count > 0)
                {
                    foreach (infobot_core.InfobotKey Key in list)
                    {
                        HTML += core.HTML.AddKey(Key.Key, Key.Text);
                    }
                }
                HTML += "</table>\n";
                HTML += "<h4>Aliases</h4>\n<table class=\"infobot\" border=1 width=100%>\n";
                lock (info)
                {
                    foreach (infobot_core.InfobotAlias data in info.Alias)
                    {
                        HTML += core.HTML.AddLink(data.Name, data.Key);
                    }
                }
                HTML += "</table><br>\n";
            }
            return HTML;
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            // "\uff01" is the full-width version of "!".
            if ((message.StartsWith("!") || message.StartsWith("\uff01")) && GetConfig(channel, "Infobot.Enabled", true))
            {
                while (Unwritable)
                {
                    Thread.Sleep(10);
                }
                Unwritable = true;
                infobot_core.InfoItem item = new infobot_core.InfoItem();
                item.Channel = channel;
                item.Name = "!" + message.Substring(1); // Normalizing "!".
                item.User = invoker.Nick;
                item.Host = invoker.Host;
                jobs.Add(item);
                Unwritable = false;
            }

            infobot_core infobot = null;

            if (message.StartsWith(config.CommandPrefix))
            {
                infobot = (infobot_core)channel.RetrieveObject("Infobot");
                if (infobot == null)
                {
                    core.Log("Object Infobot in " + channel.Name + " doesn't exist", true);
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
                if (message.StartsWith(config.CommandPrefix + "infobot-recovery "))
                {
                    if (channel.Users.IsApproved(invoker, "admin"))
                    {
                        string name = message.Substring("@infobot-recovery ".Length);
                        if (!GetConfig(channel, "Infobot.Enabled", true))
                        {
                            core.irc._SlowQueue.DeliverMessage("Infobot is not enabled in this channel", channel, IRC.priority.low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.RecoverSnapshot(channel, name);
                        }
                        return;
                    }
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                    }
                    return;
                }

                if (message.StartsWith(config.CommandPrefix + "infobot-snapshot "))
                {
                    if (channel.Users.IsApproved(invoker, "admin"))
                    {
                        string name = message.Substring("@infobot-snapshot ".Length);
                        if (!GetConfig(channel, "Infobot.Enabled", true))
                        {
                            core.irc._SlowQueue.DeliverMessage("Infobot is not enabled in this channel", channel, IRC.priority.low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.CreateSnapshot(channel, name);
                        }
                        return;
                    }
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                    }
                    return;
                }

                if (message.StartsWith(config.CommandPrefix + "infobot-set-raw "))
                {
                    if (channel.Users.IsApproved(invoker, "trust"))
                    {
                        string name = message.Substring("@infobot-set-raw ".Length);
                        if (!GetConfig(channel, "Infobot.Enabled", true))
                        {
                            core.irc._SlowQueue.DeliverMessage("Infobot is not enabled in this channel", channel, IRC.priority.low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.setRaw(name, invoker.Nick, channel);
                            return;
                        }
                    }
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                    }
                    return;
                }

                if (message.StartsWith(config.CommandPrefix + "infobot-unset-raw "))
                {
                    if (channel.Users.IsApproved(invoker, "trust"))
                    {
                        string name = message.Substring("@infobot-unset-raw ".Length);
                        if (!GetConfig(channel, "Infobot.Enabled", true))
                        {
                            core.irc._SlowQueue.DeliverMessage("Infobot is not enabled in this channel", channel, IRC.priority.low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.unsetRaw(name, invoker.Nick, channel);
                            return;
                        }
                    }
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                    }
                    return;
                }

                if (message.StartsWith(config.CommandPrefix + "infobot-snapshot-rm "))
                {
                    if (channel.Users.IsApproved(invoker, "admin"))
                    {
                        string name = message.Substring("@infobot-snapshot-rm ".Length);
                        name.Replace(".", "");
                        name.Replace("/", "");
                        name.Replace("\\", "");
                        name.Replace("*", "");
                        name.Replace("?", "");
                        if (name == "")
                        {
                            core.irc._SlowQueue.DeliverMessage("You should specify a file name", channel, IRC.priority.normal);
                            return;
                        }
                        if (!File.Exists(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name + Path.DirectorySeparatorChar + name))
                        {
                            core.irc._SlowQueue.DeliverMessage("File not found", channel, IRC.priority.normal);
                            return;
                        }
                        File.Delete(SnapshotsDirectory + Path.DirectorySeparatorChar + channel.Name + Path.DirectorySeparatorChar + name);
                        core.irc._SlowQueue.DeliverMessage("Requested file was removed", channel, IRC.priority.normal);
                        return;
                    }
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                    }
                    return;
                }

                if (message == config.CommandPrefix + "infobot-snapshot-ls")
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
                    string response = "";
                    if (curr == displaying)
                    {
                        response = "There are " + displaying.ToString() + " files: " + files;
                    }
                    else
                    {
                        response = "There are " + curr.ToString() + " files, but displaying only " + displaying.ToString() + " of them: " + files;
                    }
                    if (curr == 0)
                    {
                        response = "There is no snapshot so far, create one!:)";
                    }
                    core.irc._SlowQueue.DeliverMessage(response, channel.Name, IRC.priority.normal);
                    return;
                }
            }

            if (message.StartsWith(config.CommandPrefix + "infobot-share-trust+ "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (channel.shared != "local")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot16", channel.Language), channel);
                        return;
                    }
                    if (channel.shared != "local" && channel.shared != "")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot15", channel.Language), channel);
                        return;
                    }
                    else
                    {
                        if (message.Length <= "@infobot-share-trust+ ".Length)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db6", channel.Language), channel.Name);
                            return;
                        }
                        string name = message.Substring("@infobot-share-trust+ ".Length);
                        config.channel guest = core.getChannel(name);
                        if (guest == null)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db8", channel.Language), channel.Name);
                            return;
                        }
                        if (channel.SharedLinkedChan.Contains(guest))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db14", channel.Language), channel.Name);
                            return;
                        }
                        core.irc._SlowQueue.DeliverMessage(messages.get("db1", channel.Language, new List<string> { name }), channel.Name);
                        lock (channel.SharedLinkedChan)
                        {
                            channel.SharedLinkedChan.Add(guest);
                        }
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

            if (message.StartsWith(config.CommandPrefix + "infobot-ignore- "))
            {
                if (channel.Users.IsApproved(invoker, "trust"))
                {
                    string item = message.Substring("@infobot-ignore+ ".Length);
                    if (item != "")
                    {
                        if (!channel.Infobot_IgnoredNames.Contains(item))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-found", channel.Language, new List<string> { item }), channel);
                            return;
                        }
                        channel.Infobot_IgnoredNames.Remove(item);
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-rm", channel.Language, new List<string> { item }), channel);
                        channel.SaveConfig();
                        return;
                    }
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                    }
                }
            }

            if (message.StartsWith(config.CommandPrefix + "infobot-ignore+ "))
            {
                if (channel.Users.IsApproved(invoker, "trust"))
                {
                    string item = message.Substring("@infobot-ignore+ ".Length);
                    if (item != "")
                    {
                        if (channel.Infobot_IgnoredNames.Contains(item))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-exist", channel.Language, new List<string> { item }), channel);
                            return;
                        }
                        channel.Infobot_IgnoredNames.Add(item);
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot-ignore-ok", channel.Language, new List<string> { item }), channel);
                        channel.SaveConfig();
                        return;
                    }
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                    }
                }
            }

            if (message == config.CommandPrefix + "infobot-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "Infobot.Enabled", true))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot1", channel.Language), channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot2", channel.Language), channel, IRC.priority.high);
                        SetConfig(channel, "Infobot.Enabled", false);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "infobot-share-trust- "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (channel.shared != "local")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot16", channel.Language), channel);
                        return;
                    }
                    else
                    {
                        if (message.Length <= "@infobot-share-trust+ ".Length)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db6", channel.Language), channel);
                            return;
                        }
                        string name = message.Substring("@infobot-share-trust- ".Length);
                        config.channel target = core.getChannel(name);
                        if (target == null)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("db8", channel.Language), channel);
                            return;
                        }
                        if (channel.SharedLinkedChan.Contains(target))
                        {
                            channel.SharedLinkedChan.Remove(target);
                            core.irc._SlowQueue.DeliverMessage(messages.get("db2", channel.Language, new List<string> { name }), channel);
                            channel.SaveConfig();
                            return;
                        }
                        core.irc._SlowQueue.DeliverMessage(messages.get("db4", channel.Language), channel);
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "infobot-detail "))
            {
                if ((message.Length) <= "@infobot-detail ".Length)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("db6", channel.Language), channel);
                    return;
                }
                if (GetConfig(channel, "Infobot.Enabled", true))
                {
                    if (channel.shared == "local" || channel.shared == "")
                    {
                        if (infobot != null)
                        {
                            infobot.Info(message.Substring(16), channel);
                        }
                        return;
                    }
                    if (channel.shared != "")
                    {
                        config.channel db = core.getChannel(channel.shared);
                        if (db == null)
                        {
                            core.irc._SlowQueue.DeliverMessage("Error, null pointer to shared channel", channel, IRC.priority.low);
                            return;
                        }
                        if (infobot != null)
                        {
                            infobot.Info(message.Substring(16), channel);
                        }
                        return;
                    }
                    return;
                }
                core.irc._SlowQueue.DeliverMessage("Infobot is not enabled on this channel", channel, IRC.priority.low);
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "infobot-link "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (channel.shared == "local")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot17", channel.Language), channel);
                        return;
                    }
                    if (channel.shared != "")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot18", channel.Language, new List<string> { channel.shared }), channel);
                        return;
                    }
                    if ((message.Length - 1) < "@infobot-link ".Length)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("db6", channel.Language), channel);
                        return;
                    }
                    string name = message.Substring("@infobot-link ".Length);
                    config.channel db = core.getChannel(name);
                    if (db == null)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("db8", channel.Language), channel);
                        return;
                    }
                    if (!infobot_core.Linkable(db, channel))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("db9", channel.Language), channel);
                        return;
                    }
                    channel.shared = name.ToLower();
                    core.irc._SlowQueue.DeliverMessage(messages.get("db10", channel.Language), channel);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "infobot-share-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (channel.shared == "")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot14", channel.Language), channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot13", channel.Language), channel);
                        foreach (config.channel curr in config.channels)
                        {
                            if (curr.shared == channel.Name.ToLower())
                            {
                                curr.shared = "";
                                curr.SaveConfig();
                                core.irc._SlowQueue.DeliverMessage(messages.get("infobot19", curr.Language, new List<string> { invoker.Nick }), curr);
                            }
                        }
                        channel.shared = "";
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "infobot-on")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "Infobot.Enabled", true))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot3", channel.Language), channel);
                        return;
                    }
                    SetConfig(channel, "Infobot.Enabled", true);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage(messages.get("infobot4", channel.Language), channel, IRC.priority.high);
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "infobot-share-on")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (channel.shared == "local")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot11", channel.Language), channel, IRC.priority.high);
                        return;
                    }
                    if (channel.shared != "local" && channel.shared != "")
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot15", channel.Language), channel, IRC.priority.high);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("infobot12", channel.Language), channel);
                        channel.shared = "local";
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }
        }

        public override bool Hook_SetConfig(config.channel chan, User invoker, string config, string value)
        {
            bool _temp_a;
            switch (config)
            {
                case "infobot-trim-white-space-in-name":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.Trim-white-space-in-name", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-auto-complete":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.auto-complete", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-sorted":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.Sorted", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-help":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.Help", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
                case "infobot-case":
                    if (bool.TryParse(value, out _temp_a))
                    {
                        Module.SetConfig(chan, "Infobot.Case", _temp_a);
                        core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                        chan.SaveConfig();
                        infobot_core infobot = (infobot_core)chan.RetrieveObject("Infobot");
                        if (infobot != null)
                        {
                            infobot.Sensitive = _temp_a;
                        }
                        return true;
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                    return true;
            }
            return false;
        }

        public override bool Construct()
        {
            Name = "Infobot core";
            Reload = true;
            start = true;
            Version = "1.6.0";
            return true;
        }

        public override void Hook_ReloadConfig(config.channel chan)
        {
            if (chan.ExtensionObjects.ContainsKey("Infobot"))
            {
                chan.ExtensionObjects["Infobot"] = new infobot_core(getDB(ref chan), chan.Name);
            }
        }

        public override void Load()
        {
            try
            {
                Unwritable = false;
                while (Disabled != true)
                {
                    if (Unwritable)
                    {
                        Thread.Sleep(200);
                    }
                    else if (jobs.Count > 0)
                    {
                        Unwritable = true;
                        List<infobot_core.InfoItem> list = new List<infobot_core.InfoItem>();
                        list.AddRange(jobs);
                        jobs.Clear();
                        Unwritable = false;
                        foreach (infobot_core.InfoItem item in list)
                        {
                            infobot_core infobot = (infobot_core)item.Channel.RetrieveObject("Infobot");
                            if (infobot != null)
                            {
                                infobot.print(item.Name, item.User, item.Channel, item.Host);
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
            return;
        }
    }
}
