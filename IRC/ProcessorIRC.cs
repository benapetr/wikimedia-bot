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
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace wmib
{
    public class ProcessorIRC
    {
        public string text;

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
                config.channel channel = core.getChannel(code[3]);
                if (channel != null)
                {
                    Program.Log("Finished parsing for " + channel.Name + " parsed totaly: " + channel.UserList.Count.ToString());
                    channel.FreshList = true;
                }
            }
            return false;
        }

        private bool TopicInfo(string[] code, string parameters)
        {
            return false;
        }

        private bool ParseUs(string[] code)
        {
            if (code.Length > 8)
            {
                config.channel channel = core.getChannel(code[3]);
                string ident = code[4];
                string host = code[5];
                string nick = code[7];
                string server = code[6];
                if (channel != null)
                {
                    if (!channel.containsUser(nick))
                    {
                        User _user = new User(nick, host, ident);
                        channel.UserList.Add(_user);
                        return true;
                    }
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
            return false;
        }

        private bool ParseInfo(string[] code, string[] data)
        {
            if (code.Length > 3)
            {
                string name = code[4];
                config.channel channel = core.getChannel(name);
                if (channel != null)
                {
                    string[] _chan = data[2].Split(' ');
                    foreach (var user in _chan)
                    {
                        if (!channel.containsUser(user) && user != "")
                        {
                            lock (channel.UserList)
                            {
                                channel.UserList.Add(new User(user, "", ""));
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
            foreach (config.channel item in config.channels)
            {
                lock (item.UserList)
                {
                    foreach (User curr in item.UserList)
                    {
                        if (curr.Nick == nick)
                        {
                            lock (Module.module)
                            {
                                foreach (Module xx in Module.module)
                                {
                                    try
                                    {
                                        if (xx.working)
                                        {
                                            xx.Hook_Nick(item, new User(_new, _host, _ident), nick);
                                        }
                                    }
                                    catch (Exception er)
                                    {
                                        core.Log("Error on hook in " + xx.Name, true);
                                        core.handleException(er);
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

        private bool Mode(string source, string parameters, string value)
        {
            return false;
        }

        private bool Part(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            config.channel channel = core.getChannel(chan);
            User us = new User(user, _host, _ident);
            if (channel != null)
            {
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        if (!module.working)
                        {
                            continue;
                        }
                        try
                        {
                            module.Hook_Part(channel, us);
                        }
                        catch (Exception fail)
                        {
                            core.handleException(fail);
                        }
                    }
                }
                User delete = null;
                if (channel.containsUser(user))
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
            string _new = value;
            lock (Module.module)
            {
                foreach (Module module in Module.module)
                {
                    if (!module.working)
                    {
                        continue;
                    }
                    try
                    {
                        module.Hook_Quit(_user, value);
                    }
                    catch (Exception fail)
                    {
                        core.Log("MODULE: exception at Hook_Quit in " + module.Name, true);
                        core.handleException(fail);
                    }
                }
            }
            foreach (config.channel item in config.channels)
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
            User user01 = new User(user, "", "");
            User sr = new User(user2, _host, _ident);
            // petan!pidgeon@petan.staff.tm-irc.org KICK #support HelpBot :Removed from the channel
            config.channel channel = core.getChannel(parameters.Substring(0, parameters.IndexOf(" ")));
            if (channel != null)
            {
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        if (!module.working)
                        {
                            continue;
                        }
                        try
                        {
                            module.Hook_Kick(channel, sr, user01);
                        }
                        catch (Exception fail)
                        {
                            core.Log("MODULE: exception at Hook_Kick in " + module.Name, true);
                            core.handleException(fail);
                        }
                    }
                }
                if (channel.containsUser(user))
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

        private bool Join(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            if (chan == "")
            {
                chan = value;
            }
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            config.channel channel = core.getChannel(chan);
            User _user = new User(user, _host, _ident);
            if (channel != null)
            {
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        try
                        {
                            if (module.working)
                            {
                                module.Hook_Join(channel, _user);
                            }
                        }
                        catch (Exception fail)
                        {
                            core.Log("MODULE: exception at Hook_Join in " + module.Name, true);
                            core.handleException(fail);
                        }
                    }
                }
                if (!channel.containsUser(user))
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

        public bool Result()
        {
            try
            {
                if (text == null || text == "")
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
                        string command2 = "";
                        string source;
                        string _value;
                        source = text.Substring(1);
                        source = source.Substring(0, source.IndexOf(" "));
                        command2 = text.Substring(1);
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
                        _value = "";
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
                                    if (ParseUs(code))
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
                core.handleException(fail);
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
