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

namespace wmib
{
    /// <summary>
    /// Processor
    /// </summary>
    public class ProcessorIRC
    {
        private string text;
        /// <summary>
        /// Instance this processor is attached to
        /// </summary>
        public Instance instance;

        private void Ping()
        {
            return;
        }

        private bool Info(string command, string parameters, string value)
        {
            return true;
        }

        private bool ChannelInfo(string[] code, string command, string source, string parameters, string _value)
        {
            if (code.Length > 3)
            {

            }
            return false;
        }

        private bool ChannelTopic(string[] code, string command, string source, string parameters, string value)
        {

            return false;
        }

        private bool FinishChan(string[] code)
        {
            if (code.Length > 2)
            {
                if (code[3] == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
                {
                    return true;
                }
                Channel channel = Core.GetChannel(code[3]);
                if (channel != null)
                {
                    if (channel.UserList.Count == 0)
                    {
                        Syslog.WarningLog("Parsed no data records for " + channel.Name + " which means I am likely not in a channel, trying to rejoin it");
                        channel.HasFreshUserList = false;
                        channel.PrimaryInstance.irc.Join(channel);
                        return false;

                    } else
                    {
                        Syslog.Log("Finished parsing for " + channel.Name + " parsed totaly: " + channel.UserList.Count.ToString());
                    }
                    if (Configuration.System.SelectedVerbosity > 8)
                    {
                        string list = "";
                        lock (channel.UserList)
                        {
                            foreach (User u in channel.UserList)
                            {
                                list = list + u.Nick + ", ";
                            }
                        }
                        Syslog.DebugLog("Parsed: " + list, 8);
                    }
                    channel.HasFreshUserList = true;
                }
            }
            return false;
        }

        private bool TopicInfo(string[] code, string parameters)
        {
            return false;
        }

        private bool ParseUser(string[] code)
        {
            if (code.Length > 8)
            {
                if (code[3] == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
                {
                    return true;
                }
                Channel channel = Core.GetChannel(code[3]);
                string ident = code[4];
                string host = code[5];
                string nick = code[7];
                //string server = code[6];
                char mode = '\0';
                if (code[8].Length > 0)
                {
                    mode = code[8][code[8].Length - 1];
                    if (!Core.irc.UChars.Contains(mode))
                    {
                        mode = '\0';
                    }
                }
                if (channel != null)
                {
                    if (!channel.ContainsUser(nick))
                    {
                        User _user = null;
                        if (mode != '\0')
                        {
                            _user = new User(mode.ToString() + nick, host, ident);
                        }
                        else
                        {
                            _user = new User(nick, host, ident);
                        }
                        lock (channel.UserList)
                        {
                            channel.UserList.Add(_user);
                        }
                        return true;
                    }
                    lock (channel.UserList)
                    {
                        foreach (User u in channel.UserList)
                        {
                            if (u.Nick == nick)
                            {
                                u.Ident = ident;
                                u.Host = host;
                                break;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool ParseInfo(string[] code, string[] data)
        {
            if (code.Length > 3)
            {
                string name = code[4];
                if (name == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
                {
                    return true;
                }
                Channel channel = Core.GetChannel(name);
                if (channel != null)
                {
                    string[] _chan = data[2].Split(' ');
                    foreach (var user in _chan)
                    {
                        string _user = user;
                        char _UserMode = '\0';
                        if (_user.Length > 0)
                        {
                            foreach (char mode in Core.irc.UChars)
                            {
                                if (_user[0] == mode)
                                {
                                    _UserMode = user[0];
                                    _user = _user.Substring(1);
                                }
                            }

                            lock (channel.UserList)
                            {
                                User _u = channel.RetrieveUser(_user);
                                if (_u == null && _user != "")
                                {
                                    channel.UserList.Add(new User(user, "", ""));
                                }
                                else
                                {
                                    if (_u != null)
                                    {
                                        _u.SymbolMode(_UserMode);
                                    }
                                }
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool ChannelBans(string[] code)
        {

            return false;
        }

        private bool ProcessNick(string source, string parameters, string value)
        {
            string nick = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            string _new = value;
            foreach (Channel item in Configuration.ChannelList)
            {
                if (item.Name == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
                {
                    return true;
                }
                lock (item.UserList)
                {
                    foreach (User curr in item.UserList)
                    {
                        if (curr.Nick == nick)
                        {
                            lock (ExtensionHandler.Extensions)
                            {
                                foreach (Module xx in ExtensionHandler.Extensions)
                                {
                                    try
                                    {
                                        if (xx.IsWorking)
                                        {
                                            xx.Hook_Nick(item, new User(_new, _host, _ident), nick);
                                        }
                                    }
                                    catch (Exception er)
                                    {
                                        Syslog.Log("Error on hook in " + xx.Name, true);
                                        Core.HandleException(er);
                                    }
                                }
                            }
                            curr.Nick = _new;
                        }
                    }
                }
            }
            return true;
        }

        private bool Part(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            if (chan == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
            {
                return true;
            }
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            Channel channel = Core.GetChannel(chan);
            User us = new User(user, _host, _ident);
            if (channel != null)
            {
                lock (ExtensionHandler.Extensions)
                {
                    foreach (Module module in ExtensionHandler.Extensions)
                    {
                        if (!module.IsWorking)
                        {
                            continue;
                        }
                        try
                        {
                            module.Hook_Part(channel, us);
                        }
                        catch (Exception fail)
                        {
                            Core.HandleException(fail);
                        }
                    }
                }
                User delete = null;
                if (channel.ContainsUser(user))
                {
                    lock (channel.UserList)
                    {
                        foreach (User User in channel.UserList)
                        {
                            if (User.Nick == user)
                            {
                                delete = User;
                                break;
                            }
                        }
                        channel.UserList.Remove(delete);
                    }
                    return true;
                }
                return true;
            }
            return false;
        }

        private bool Topic(string source, string parameters, string value)
        {
            string chan = parameters;
            if (chan == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
            {
                return false;
            }
            return false;
        }

        private bool Quit(string source, string parameters, string value)
        {
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            User _user = new User(user, _host, _ident);
            //string _new = value;
            lock (ExtensionHandler.Extensions)
            {
                foreach (Module module in ExtensionHandler.Extensions)
                {
                    if (!module.IsWorking)
                    {
                        continue;
                    }
                    try
                    {
                        module.Hook_Quit(_user, value);
                    }
                    catch (Exception fail)
                    {
                        Syslog.Log("MODULE: exception at Hook_Quit in " + module.Name, true);
                        Core.HandleException(fail);
                    }
                }
            }
            foreach (Channel item in Configuration.ChannelList)
            {
                User target = null;
                lock (item.UserList)
                {
                    foreach (User curr in item.UserList)
                    {
                        if (curr.Nick == user)
                        {
                            target = curr;
                            break;
                        }
                    }
                }
                if (target != null)
                {
                    lock (ExtensionHandler.Extensions)
                    {
                        foreach (Module module in ExtensionHandler.Extensions)
                        {
                            if (!module.IsWorking)
                            {
                                continue;
                            }
                            try
                            {
                                module.Hook_ChannelQuit(item, _user, value);
                            }
                            catch (Exception fail)
                            {
                                Syslog.Log("MODULE: exception at Hook_ChannelQuit in " + module.Name, true);
                                Core.HandleException(fail);
                            }
                        }
                    }
                    lock (item.UserList)
                    {
                        item.UserList.Remove(target);
                    }
                }
            }
            return true;
        }

        private bool Kick(string source, string parameters, string value)
        {
            string user = parameters.Substring(parameters.IndexOf(" ") + 1);
            string user2 = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            User Target = new User(user, "", "");
            User Source = new User(user2, _host, _ident);
            // petan!pidgeon@petan.staff.tm-irc.org KICK #support HelpBot :Removed from the channel
            string chan = parameters.Substring(0, parameters.IndexOf(" "));
            if (chan == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
            {
                return true; ;
            }
            Channel channel = Core.GetChannel(chan);
            if (channel != null)
            {
                SystemHooks.IrcKick(channel, Source, Target);
                if (channel.ContainsUser(user))
                {
                    User delete = null;
                    lock (channel.UserList)
                    {
                        foreach (User _user in channel.UserList)
                        {
                            if (_user.Nick == user)
                            {
                                delete = _user;
                                break;
                            }
                        }
                        if (delete != null)
                        {
                            channel.UserList.Remove(delete);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private bool Mode(string source, string parameters, string value)
        {
            if (parameters.Contains(" "))
            {
                string chan = parameters.Substring(0, parameters.IndexOf(" "));
                chan = chan.Replace(" ", "");
                //string user = source;
                if (chan.StartsWith("#"))
                {
                    if (chan == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
                    {
                        return true;
                    }
                    Channel channel = Core.GetChannel(chan);
                    if (channel != null)
                    {
                        string change = parameters.Substring(parameters.IndexOf(" "));

                        while (change.StartsWith(" "))
                        {
                            change = change.Substring(1);
                        }

                        Formatter formatter = new Formatter();

                        while (change.EndsWith(" ") && change.Length > 1)
                        {
                            change = change.Substring(0, change.Length - 1);
                        }

                        // we get all the mode changes for this channel
                        formatter.RewriteBuffer(change);

                        foreach (SimpleMode m in formatter.getMode)
                        {
                            if (Core.irc.CUModes.Contains(m.Mode) && m.ContainsParameter)
                            {
                                User flagged_user = channel.RetrieveUser(m.Parameter);
                                if (flagged_user != null)
                                {
                                    flagged_user.ChannelMode.ChangeMode("+" + m.Mode);
                                }
                            }
                        }

                        foreach (SimpleMode m in formatter.getRemovingMode)
                        {
                            if (Core.irc.CUModes.Contains(m.Mode) && m.ContainsParameter)
                            {
                                User flagged_user = channel.RetrieveUser(m.Parameter);
                                if (flagged_user != null)
                                {
                                    flagged_user.ChannelMode.ChangeMode("-" + m.Mode);
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private bool Join(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            if (string.IsNullOrEmpty(chan))
            {
                chan = value;
            }
            if (chan == Configuration.System.DebugChan && instance.Nick != Core.irc.NickName)
            {
                return true;
            }
            string user = source.Substring(0, source.IndexOf("!"));
            string _host = source.Substring(source.IndexOf("@") + 1);
            string _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            Channel channel = Core.GetChannel(chan);
            User _user = new User(user, _host, _ident);
            if (channel != null)
            {
                lock (ExtensionHandler.Extensions)
                {
                    foreach (Module module in ExtensionHandler.Extensions)
                    {
                        try
                        {
                            if (module.IsWorking)
                            {
                                module.Hook_Join(channel, _user);
                            }
                        }
                        catch (Exception fail)
                        {
                            Syslog.Log("MODULE: exception at Hook_Join in " + module.Name, true);
                            Core.HandleException(fail);
                        }
                    }
                }
                if (!channel.ContainsUser(user))
                {
                    lock (channel.UserList)
                    {
                        channel.UserList.Add(new User(user, _host, _ident));
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Result()
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    return false;
                }
                if (text.StartsWith(":"))
                {
                    string[] data = text.Split(':');
                    if (data.Length > 1)
                    {
                        string command = "";
                        string parameters = "";
                        string source = text.Substring(1);
                        source = source.Substring(0, source.IndexOf(" "));
                        string command2 = text.Substring(1);
                        command2 = command2.Substring(source.Length + 1);
                        if (command2.Contains(" :"))
                        {
                            command2 = command2.Substring(0, command2.IndexOf(" :"));
                        }
                        string[] _command = command2.Split(' ');
                        if (_command.Length > 0)
                        {
                            command = _command[0];
                        }
                        if (_command.Length > 1)
                        {
                            int curr = 1;
                            while (curr < _command.Length)
                            {
                                parameters += _command[curr] + " ";
                                curr++;
                            }
                            if (parameters.EndsWith(" "))
                            {
                                parameters = parameters.Substring(0, parameters.Length - 1);
                            }
                        }
                        string _value = "";
                        if (text.Length > 3 + command2.Length + source.Length)
                        {
                            _value = text.Substring(3 + command2.Length + source.Length);
                        }
                        if (_value.StartsWith(":"))
                        {
                            _value = _value.Substring(1);
                        }
                        string[] code = data[1].Split(' ');
                        switch (command)
                        {
                            case "001":
                                return true;
                            case "002":
                            case "003":
                            case "004":
                            case "005":
                                if (Info(command, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "376":
                                instance.irc.IsWorking = true;
                                break;
                            case "439":
                            case "707":
                                Syslog.Log("Unable to send data to irc: " + text, true);
                                break;
                            case "PONG":
                                Ping();
                                return true;
                            case "INFO":
                                return true;
                            case "NOTICE":
                                return true;
                            case "PING":
                                //protocol.Transfer("PONG ", Configuration.Priority.High);
                                return true;
                            case "NICK":
                                if (ProcessNick(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "TOPIC":
                                if (Topic(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "MODE":
                                if (Mode(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "PART":
                                if (Part(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "QUIT":
                                if (Quit(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "JOIN":
                                if (Join(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "KICK":
                                if (Kick(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                        }
                        if (data[1].Contains(" "))
                        {
                            switch (command)
                            {
                                case "315":
                                    if (FinishChan(code))
                                    {
                                        return true;
                                    }
                                    break;
                                case "324":
                                    if (ChannelInfo(code, command, source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "332":
                                    if (ChannelTopic(code, command, source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "333":
                                    if (TopicInfo(code, parameters))
                                    {
                                        return true;
                                    }
                                    break;
                                case "352":
                                    if (ParseUser(code))
                                    {
                                        return true;
                                    }
                                    break;
                                case "353":
                                    if (ParseInfo(code, data))
                                    {
                                        return true;
                                    }
                                    break;
                                case "366":
                                    return true;
                                case "367":
                                    if (ChannelBans(code))
                                    {
                                        return true;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            return true;
        }

        /// <summary>
        /// Creates a new instance of processor
        /// </summary>
        /// <param name="_text"></param>
        public ProcessorIRC(string _text)
        {
            text = _text;
        }
    }
}
