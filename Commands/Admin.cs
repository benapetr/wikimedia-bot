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

namespace wmib
{
    public partial class core
    {
        /// <summary>
        /// Display admin command
        /// </summary>
        /// <param name="chan">Channel</param>
        /// <param name="user">User name</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        public static void ParseAdmin(config.channel chan, string user, string host, string message)
        {
            User invoker = new User(user, host, "");
            if (message == config.CommandPrefix + "reload")
            {
                if (chan.Users.IsApproved(invoker, "admin"))
                {
                    chan.LoadConfig();
					SystemHooks.IrcReloadChannelConf(chan);
                    irc._SlowQueue.DeliverMessage(messages.get("Config", chan.Language), chan);
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan);
                }
                return;
            }
            if (message == config.CommandPrefix + "refresh")
            {
                if (chan.Users.IsApproved(invoker, "flushcache"))
                {
                    irc.RestartIRCMessageDelivery();
                    chan.instance.irc.Message(messages.get("MessageQueueWasReloaded", chan.Language), chan.Name);
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "info")
            {
                irc._SlowQueue.DeliverMessage(config.WebpageURL + config.DumpDir + "/" + System.Web.HttpUtility.UrlEncode(chan.Name) + ".htm", chan);
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "part "))
            {
                string channel = message.Substring(6);
                if (channel != "")
                {
                    config.channel Channel = core.getChannel(channel);
                    if (Channel == null)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("UnknownChan", chan.Language), chan, IRC.priority.low);
                        return;
                    }
                    core.partChannel(Channel, invoker.Nick, invoker.Host, config.CommandPrefix + "part", chan.Name);
                    return;
                }
                irc._SlowQueue.DeliverMessage(messages.get("Responses-PartFail", chan.Language), chan, IRC.priority.low);
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "drop "))
            {
                string channel = message.Substring(6);
                if (channel != "")
                {
                    config.channel Channel = core.getChannel(channel);
                    if (Channel == null)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("UnknownChan", chan.Language), chan, IRC.priority.low);
                        return;
                    }
                    core.partChannel(Channel, invoker.Nick, invoker.Host, config.CommandPrefix + "drop", chan.Name);
                    return;
                }
                irc._SlowQueue.DeliverMessage(messages.get("Responses-PartFail", chan.Language), chan, IRC.priority.low);
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "language"))
            {
                if (chan.Users.IsApproved(invoker, "admin"))
                {
                    string parameter = "";
                    if (message.Contains(" "))
                    {
                        parameter = message.Substring(message.IndexOf(" ") + 1).ToLower();
                    }
                    if (parameter != "")
                    {
                        if (messages.exist(parameter))
                        {
                            chan.Language = parameter;
                            irc._SlowQueue.DeliverMessage(messages.get("Language", chan.Language), chan);
                            chan.SaveConfig();
                            return;
                        }
                        if (!chan.suppress_warnings)
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("InvalidCode", chan.Language), chan);
                        }
                        return;
                    }
                    irc._SlowQueue.DeliverMessage(messages.get("LanguageInfo", chan.Language), chan);
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "help"))
            {
                string parameter = "";
                if (message.Contains(" "))
                {
                    parameter = message.Substring(message.IndexOf(" ") + 1);
                }
                if (parameter != "")
                {
                    ShowHelp(parameter, chan);
                    return;
                }
                irc._SlowQueue.DeliverMessage("I am running http://meta.wikimedia.org/wiki/WM-Bot version " + config.Version + " my source code is licensed under GPL and located at https://github.com/benapetr/wikimedia-bot I will be very happy if you fix my bugs or implement new features", chan);
                return;
            }

            if (message == config.CommandPrefix + "suppress-off")
            {
                if (chan.Users.IsApproved(invoker, "admin"))
                {
                    if (!chan.suppress)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Silence1", chan.Language), chan);
                        return;
                    }
                    chan.suppress = false;
                    irc._SlowQueue.DeliverMessage(messages.get("Silence2", chan.Language), chan);
                    chan.SaveConfig();
                    config.Save();
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "suppress-on")
            {
                if (chan.Users.IsApproved(invoker, "admin"))
                {
                    if (chan.suppress)
                    {
                        //Message("Channel had already quiet mode disabled", chan.name);
                        return;
                    }
                    chan.instance.irc.Message(messages.get("SilenceBegin", chan.Language), chan.Name);
                    chan.suppress = true;
                    chan.SaveConfig();
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "whoami")
            {
                SystemUser current = chan.Users.getUser(user + "!@" + host);
                if (current.level == "null")
                {
                    irc._SlowQueue.DeliverMessage(messages.get("Unknown", chan.Language), chan);
                    return;
                }
                irc._SlowQueue.DeliverMessage(messages.get("usr1", chan.Language, new List<string> { current.level, current.name }), chan);
                return;
            }

            if (message == config.CommandPrefix + "system-relog")
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    core.irc.Authenticate();
                    return;
                }
            }

            if (message.StartsWith(config.CommandPrefix + "instance "))
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    string channel;
                    string instance;
                    message = message.Substring(".instance ".Length);
                    if (!message.Contains(" "))
                    {
                        irc._SlowQueue.DeliverMessage("This command need 2 parameters", chan);
                        return;
                    }
                    channel = message.Substring(message.IndexOf(" ") + 1);
                    instance = message.Substring(0, message.IndexOf(" "));
                    config.channel ch = core.getChannel(channel);
                    if (ch == null)
                    {
                        irc._SlowQueue.DeliverMessage("This channel I never heard of :'(", chan);
                        return;
                    }

                    Instance _instance = null;

                    lock (core.Instances)
                    {
                        if (!core.Instances.ContainsKey(instance))
                        {
                            irc._SlowQueue.DeliverMessage("This instance I never heard of :'(", chan);
                            return;
                        }
                        _instance = core.Instances[instance];
                    }

                    if (_instance == ch.instance)
                    {
                        irc._SlowQueue.DeliverMessage("This channel is already in this instance", chan);
                        return;
                    }

                    ch.instance.irc.SendData("PART " + ch.Name + " :Switching instance");
                    ch.instance = _instance;
                    ch.instance.irc.SendData("JOIN " + ch.Name);
                    ch.DefaultInstance = ch.instance.Nick;
                    ch.SaveConfig();

                    chan.instance.irc._SlowQueue.DeliverMessage("Changed default instance of " + channel + " to " + instance, chan);
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan, IRC.priority.low);
                }
            }

            if (message == config.CommandPrefix + "traffic-off")
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    config.Logging = false;
                    irc._SlowQueue.DeliverMessage("Logging stopped", chan);
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan, IRC.priority.low);
                }
            }

            if (message == config.CommandPrefix + "traffic-on")
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    config.Logging = true;
                    irc._SlowQueue.DeliverMessage("Logging traf", chan.Name);
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan, IRC.priority.low);
                }
            }

            if (message == config.CommandPrefix + "restart")
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    irc.Message("System is shutting down, requested by " + invoker.Nick + " from " + chan.Name, config.DebugChan);
                    Syslog.Log("System is shutting down, requested by " + invoker.Nick + " from " + chan.Name);
                    Kill();
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan.Name, IRC.priority.low);
                }
            }

            if (message == config.CommandPrefix + "channellist")
            {
                irc._SlowQueue.DeliverMessage(messages.get("Responses-List", chan.Language, new List<string> { config.channels.Count.ToString() }), chan);
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "configure "))
            {
                if (chan.Users.IsApproved(invoker, "admin"))
                {
                    string text = message.Substring("@configure ".Length);
                    if (string.IsNullOrEmpty(text))
                    {
                        return;
                    }
                    if (text.Contains("=") && !text.EndsWith("="))
                    {
                        string name = text.Substring(0, text.IndexOf("="));
                        string value = text.Substring(text.IndexOf("=") + 1);
                        bool _temp_a;
                        switch (name)
                        {
                            case "ignore-unknown":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.IgnoreUnknown = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan);
                                return;
                            case "respond-wait":
                                int _temp_b;
                                if (int.TryParse(value, out _temp_b))
                                {
                                    if (_temp_b > 1 && _temp_b < 364000)
                                    {
                                        chan.respond_wait = _temp_b;
                                        irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan);
                                        chan.SaveConfig();
                                        return;
                                    }
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan);
                                return;
                            case "respond-message":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.respond_message = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan);
                                return;
                            case "suppress-warnings":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.suppress_warnings = _temp_a;
                                    irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, name }), chan);
                                    chan.SaveConfig();
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { name, value }), chan);
                                return;
                        }
                        bool exist = false;
                        lock (Module.module)
                        {
                            foreach (Module curr in Module.module)
                            {
                                try
                                {
                                    if (curr.working)
                                    {
                                        if (curr.Hook_SetConfig(chan, invoker, name, value))
                                        {
                                            exist = true;
                                        }
                                    }
                                }
                                catch (Exception fail)
                                {
                                    Syslog.Log("Error on Hook_SetConfig module " + curr.Name);
                                    core.handleException(fail);
                                }
                            }
                        }
                        if (!chan.suppress_warnings && !exist)
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("configure-wrong", chan.Language), chan);
                        }
                        return;
                    }
                    if (!text.Contains(" "))
                    {
                        switch (text)
                        {
                            case "ignore-unknown":
                                irc._SlowQueue.DeliverMessage(messages.get("Responses-Conf", chan.Language, new List<string> { text, chan.IgnoreUnknown.ToString() } ), chan);
                                return;
                            case "respond-message":
                                irc._SlowQueue.DeliverMessage(messages.get("Responses-Conf", chan.Language, new List<string> { text, chan.respond_message.ToString() }), chan);
                                return;
                            case "suppress-warnings":
                                irc._SlowQueue.DeliverMessage(messages.get("Responses-Conf", chan.Language, new List<string> { text, chan.suppress_warnings.ToString() } ), chan);
                                return;
                        }
                        bool exist = false;
                        lock (Module.module)
                        {
                            foreach (Module curr in Module.module)
                            {
                                try
                                {
                                    if (curr.working)
                                    {
                                        if (curr.Hook_GetConfig(chan, invoker, text))
                                        {
                                            exist = true;
                                        }
                                    }
                                }
                                catch (Exception fail)
                                {
                                    Syslog.Log("Error on Hook_GetConfig module " + curr.Name);
                                    core.handleException(fail);
                                }
                            }
                        }
                        if (exist)
                        {
                            return;
                        }
                    }
                    if (!chan.suppress_warnings)
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("configure-wrong", chan.Language), chan);
                    }
                    return;
                }
                if (!chan.suppress_warnings)
                {
                    irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", chan.Language), chan, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "system-lm "))
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    string module = message.Substring("@system-lm ".Length);
                    if (module.EndsWith(".bin"))
                    {
                        Module _m = getModule(module);
                        if (_m != null)
                        {
                            irc._SlowQueue.DeliverMessage("This module was already loaded and you can't load one module twice, module will be reloaded now", chan, IRC.priority.high);
                            _m.Exit();
                        }
                        if (module.EndsWith(".bin"))
                        {
                            module = "modules" + Path.DirectorySeparatorChar + module;
                            if (File.Exists(module))
                            {
                                if (LoadMod(module))
                                {
                                    irc._SlowQueue.DeliverMessage("Loaded module " + module, chan, IRC.priority.high);
                                    return;
                                }
                                irc._SlowQueue.DeliverMessage("Unable to load module " + module, chan, IRC.priority.high);
                                return;
                            }
                            irc._SlowQueue.DeliverMessage("File not found " + module, chan, IRC.priority.high);
                            return;
                        }

                        irc._SlowQueue.DeliverMessage("Loaded module " + module, chan, IRC.priority.high);
                        return;
                    }
                    irc._SlowQueue.DeliverMessage("This module is not currently loaded in core", chan, IRC.priority.high);
                    return;

                }
            }

            if (message == config.CommandPrefix + "verbosity--")
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    if (config.SelectedVerbosity > 0)
                    {
                        config.SelectedVerbosity--;
                    }
                    irc._SlowQueue.DeliverMessage("Verbosity: " + config.SelectedVerbosity.ToString(), chan, IRC.priority.high);
                }
            }

            if (message == config.CommandPrefix + "verbosity++")
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    config.SelectedVerbosity++;
                    irc._SlowQueue.DeliverMessage("Verbosity: " + config.SelectedVerbosity.ToString(), chan, IRC.priority.high);
                }
            }

            if (message.StartsWith(config.CommandPrefix + "system-rm "))
            {
                if (chan.Users.IsApproved(invoker, "root"))
                {
                    string module = message.Substring("@system-lm ".Length);
                    Module _m = getModule(module);
                    if (_m == null)
                    {
                        irc._SlowQueue.DeliverMessage("This module is not currently loaded in core", chan, IRC.priority.high);
                        return;
                    }
                    _m.Exit();
                    irc._SlowQueue.DeliverMessage("Unloaded module " + module, chan, IRC.priority.high);
                }
            }

            if (message.StartsWith(config.CommandPrefix + "join "))
            {
                if (chan.Users.IsApproved(invoker, "reconnect"))
                {
                    config.channel channel = core.getChannel(message.Substring("@join ".Length));
                    irc.Join(channel);
                }
            }

            if (message == config.CommandPrefix + "commands")
            {
                irc._SlowQueue.DeliverMessage("Commands: there is too many commands to display on one line, see http://meta.wikimedia.org/wiki/wm-bot for a list of commands and help", chan);
            }
        }
    }
}
