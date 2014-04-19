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
using System.Web;

namespace wmib
{
    public partial class Commands
    {
        /// <summary>
        /// Display admin command
        /// </summary>
        /// <param name="chan">Channel</param>
        /// <param name="user">User name</param>
        /// <param name="host">Host</param>
        /// <param name="message">Message</param>
        public static void ParseAdmin(Channel chan, string user, string host, string message)
        {
            libirc.UserInfo invoker = new libirc.UserInfo(user, "", host);
            if (message == Configuration.System.CommandPrefix + "reload")
            {
                if (chan.SystemUsers.IsApproved(invoker, "admin"))
                {
                    chan.LoadConfig();
                    SystemHooks.IrcReloadChannelConf(chan);
                    Core.irc.Queue.DeliverMessage(messages.Localize("Config", chan.Language), chan);
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan);
                }
                return;
            }
            if (message == Configuration.System.CommandPrefix + "flushcaches")
            {
                if (chan.SystemUsers.IsApproved(invoker, "flushcache"))
                {
                    Core.irc.RestartIRCMessageDelivery();
                    chan.PrimaryInstance.irc.Message(messages.Localize("MessageQueueWasReloaded", chan.Language), chan);
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan,
                                                  libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "info")
            {
                Core.irc.Queue.DeliverMessage(Configuration.WebPages.WebpageURL + Configuration.Paths.DumpDir
                                              + "/" + HttpUtility.UrlEncode(chan.Name) + ".htm", chan);
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "part "))
            {
                string channel = message.Substring(6);
                if (channel != "")
                {
                    Channel _Channel = Core.GetChannel(channel);
                    if (_Channel == null)
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("UnknownChan", chan.Language), chan,
                                                      libirc.Defs.Priority.Low);
                        return;
                    }
                    PartChannel(_Channel, invoker.Nick, invoker.Host, Configuration.System.CommandPrefix
                                     + "part", chan.Name);
                    return;
                }
                Core.irc.Queue.DeliverMessage(messages.Localize("Responses-PartFail", chan.Language), chan,
                                              libirc.Defs.Priority.Low);
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "drop "))
            {
                string channel = message.Substring(6);
                if (channel != "")
                {
                    Channel _Channel = Core.GetChannel(channel);
                    if (_Channel == null)
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("UnknownChan", chan.Language), chan,
                                                      libirc.Defs.Priority.Low);
                        return;
                    }
                    PartChannel(_Channel, invoker.Nick, invoker.Host, Configuration.System.CommandPrefix
                                     + "drop", chan.Name);
                    return;
                }
                Core.irc.Queue.DeliverMessage(messages.Localize("Responses-PartFail", chan.Language), chan,
                                              libirc.Defs.Priority.Low);
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "language"))
            {
                if (chan.SystemUsers.IsApproved(invoker, "admin"))
                {
                    string parameter = "";
                    if (message.Contains(" "))
                    {
                        parameter = message.Substring(message.IndexOf(" ") + 1).ToLower();
                    }
                    if (parameter != "")
                    {
                        if (messages.Exists(parameter))
                        {
                            chan.Language = parameter;
                            Core.irc.Queue.DeliverMessage(messages.Localize("Language", chan.Language), chan);
                            chan.SaveConfig();
                            return;
                        }
                        if (!chan.SuppressWarnings)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("InvalidCode", chan.Language), chan);
                        }
                        return;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("LanguageInfo", chan.Language), chan);
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan,
                                                  libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "help"))
            {
                string parameter = "";
                if (message.Contains(" "))
                {
                    parameter = message.Substring(message.IndexOf(" ") + 1);
                }
                if (parameter != "")
                {
                    Core.ShowHelp(parameter, chan);
                    return;
                }
                Core.irc.Queue.DeliverMessage("I am running http://meta.wikimedia.org/wiki/WM-Bot version "
                                              + Configuration.System.Version + " my source code is licensed "
                                              + "under GPL and located at https://github.com/benapetr/wikimedia-bot "
                                              + "I will be very happy if you fix my bugs or implement new features",
                                              chan);
                return;
            }

            if (message == Configuration.System.CommandPrefix + "suppress-off")
            {
                if (chan.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (!chan.Suppress)
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("Silence1", chan.Language), chan);
                        return;
                    }
                    chan.Suppress = false;
                    Core.irc.Queue.DeliverMessage(messages.Localize("Silence2", chan.Language), chan);
                    chan.SaveConfig();
                    Configuration.Save();
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "suppress-on")
            {
                if (chan.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (chan.Suppress)
                    {
                        //Message("Channel had already quiet mode disabled", chan.name);
                        return;
                    }
                    chan.PrimaryInstance.irc.Message(messages.Localize("SilenceBegin", chan.Language), chan);
                    chan.Suppress = true;
                    chan.SaveConfig();
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "whoami")
            {
                SystemUser current = chan.SystemUsers.GetUser(user + "!@" + host);
                if (current.Role == "null")
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("Unknown", chan.Language), chan);
                    return;
                }
                Core.irc.Queue.DeliverMessage(messages.Localize("usr1", chan.Language, new List<string> { current.Role, current.Name }), chan);
                return;
            }

            if (message == Configuration.System.CommandPrefix + "system-relog")
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    Core.irc.Authenticate();
                    return;
                }
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "instance "))
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    message = message.Substring(".instance ".Length);
                    if (!message.Contains(" "))
                    {
                        Core.irc.Queue.DeliverMessage("This command need 2 parameters", chan);
                        return;
                    }
                    string channel = message.Substring(message.IndexOf(" ") + 1);
                    string instance = message.Substring(0, message.IndexOf(" "));
                    Channel ch = Core.GetChannel(channel);
                    if (ch == null)
                    {
                        Core.irc.Queue.DeliverMessage("This channel I never heard of :'(", chan);
                        return;
                    }

                    Instance _instance;

                    lock (WmIrcProtocol.Instances)
                    {
                        if (!WmIrcProtocol.Instances.ContainsKey(instance))
                        {
                            Core.irc.Queue.DeliverMessage("This instance I never heard of :'(", chan);
                            return;
                        }
                        _instance = WmIrcProtocol.Instances[instance];
                    }

                    if (_instance == ch.PrimaryInstance)
                    {
                        Core.irc.Queue.DeliverMessage("This channel is already in this instance", chan);
                        return;
                    }

                    ch.PrimaryInstance.irc.SendData("PART " + ch.Name + " :Switching instance");
                    ch.PrimaryInstance = _instance;
                    ch.PrimaryInstance.irc.SendData("JOIN " + ch.Name);
                    ch.DefaultInstance = ch.PrimaryInstance.Nick;
                    ch.SaveConfig();

                    chan.PrimaryInstance.irc.Queue.DeliverMessage("Changed default instance of " + channel + " to " + instance, chan);
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan, libirc.Defs.Priority.Low);
                }
            }

            if (message == Configuration.System.CommandPrefix + "traffic-off")
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    Configuration.Network.Logging = false;
                    Core.irc.Queue.DeliverMessage("Logging stopped", chan);
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan, libirc.Defs.Priority.Low);
                }
            }

            if (message == Configuration.System.CommandPrefix + "traffic-on")
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    Configuration.Network.Logging = true;
                    Core.irc.Queue.DeliverMessage("Logging traf", chan.Name);
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan, libirc.Defs.Priority.Low);
                }
            }

            if (message == Configuration.System.CommandPrefix + "restart")
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    Core.irc.Message("System is shutting down, requested by " + invoker.Nick + " from " + chan.Name, Configuration.System.DebugChan);
                    Syslog.Log("System is shutting down, requested by " + invoker.Nick + " from " + chan.Name);
                    Core.Kill();
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan.Name, libirc.Defs.Priority.Low);
                }
            }

            if (message == Configuration.System.CommandPrefix + "channellist")
            {
                Core.irc.Queue.DeliverMessage(messages.Localize("Responses-List", chan.Language, new List<string>
                                                        { Configuration.Channels.Count.ToString() }), chan);
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "configure "))
            {
                if (chan.SystemUsers.IsApproved(invoker, "admin"))
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
                                    Core.irc.Queue.DeliverMessage(messages.Localize("configuresave", chan.Language,
                                                                               new List<string> { value, name }), chan);
                                    chan.SaveConfig();
                                    return;
                                }
                                Core.irc.Queue.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string>
                                                                       { name, value }), chan);
                                return;
                            case "respond-wait":
                                int _temp_b;
                                if (int.TryParse(value, out _temp_b))
                                {
                                    if (_temp_b > 1 && _temp_b < 364000)
                                    {
                                        chan.RespondWait = _temp_b;
                                        Core.irc.Queue.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string>
                                                                                   { value, name }), chan);
                                        chan.SaveConfig();
                                        return;
                                    }
                                }
                                Core.irc.Queue.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string>
                                                                       { name, value }), chan);
                                return;
                            case "respond-message":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.RespondMessage = _temp_a;
                                    Core.irc.Queue.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string>
                                                                               { value, name }), chan);
                                    chan.SaveConfig();
                                    return;
                                }
                                Core.irc.Queue.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string>
                                                                       { name, value }), chan);
                                return;
                            case "suppress-warnings":
                                if (bool.TryParse(value, out _temp_a))
                                {
                                    chan.SuppressWarnings = _temp_a;
                                    Core.irc.Queue.DeliverMessage(messages.Localize("configuresave", chan.Language, new List<string>
                                                                               { value, name }), chan);
                                    chan.SaveConfig();
                                    return;
                                }
                                Core.irc.Queue.DeliverMessage(messages.Localize("configure-va", chan.Language, new List<string>
                                                                       { name, value }), chan);
                                return;
                        }
                        bool exist = false;
                        lock (ExtensionHandler.Extensions)
                        {
                            foreach (Module curr in ExtensionHandler.Extensions)
                            {
                                try
                                {
                                    if (curr.IsWorking)
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
                                    Core.HandleException(fail, curr.Name);
                                }
                            }
                        }
                        if (!chan.SuppressWarnings && !exist)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("configure-wrong", chan.Language), chan);
                        }
                        return;
                    }
                    if (!text.Contains(" "))
                    {
                        switch (text)
                        {
                            case "ignore-unknown":
                                Core.irc.Queue.DeliverMessage(messages.Localize("Responses-Conf", chan.Language, new List<string>
                                                                       { text, chan.IgnoreUnknown.ToString() } ), chan);
                                return;
                            case "respond-message":
                                Core.irc.Queue.DeliverMessage(messages.Localize("Responses-Conf", chan.Language, new List<string>
                                                                       { text, chan.RespondMessage.ToString() }), chan);
                                return;
                            case "suppress-warnings":
                                Core.irc.Queue.DeliverMessage(messages.Localize("Responses-Conf", chan.Language, new List<string>
                                                                       { text, chan.SuppressWarnings.ToString() } ), chan);
                                return;
                        }
                        bool exist = false;
                        lock (ExtensionHandler.Extensions)
                        {
                            foreach (Module curr in ExtensionHandler.Extensions)
                            {
                                try
                                {
                                    if (curr.IsWorking)
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
                                    Core.HandleException(fail);
                                }
                            }
                        }
                        if (exist)
                        {
                            return;
                        }
                    }
                    if (!chan.SuppressWarnings)
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("configure-wrong", chan.Language), chan);
                    }
                    return;
                }
                if (!chan.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", chan.Language), chan, libirc.Defs.Priority.Low);
                }
                return;
            }

#if FALSE
            if (message.StartsWith(Configuration.System.CommandPrefix + "system-lm "))
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    string module = message.Substring("@system-lm ".Length);
                    if (module.EndsWith(".bin"))
                    {
                        Module _m = ExtensionHandler.RetrieveModule(module);
                        if (_m != null)
                        {
                            Core.irc.Queue.DeliverMessage("This module was already loaded and you can't load one module twice,"
                                                          +" module will be reloaded now", chan, IRC.priority.high);
                            _m.Exit();
                        }
                        if (module.EndsWith(".bin"))
                        {
                            module = "modules" + Path.DirectorySeparatorChar + module;
                            if (File.Exists(module))
                            {
                                if (ExtensionHandler.LoadAllModulesInLibrary(module))
                                {
                                    Core.irc.Queue.DeliverMessage("Loaded module " + module, chan, IRC.priority.high);
                                    return;
                                }
                                Core.irc.Queue.DeliverMessage("Unable to load module " + module, chan, IRC.priority.high);
                                return;
                            }
                            Core.irc.Queue.DeliverMessage("File not found " + module, chan, IRC.priority.high);
                            return;
                        }

                        Core.irc.Queue.DeliverMessage("Loaded module " + module, chan, IRC.priority.high);
                        return;
                    }
                    Core.irc.Queue.DeliverMessage("This module is not currently loaded in core", chan, IRC.priority.high);
                    return;

                }
            }
#endif

            if (message == Configuration.System.CommandPrefix + "verbosity--")
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    if (Configuration.System.SelectedVerbosity > 0)
                    {
                        Configuration.System.SelectedVerbosity--;
                    }
                    Core.irc.Queue.DeliverMessage("Verbosity: " + Configuration.System.SelectedVerbosity, 
                                                  chan, libirc.Defs.Priority.High);
                }
            }

            if (message == Configuration.System.CommandPrefix + "verbosity++")
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    Configuration.System.SelectedVerbosity++;
                    Core.irc.Queue.DeliverMessage("Verbosity: " + Configuration.System.SelectedVerbosity,
                                                  chan, libirc.Defs.Priority.High);
                }
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "system-rm "))
            {
                if (chan.SystemUsers.IsApproved(invoker, "root"))
                {
                    string module = message.Substring("@system-lm ".Length);
                    Module _m = ExtensionHandler.RetrieveModule(module);
                    if (_m == null)
                    {
                        Core.irc.Queue.DeliverMessage("This module is not currently loaded in core", chan, libirc.Defs.Priority.High);
                        return;
                    }
                    _m.Exit();
                    Core.irc.Queue.DeliverMessage("Unloaded module " + module, chan, libirc.Defs.Priority.High);
                }
            }

            if (message == Configuration.System.CommandPrefix + "commands")
            {
                Core.irc.Queue.DeliverMessage("Commands: there is too many commands to display on one line,"
                                              + " see http://meta.wikimedia.org/wiki/wm-bot for a list of"
                                              + " commands and help", chan);
            }
        }
    }
}
