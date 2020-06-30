//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Copyright 2013 - 2014 Petr Bena (benapetr@gmail.com)

using System;
using System.Collections.Generic;
using System.Web;

namespace wmib
{
    public partial class Commands
    {
        public static void InitAdminCommands()
        {
            CommandPool.RegisterCommand(new GenericCommand("add", Commands.AddChannel, false, "join"));
            CommandPool.RegisterCommand(new GenericCommand("commands", Commands.CommandList, true, null, false));
            CommandPool.RegisterCommand(new GenericCommand("configure", Commands.Configure, false, "admin"));
            CommandPool.RegisterCommand(new GenericCommand("channellist", Commands.ChannelList));
            CommandPool.RegisterCommand(new GenericCommand("drop", Commands.Drop, false));
            CommandPool.RegisterCommand(new GenericCommand("help", Commands.Help));
            CommandPool.RegisterCommand(new GenericCommand("join", Commands.AddChannel, false, "join"));
            CommandPool.RegisterCommand(new GenericCommand("language", Commands.Language, true, "admin"));
            CommandPool.RegisterCommand(new GenericCommand("ignore", Commands.Ignore, true, "root"));
            CommandPool.RegisterCommand(new GenericCommand("info", Commands.Info));
            CommandPool.RegisterCommand(new GenericCommand("instance", Commands.Instance, false, "root"));
            CommandPool.RegisterCommand(new GenericCommand("part", Commands.Part, false));
            CommandPool.RegisterCommand(new GenericCommand("reload", Commands.Reload, true, "admin"));
            CommandPool.RegisterCommand(new GenericCommand("restart", Commands.Restart, true, "root"));
            CommandPool.RegisterCommand(new GenericCommand("restrict", Commands.Restrict, true, "root"));
            CommandPool.RegisterCommand(new GenericCommand("reauth", Commands.Reauth, false, "root"));
            CommandPool.RegisterCommand(new GenericCommand("traffic-off", Commands.TrafficOff, true, "root"));
            CommandPool.RegisterCommand(new GenericCommand("traffic-on", Commands.TrafficOn, true, "root"));
            CommandPool.RegisterCommand(new GenericCommand("trustadd", Commands.TrustAdd, false, "trustadd"));
            CommandPool.RegisterCommand(new GenericCommand("trustdel", Commands.TrustDel, false, "trustdel"));
            CommandPool.RegisterCommand(new GenericCommand("trusted", Commands.TrustedList, false));
            CommandPool.RegisterCommand(new GenericCommand("suppress-on", Commands.SuppressOn, false, "suppress"));
            CommandPool.RegisterCommand(new GenericCommand("suppress-off", Commands.SuppressOff, false, "unsuppress"));
            CommandPool.RegisterCommand(new GenericCommand("system-rejoin-all", Commands.SysRejoin, false, "root"));
            CommandPool.RegisterCommand(new GenericCommand("system-rm", Commands.SystemUnload, true, "root"));
            CommandPool.RegisterCommand(new GenericCommand("verbosity--", Commands.VerbosityDown, true, "root"));
            CommandPool.RegisterCommand(new GenericCommand("verbosity++", Commands.VerbosityUp, true, "root"));
            CommandPool.RegisterCommand(new GenericCommand("whoami", Commands.Whoami));
            CommandPool.RegisterCommand(new GenericCommand("channel-info", Commands.ChannelOverview));
            CommandPool.RegisterCommand(new GenericCommand("changepass", Commands.ChangePass, false, "admin"));
        }

        private static void SysRejoin(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
            {
                IRC.DeliverMessage("You need to provide exactly 1 parameter", parameters.SourceChannel);
                return;
            }

            if (wmib.Instance.Instances.ContainsKey(parameters.Parameters))
            {
                if (!wmib.Instance.Instances[parameters.Parameters].ChannelsJoined)
                {
                    IRC.DeliverMessage("Instance " + parameters.Parameters + " is still joining channels", parameters.SourceChannel);
                    return;
                }
                wmib.Instance.Instances[parameters.Parameters].ChannelsJoined = false;
                wmib.Instance.Instances[parameters.Parameters].Join();
                IRC.DeliverMessage("Rejoining all channels on instance " + parameters.Parameters, parameters.SourceChannel);
            }
            else
            {
                IRC.DeliverMessage("Unknown bot: " + parameters.Parameters, parameters.SourceChannel);
            }
        }

        private static void ChangePass(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
            {
                IRC.DeliverMessage("You need to provide exactly 1 parameter", parameters.SourceChannel);
                return;
            }

            parameters.SourceChannel.Password = parameters.Parameters.Trim();
            parameters.SourceChannel.SaveConfig();
            IRC.DeliverMessage("Password updated", parameters.SourceChannel);
        }

        private static void ChannelOverview(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
            {
                IRC.DeliverMessage("You need to provide exactly 1 parameter", parameters.SourceChannel);
                return;
            }

            string channel_name = parameters.Parameters.Trim();
            Channel channel = Core.GetChannel(channel_name);
            if (channel == null)
            {
                IRC.DeliverMessage("Unknown channel: " + channel_name, parameters.SourceChannel);
                return;
            }
            string founder = channel.Extension_GetConfig("generic.owner");
            if (founder != null)
                founder += " (original founder: " + channel.Extension_GetConfig("generic.founder", "N/A") + ")";
            else
                founder = channel.Extension_GetConfig("generic.founder");

            if (founder == null)
                founder = "N/A";

            IRC.DeliverMessage("Channel " + channel.Name + " was joined " + channel.Extension_GetConfig("generic.joindate", "unknown time") + ", requested by " +
                founder, parameters.SourceChannel);
        }

        private static void CommandList(CommandParams parameters)
        {
            string commands = "";
            List<string> list = new List<string>(CommandPool.CommandsList.Keys);
            list.Sort();
            foreach (string command in list)
            {
                commands += command;
                commands += ", ";
            }
            if (commands.EndsWith(", ", System.StringComparison.InvariantCulture))
                commands = commands.Substring(0, commands.Length - 2);

            if (parameters.SourceChannel != null)
            {
                int max_possible_length = IRC.GetMaxMessageLength (parameters.SourceChannel);
                // "I know: "
                max_possible_length -= 8;
                if (commands.Length > max_possible_length)
                {
                    commands = commands.Substring (0, max_possible_length - 3) + "...";
                }
                IRC.DeliverMessage ("I know: " + commands, parameters.SourceChannel);
            } else if (parameters.SourceUser != null)
            {
                IRC.DeliverMessage ("I know: " + commands, parameters.SourceUser);
            }
        }

        private static void Ignore(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
            {
                IRC.DeliverMessage("You need to provide exactly 1 parameter", parameters.SourceChannel);
                return;
            }

            string hostmask = parameters.Parameters.Trim();
            Configuration.IgnoredHostmasks.Add(hostmask);
            Configuration.Save();

            IRC.DeliverMessage("I will ignore all commands of user with hostmask " + hostmask, parameters.SourceChannel.Name);
        }

        private static void Restrict(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
            {
                IRC.DeliverMessage("Please specify restrict action", parameters.SourceChannel);
                return;
            }

            List<string> pm = new List<string>(parameters.Parameters.Trim().Split(' '));
            switch (pm[0])
            {
                case "add":
                    if (pm.Count == 1)
                    {
                        IRC.DeliverMessage("This command requires channel name to be specified", parameters.SourceChannel.Name);
                        break;
                    }

                    string channel_name = pm[1];
                    if (!Core.ValidFile(channel_name) || !channel_name.StartsWith("#"))
                    {
                        IRC.DeliverMessage(messages.Localize("InvalidName", parameters.SourceChannel.Language, new List<string> { channel_name }), parameters.SourceChannel);
                        return;
                    }
                    if (Configuration.RestrictedChannels.ContainsKey(channel_name))
                    {
                        IRC.DeliverMessage("Channel " + channel_name + " is already restricted to join", parameters.SourceChannel.Name);
                        return;
                    }

                    int restriction_level = Security.Roles["root"].Level;
                    if (pm.Count >= 3)
                    {
                        if (Security.Roles.ContainsKey(pm[2]))
                        {
                            restriction_level = Security.Roles[pm[2]].Level;
                        }
                        else
                        {
                            IRC.DeliverMessage("Unknown user role: " + pm[2], parameters.SourceChannel.Name);
                            return;
                        }
                    }

                    foreach (KeyValuePair<string, Instance> instance in wmib.Instance.Instances)
                    {
                        foreach (Channel channel in instance.Value.ChannelList)
                        {
                            if (channel.Name == channel_name)
                            {
                                instance.Value.Network.Transfer("PART " + channel_name);
                                break;
                            }
                        }
                    }
                    lock (Configuration.Channels)
                    {
                        Configuration.Channels.RemoveAll(c => c.Name == channel_name);
                    }
                    lock (Configuration.RestrictedChannels)
                    {
                        Configuration.RestrictedChannels[channel_name] = restriction_level;
                    }
                    Configuration.Save();

                    IRC.DeliverMessage("I will not join " + channel_name + " anymore", parameters.SourceChannel.Name);
                    break;
                default:
                    IRC.DeliverMessage("Unknown restrict action: " + pm[0], parameters.SourceChannel.Name);
                    break;
            }
        }

        private static void Reauth(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
            {
                IRC.DeliverMessage("You need to provide exactly 1 parameter", parameters.SourceChannel);
                return;
            }

            if (wmib.Instance.Instances.ContainsKey(parameters.Parameters))
                wmib.Instance.Instances[parameters.Parameters].Protocol.Authenticate(false);
            else
                IRC.DeliverMessage("Unknown bot: " + parameters.Parameters, parameters.SourceChannel);
        }

        private static void VerbosityDown(CommandParams parameters)
        {
            if (Configuration.System.SelectedVerbosity > 0)
            {
                Configuration.System.SelectedVerbosity--;
            }
            IRC.DeliverMessage("Verbosity: " + Configuration.System.SelectedVerbosity,
                                          parameters.SourceChannel, libirc.Defs.Priority.High);
        }

        private static void VerbosityUp(CommandParams parameters)
        {
            Configuration.System.SelectedVerbosity++;
            IRC.DeliverMessage("Verbosity: " + Configuration.System.SelectedVerbosity,
                                              parameters.SourceChannel, libirc.Defs.Priority.High);
        }

        private static void SystemUnload(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
            {
                IRC.DeliverMessage("You need to provide at least 1 parameters", parameters.SourceChannel);
                return;
            }
            string module = parameters.Parameters;
            Module _m = ExtensionHandler.RetrieveModule(module);
            if (_m == null)
            {
                IRC.DeliverMessage("This module is not currently loaded in core", parameters.SourceChannel, libirc.Defs.Priority.High);
                return;
            }
            _m.Exit();
            IRC.DeliverMessage("Unloaded module " + module, parameters.SourceChannel, libirc.Defs.Priority.High);
        }

        private static void Configure(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
                return;

            if (parameters.Parameters.Contains("=") && !parameters.Parameters.EndsWith("=", System.StringComparison.InvariantCulture))
            {
                string name = parameters.Parameters.Substring(0, parameters.Parameters.IndexOf("=", System.StringComparison.InvariantCulture));
                string value = parameters.Parameters.Substring(parameters.Parameters.IndexOf("=", System.StringComparison.InvariantCulture) + 1);
                bool _temp_a;
                switch (name)
                {
                    case "ignore-unknown":
                        if (bool.TryParse(value, out _temp_a))
                        {
                            parameters.SourceChannel.IgnoreUnknown = _temp_a;
                            IRC.DeliverMessage(messages.Localize("configuresave", parameters.SourceChannel.Language,
                                                                       new List<string> { value, name }), parameters.SourceChannel);
                            parameters.SourceChannel.SaveConfig();
                            return;
                        }
                        IRC.DeliverMessage(messages.Localize("configure-va", parameters.SourceChannel.Language, new List<string> { name, value }), parameters.SourceChannel);
                        return;
                    case "respond-wait":
                        int _temp_b;
                        if (int.TryParse(value, out _temp_b))
                        {
                            if (_temp_b > 1 && _temp_b < 364000)
                            {
                                parameters.SourceChannel.RespondWait = _temp_b;
                                IRC.DeliverMessage(messages.Localize("configuresave", parameters.SourceChannel.Language, new List<string> { value, name }), parameters.SourceChannel);
                                parameters.SourceChannel.SaveConfig();
                                return;
                            }
                        }
                        IRC.DeliverMessage(messages.Localize("configure-va", parameters.SourceChannel.Language, new List<string> { name, value }), parameters.SourceChannel);
                        return;
                    case "respond-message":
                        if (bool.TryParse(value, out _temp_a))
                        {
                            parameters.SourceChannel.RespondMessage = _temp_a;
                            IRC.DeliverMessage(messages.Localize("configuresave", parameters.SourceChannel.Language, new List<string> { value, name }), parameters.SourceChannel);
                            parameters.SourceChannel.SaveConfig();
                            return;
                        }
                        IRC.DeliverMessage(messages.Localize("configure-va", parameters.SourceChannel.Language, new List<string> { name, value }), parameters.SourceChannel);
                        return;
                    case "suppress-warnings":
                        if (bool.TryParse(value, out _temp_a))
                        {
                            parameters.SourceChannel.SuppressWarnings = _temp_a;
                            IRC.DeliverMessage(messages.Localize("configuresave", parameters.SourceChannel.Language, new List<string> { value, name }), parameters.SourceChannel);
                            parameters.SourceChannel.SaveConfig();
                            return;
                        }
                        IRC.DeliverMessage(messages.Localize("configure-va", parameters.SourceChannel.Language, new List<string> { name, value }), parameters.SourceChannel);
                        return;
                }
                bool exist = false;
                foreach (Module curr in ExtensionHandler.ExtensionList)
                {
                    try
                    {
                        if (curr.IsWorking && curr.Hook_SetConfig(parameters.SourceChannel, parameters.User, name, value))
                            exist = true;
                    }
                    catch (Exception fail)
                    {
                        Syslog.Log("Error on Hook_SetConfig module " + curr.Name);
                        Core.HandleException(fail, curr.Name);
                    }
                }
                if (!parameters.SourceChannel.SuppressWarnings && !exist)
                    IRC.DeliverMessage(messages.Localize("configure-wrong", parameters.SourceChannel.Language), parameters.SourceChannel);
                return;
            }
            if (!parameters.Parameters.Contains(" "))
            {
                switch (parameters.Parameters)
                {
                    case "ignore-unknown":
                        IRC.DeliverMessage(messages.Localize("Responses-Conf", parameters.SourceChannel.Language, new List<string> { parameters.Parameters, parameters.SourceChannel.IgnoreUnknown.ToString() }), parameters.SourceChannel);
                        return;
                    case "respond-message":
                        IRC.DeliverMessage(messages.Localize("Responses-Conf", parameters.SourceChannel.Language, new List<string> { parameters.Parameters, parameters.SourceChannel.RespondMessage.ToString() }), parameters.SourceChannel);
                        return;
                    case "suppress-warnings":
                        IRC.DeliverMessage(messages.Localize("Responses-Conf", parameters.SourceChannel.Language, new List<string> { parameters.Parameters, parameters.SourceChannel.SuppressWarnings.ToString() }), parameters.SourceChannel);
                        return;
                }
                bool exist = false;
                foreach (Module curr in ExtensionHandler.ExtensionList)
                {
                    try
                    {
                        if (curr.IsWorking && curr.Hook_GetConfig(parameters.SourceChannel, parameters.User, parameters.Parameters))
                            exist = true;
                    }
                    catch (Exception fail)
                    {
                        Syslog.Log("Error on Hook_GetConfig module " + curr.Name);
                        Core.HandleException(fail);
                    }
                }
                if (exist)
                    return;
            }
            if (!parameters.SourceChannel.SuppressWarnings)
                IRC.DeliverMessage(messages.Localize("configure-wrong", parameters.SourceChannel.Language), parameters.SourceChannel);
        }

        private static void ChannelList(CommandParams parameters)
        {
            IRC.DeliverMessage(messages.Localize("Responses-List", parameters.SourceChannel.Language, new List<string> { Configuration.Channels.Count.ToString() }),
                                parameters.SourceChannel);
        }

        private static void TrafficOff(CommandParams parameters)
        {
            Configuration.Network.Logging = false;
            IRC.DeliverMessage("Logging stopped", parameters.SourceChannel);
        }

        private static void TrafficOn(CommandParams parameters)
        {
            Configuration.Network.Logging = true;
            IRC.DeliverMessage("Logging started", parameters.SourceChannel);
        }

        private static void Instance(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters) || !parameters.Parameters.Contains(" "))
            {
                IRC.DeliverMessage("This command needs 2 parameters", parameters.SourceChannel);
                return;
            }
            string channel = parameters.Parameters.Substring(parameters.Parameters.IndexOf(" ") + 1);
            string instance = parameters.Parameters.Substring(0, parameters.Parameters.IndexOf(" "));
            Channel ch = Core.GetChannel(channel);
            if (ch == null)
            {
                IRC.DeliverMessage("This channel I never heard of :'(", parameters.SourceChannel);
                return;
            }

            if (!wmib.Instance.Instances.ContainsKey(instance))
            {
                IRC.DeliverMessage("This instance I never heard of :'(", parameters.SourceChannel);
                return;
            }
            Instance _instance = wmib.Instance.Instances[instance];
            if (_instance == ch.PrimaryInstance)
            {
                IRC.DeliverMessage("This channel is already in this instance", parameters.SourceChannel);
                return;
            }
            ch.PrimaryInstance.Network.Transfer("PART " + ch.Name + " :Switching instance");
            ch.PrimaryInstance = _instance;
            ch.PrimaryInstance.Network.Transfer("JOIN " + ch.Name);
            ch.DefaultInstance = ch.PrimaryInstance.Nick;
            ch.SaveConfig();
            IRC.DeliverMessage("Changed default instance of " + channel + " to " + instance, parameters.SourceChannel);
        }

        private static void Whoami(CommandParams parameters)
        {
            SystemUser current = parameters.SourceChannel.SystemUsers.GetUser(parameters.User);
            if (current.Role == "null")
            {
                IRC.DeliverMessage(messages.Localize("Unknown", parameters.SourceChannel.Language), parameters.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("usr1", parameters.SourceChannel.Language, new List<string> { current.Role, current.Name }), parameters.SourceChannel);
        }

        private static void Restart(CommandParams parameters)
        {
            IRC.DeliverMessage("System is shutting down, requested by " + parameters.User.Nick + " from " + parameters.SourceChannel.Name, Configuration.System.DebugChan, libirc.Defs.Priority.High);
            Syslog.Log("System is shutting down, requested by " + parameters.User.Nick + " from " + parameters.SourceChannel.Name);
            Core.Kill();
        }

        private static void SuppressOn(CommandParams parameters)
        {
            if (parameters.SourceChannel.Suppress)
            {
                //Message("Channel had already quiet mode disabled", chan.name);
                return;
            }
            IRC.DeliverMessage(messages.Localize("SilenceBegin", parameters.SourceChannel.Language), parameters.SourceChannel);
            parameters.SourceChannel.Suppress = true;
            parameters.SourceChannel.SaveConfig();
        }

        private static void SuppressOff(CommandParams parameters)
        {
            if (!parameters.SourceChannel.Suppress)
            {
                IRC.DeliverMessage(messages.Localize("Silence1", parameters.SourceChannel.Language), parameters.SourceChannel);
                return;
            }
            parameters.SourceChannel.Suppress = false;
            IRC.DeliverMessage(messages.Localize("Silence2", parameters.SourceChannel.Language), parameters.SourceChannel);
            parameters.SourceChannel.SaveConfig();
            Configuration.Save();
        }

        private static void Info(CommandParams parameters)
        {
            IRC.DeliverMessage(Configuration.WebPages.WebpageURL + Configuration.Paths.DumpDir
                                + "/" + HttpUtility.UrlEncode(parameters.SourceChannel.Name) + ".htm", parameters.SourceChannel);
        }

        private static void Reload(CommandParams parameters)
        {
            parameters.SourceChannel.LoadConfig();
            SystemHooks.IrcReloadChannelConf(parameters.SourceChannel);
            IRC.DeliverMessage(messages.Localize("Config", parameters.SourceChannel.Language), parameters.SourceChannel);
            return;
        }

        private static void Help(CommandParams parameters)
        {
            if (!String.IsNullOrEmpty(parameters.Parameters))
            {
                Core.ShowHelp(parameters.Parameters, parameters.SourceChannel);
                return;
            }
            IRC.DeliverMessage("I am running http://meta.wikimedia.org/wiki/WM-Bot version "
                                          + Configuration.System.Version + " my source code is licensed "
                                          + "under GPL and located at https://github.com/benapetr/wikimedia-bot "
                                          + "I will be very happy if you fix my bugs or implement new features",
                                          parameters.SourceChannel);
        }

        private static void Language(CommandParams parameters)
        {
            if (!String.IsNullOrEmpty(parameters.Parameters))
            {
                if (messages.Exists(parameters.Parameters))
                {
                    parameters.SourceChannel.Language = parameters.Parameters;
                    IRC.DeliverMessage(messages.Localize("Language", parameters.SourceChannel.Language), parameters.SourceChannel);
                    parameters.SourceChannel.SaveConfig();
                    return;
                }
                if (!parameters.SourceChannel.SuppressWarnings)
                    IRC.DeliverMessage(messages.Localize("InvalidCode", parameters.SourceChannel.Language), parameters.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("LanguageInfo", parameters.SourceChannel.Language), parameters.SourceChannel);
        }

        private static void Drop(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
                return;
            string channel = parameters.Parameters.Trim();
            if (!string.IsNullOrEmpty(channel))
            {
                Channel _Channel = Core.GetChannel(channel);
                if (_Channel == null)
                {
                    IRC.DeliverMessage(messages.Localize("UnknownChan", parameters.SourceChannel.Language), parameters.SourceChannel,
                                                  libirc.Defs.Priority.Low);
                    return;
                }
                PartChannel(_Channel, parameters.User.Nick, parameters.User.Host, Configuration.System.CommandPrefix
                                 + "drop", parameters.SourceChannel.Name);
                return;
            }
            IRC.DeliverMessage(messages.Localize("Responses-PartFail", parameters.SourceChannel.Language), parameters.SourceChannel,
                                          libirc.Defs.Priority.Low);
        }

        private static void Part(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
                return;
            string channel = parameters.Parameters.Trim();
            if (!string.IsNullOrEmpty(channel))
            {
                Channel _Channel = Core.GetChannel(channel);
                if (_Channel == null)
                {
                    IRC.DeliverMessage(messages.Localize("UnknownChan", parameters.SourceChannel.Language), parameters.SourceChannel,
                                                  libirc.Defs.Priority.Low);
                    return;
                }
                PartChannel(_Channel, parameters.User.Nick, parameters.User.Host, Configuration.System.CommandPrefix
                                 + "part", parameters.SourceChannel.Name);
                return;
            }
            IRC.DeliverMessage(messages.Localize("Responses-PartFail", parameters.SourceChannel.Language), parameters.SourceChannel,
                                          libirc.Defs.Priority.Low);
        }
    }
}
