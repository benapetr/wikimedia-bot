//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace wmib.Extensions
{
    public class Seen : Module
    {
        public class ChannelRequest
        {
            public Channel channel;
            public string nick;
            public string source;
            public bool rg;
            public bool hostname_check = false;
            public ChannelRequest(string _nick, string _source, Channel Channel, bool regexp)
            {
                rg = regexp;
                nick = _nick;
                channel = Channel;
                source = _source;
            }
        }

        public class item
        {
            public string nick;
            public string hostname;
            public string lastplace;
            public DateTime LastSeen;
            public Action LastAc;
            public string newnick;
            public string quit;
            public enum Action
            {
                Join,
                Part,
                Talk,
                Kick,
                Exit,
                Nick
            }

            public item(string Nick, string Host, string LastPlace, Action action, string Date = null, string NewNick = "", string reason = "")
            {
                nick = Nick;
                hostname = Host;
                lastplace = LastPlace;
                if (Date != null)
                {
                    LastSeen = DateTime.FromBinary(long.Parse(Date));
                }
                LastAc = action;
                if (Date == null)
                {
                    LastSeen = DateTime.Now;
                }
                quit = reason;
                newnick = NewNick;
            }
        }

        public static List<ChannelRequest> requests = new List<ChannelRequest>();
        public Thread SearchThread;
        public Thread SearchHostThread;
        public bool Working = false;
        public List<item> GlobalList = new List<item>();
        private bool save;

        public string temp_nick;
        public Channel chan;
        public string temp_source;

        public override bool Construct()
        {
            Version = new Version(2, 3, 0, 0);
            return true;
        }

        public override void Hook_ACTN(Channel channel, libirc.UserInfo invoker, string message)
        {
            WriteStatus(invoker.Nick, invoker.Host, channel.Name, item.Action.Talk);
        }

        public override bool Hook_OnPrivateFromUser(string message, libirc.UserInfo user)
        {
            WriteStatus(user.Nick, user.Host, "<private message>", item.Action.Talk);
            if (message.StartsWith(Configuration.System.CommandPrefix + "seen "))
            {
                string parameter = message.Substring(message.IndexOf(" ") + 1);
                if (parameter != "")
                {
                    RetrieveStatus(parameter, null, user.Nick);
                    return true;
                }
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "seenrx "))
            {
                IRC.DeliverMessage("Sorry but this command can be used in channels only (it's cpu expensive so it can be used on public by trusted users only)", user);
                return true;
            }
            return false;
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            WriteStatus(invoker.Nick, invoker.Host, channel.Name, item.Action.Talk);
        }

        public override void Hook_Join(Channel channel, libirc.UserInfo user)
        {
            WriteStatus(user.Nick, user.Host, channel.Name, item.Action.Join);
        }

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("seenrx", this.cmSeenrx));
            RegisterCommand(new GenericCommand("seen", this.cmSeen));
            RegisterCommand(new GenericCommand("seen-on", this.cmSeenOn, true, "admin"));
            RegisterCommand(new GenericCommand("seen-off", this.cmSeenOff, true, "admin"));
            RegisterCommand(new GenericCommand("seen-host", this.cmSeenHost));
            return base.Hook_OnRegister();
        }

        private void cmSeenOn(CommandParams parameters)
        {
            if (GetConfig(parameters.SourceChannel, "Seen.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("seen-oe", parameters.SourceChannel.Language), parameters.SourceChannel.Name);
                return;
            }
            SetConfig(parameters.SourceChannel, "Seen.Enabled", true);
            parameters.SourceChannel.SaveConfig();
            IRC.DeliverMessage(messages.Localize("seen-on", parameters.SourceChannel.Language), parameters.SourceChannel.Name);
        }

        private void cmSeenHost(CommandParams parameters)
        {
            if (parameters.Parameters == null)
                return;
            if (GetConfig(parameters.SourceChannel, "Seen.Enabled", false) && parameters.Parameters != "")
                RetrieveStatusOfHost(parameters.Parameters, parameters.SourceChannel, parameters.User.Nick);
        }

        private void cmSeen(CommandParams parameters)
        {
            if (parameters.Parameters == null)
                return;
            if (GetConfig(parameters.SourceChannel, "Seen.Enabled", false) && parameters.Parameters != "")
                    RetrieveStatus(parameters.Parameters, parameters.SourceChannel, parameters.User.Nick);
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("seen");
            UnregisterCommand("seenrx");
            UnregisterCommand("seen-on");
            UnregisterCommand("seen-off");
            UnregisterCommand("seen-host");
            return base.Hook_OnUnload();
        }

        private void cmSeenOff(CommandParams parameters)
        {
            if (!GetConfig(parameters.SourceChannel, "Seen.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("seen-e2", parameters.SourceChannel.Language), parameters.SourceChannel.Name);
                return;
            }
            IRC.DeliverMessage(messages.Localize("seen-off", parameters.SourceChannel.Language), parameters.SourceChannel.Name);
            SetConfig(parameters.SourceChannel, "Seen.Enabled", false);
            parameters.SourceChannel.SaveConfig();
        }

        private void cmSeenrx(CommandParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.Parameters))
                return;
            if (GetConfig(parameters.SourceChannel, "Seen.Enabled", false))
            {
                RegEx(parameters.Parameters, parameters.SourceChannel, parameters.User.Nick);
            }
        }

        public override void Hook_Nick(Channel channel, libirc.UserInfo Target, string OldNick, string NewNick)
        {
            WriteStatus(OldNick, Target.Host, channel.Name, item.Action.Nick, NewNick);
        }

        public override void Hook_Kick(Channel channel, libirc.UserInfo source, string user)
        {
            WriteStatus(user, "", channel.Name, item.Action.Kick);
        }

        public override void Hook_Part(Channel channel, libirc.UserInfo user)
        {
            WriteStatus(user.Nick, user.Host, channel.Name, item.Action.Part);
        }

        public override void Hook_Quit(libirc.UserInfo user, string Message)
        {
            WriteStatus(user.Nick, user.Host, "N/A", item.Action.Exit, "", Message);
        }
        
        public override void Load()
        {
            try
            {
                LoadData();
                while (this.IsWorking)
                {
                    if (save)
                    {
                        save = false;
                        Save();
                    }
                    Thread.Sleep(20000);
                }
            }
            catch (ThreadAbortException)
            {
                Save();
                if (SearchThread != null && SearchThread.ThreadState == ThreadState.Running)
                    wmib.Core.ThreadManager.KillThread(SearchThread);
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        public void WriteStatus(string nick, string host, string place, item.Action action, string newnick = "", string reason = "")
        {
            item user = null;
            lock (GlobalList)
            {
                foreach (item xx in GlobalList)
                {
                    if (nick.ToUpper() == xx.nick.ToUpper())
                    {
                        user = xx;
                        break;
                    }
                }
                if (user == null)
                {
                    user = new item(nick, host, place, action, null, newnick, reason);
                    GlobalList.Add(user);
                }
                else
                {
                    user.nick = nick;
                    user.LastAc = action;
                    user.LastSeen = DateTime.Now;
                    user.hostname = host;
                    user.lastplace = place;
                    user.quit = reason;
                    user.newnick = newnick;
                }
            }
            save = true;
        }

        public void Search()
        {
            try
            {
                if (misc.IsValidRegex(temp_nick))
                {
                    Regex ex = new Regex(temp_nick);
                    string response = "I have never seen " + temp_nick;
                    bool found = false;
                    bool multiple = false;
                    string results = "";
                    int cn = 0;
                    string action = "quitting the network with reason " ;
                    lock (GlobalList)
                    {
                        foreach (item xx in GlobalList)
                        {
                            if (ex.IsMatch(xx.nick))
                            {
                                if (found)
                                {
                                    cn++;
                                    if (cn < 6)
                                    {
                                        results += xx.nick + ", ";
                                    }
                                    multiple = true;
                                    continue;
                                }
                                found = true;
                                Channel last;
                                switch (xx.LastAc)
                                {
                                    case item.Action.Join:
                                        action = "joining the channel";
                                        last = Core.GetChannel(xx.lastplace);
                                        if (last != null)
                                        {
                                            if (last.ContainsUser(xx.nick))
                                            {
                                                action += ", they are still in the channel";
                                            }
                                            else
                                            {
                                                action += ", but they are not in the channel now and I don't know why, in";
                                            }
                                        }
                                        break;
                                    case item.Action.Kick:
                                        action = "kicked from the channel";
                                        break;
                                    case item.Action.Nick:
                                        if (xx.newnick == null)
                                        {
                                            action = "error NULL pointer at record";
                                        }
                                        else
                                        {
                                            action = "changing the nickname to " + xx.newnick;
                                            last = Core.GetChannel(xx.lastplace);
                                            if (last.ContainsUser(xx.newnick))
                                            {
                                                action += " and " + xx.newnick + " is still in the channel";
                                            }
                                            else
                                            {
                                                action += ", but " + xx.newnick + " is no longer in channel";
                                            }
                                            item nick = getItem(xx.newnick);
                                            if (nick != null)
                                            {
                                                TimeSpan span3 = DateTime.Now - nick.LastSeen;
                                                switch (nick.LastAc)
                                                {
                                                    case item.Action.Exit:
                                                        action += " because he quit the network " + span3 + " ago. The nick change was done in";
                                                        break;
                                                    case item.Action.Kick:
                                                        action += " because he was kicked from the channel " + span3 + " ago. The nick change was done in";
                                                        break;
                                                    case item.Action.Part:
                                                        action += " because he left the channel " + span3 + " ago. The nick change was done in";
                                                        break;
                                                }
                                            }
                                        }
                                        break;
                                    case item.Action.Part:
                                        action = "leaving the channel";
                                        break;
                                    case item.Action.Talk:
                                        action = "talking in the channel";
                                        last = Core.GetChannel(xx.lastplace);
                                        if (last != null)
                                        {
                                            if (last.ContainsUser(xx.nick))
                                            {
                                                action += ", they are still in the channel. It was in";
                                            }
                                            else
                                            {
                                                action += ", but they are not in the channel now and I don't know why. It was in";
                                            }
                                        }
                                        break;
                                    case item.Action.Exit:
                                        string reason = xx.quit;
                                        if (String.IsNullOrEmpty(reason))
                                        {
                                            reason = "no reason was given";
                                        }
                                        action = "quitting the network with reason: " + reason;
                                        break;
                                }
                                TimeSpan span2 = DateTime.Now - xx.LastSeen;
                                if (xx.lastplace == null)
                                    xx.lastplace = "N/A";
                                if (xx.LastAc == item.Action.Exit)
                                    response = "Last time I saw " + xx.nick + " they were " + action + " at " + xx.LastSeen + " (" + FormatTimeSpan(span2) + " ago)";
                                else
                                    response = "Last time I saw " + xx.nick + " they were " + action + " " + xx.lastplace + " at " + xx.LastSeen + " (" + FormatTimeSpan(span2) + " ago)";
                            }
                        }
                    }
                    if (temp_nick.ToUpper() == temp_source.ToUpper())
                    {
                        response = "are you really looking for yourself?";
                        IRC.DeliverMessage(temp_source + ": " + response, chan.Name);
                        Working = false;
                        goto ex;
                    }
                    if (temp_nick.ToUpper() == Configuration.IRC.NickName.ToUpper())
                    {
                        response = "I am right here";
                        IRC.DeliverMessage(temp_source + ": " + response, chan.Name);
                        Working = false;
                        goto ex;
                    }
                    if (chan.ContainsUser(temp_nick))
                        response = temp_nick + " is in here, right now";
                    if (multiple)
                    {
                        if (results.Length > 2)
                            results = results.Substring(0, results.Length - 2);
                        if (cn > 5)
                            results = results + " and " + (cn - 5) + " more results";
                        response += " (multiple results were found: " + results + ")";
                    }
                    IRC.DeliverMessage(temp_source + ": " + response, chan.Name);
                    Working = false;
                    goto ex;
                }
                IRC.DeliverMessage(messages.Localize("Error1", chan.Language), chan.Name);
                Working = false;
            }
            catch (ThreadAbortException)
            {
                goto ex;
            }
            catch (Exception fail)
            {
                HandleException(fail);
                IsWorking = false;
            }
            ex:
                Core.ThreadManager.UnregisterThread(SearchThread);
        }

        public void StartRegex()
        {
            try
            {
                while (true)
                {
                    if (requests.Count > 0)
                    {
                        List<ChannelRequest> Requests = new List<ChannelRequest>();
                        lock (requests)
                        {
                            Requests.AddRange(requests);
                            requests.Clear();
                        }
                        foreach (ChannelRequest ch in Requests)
                        {
                            if (ch.rg)
                            {
                                RegEx2(ch.nick, ch.channel, ch.source);
                                continue;
                            }
                            RetrieveStatus2(ch.nick, ch.channel, ch.source, ch.hostname_check);
                        }
                        Requests.Clear();
                    }
                    Thread.Sleep(100);
                }
            }
            catch (ThreadAbortException)
            { }
            catch (Exception fail)
            {
                HandleException(fail);
            }
            Core.ThreadManager.UnregisterThread(SearchHostThread);
        }

        public void RegEx2(string nick, Channel channel, string source)
        {
            try
            {
                temp_nick = nick;
                temp_source = source;
                chan = channel;
                SearchThread = new Thread(Search) {Name = "Module:Seen/Search"};
                wmib.Core.ThreadManager.RegisterThread(SearchThread);
                SearchThread.Start();
                Working = true;
                int curr = 0;
                while (Working)
                {
                    Thread.Sleep(10);
                    curr++;
                    if (curr > 80)
                    {
                        Core.ThreadManager.KillThread(SearchThread);
                        IRC.DeliverMessage("This search took too much time, please optimize query", channel.Name);
                        Working = false;
                        break;
                    }
                }
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        public void RegEx(string nick, Channel channel, string source)
        {
            lock (requests)
            {
                requests.Add(new ChannelRequest(nick, source, channel, true));
            }
        }

        public void RetrieveStatus(string nick, Channel channel, string source)
        {
            nick = nick.Trim();
            lock (requests)
            {
                requests.Add(new ChannelRequest(nick, source, channel, false));
            }
        }

        public void RetrieveStatusOfHost(string nick, Channel channel, string source)
        {
            lock (requests)
            {
                ChannelRequest rq = new ChannelRequest(nick, source, channel, false) {hostname_check = true};
                requests.Add(rq);
            }
        }

        public item getItem(string nick)
        {
            nick = nick.ToUpper();
            lock (GlobalList)
            {
                foreach (item xx in GlobalList)
                {
                    if (nick == xx.nick.ToUpper())
                    {
                        return xx;
                    }
                }
            }
            return null;
        }

        public override void Hook_BeforeSysWeb(ref string html)
        {
            html += "<br /><p>Seen data: " + GlobalList.Count + "</p>";
        }

        public void RetrieveStatus2(string nick, Channel channel, string source, bool by_host)
        {
            try
            {
                string response = "I have never seen " + nick;
                bool found = false;
                string action = "quiting the network";
                lock (GlobalList)
                {
                    foreach (item xx in GlobalList)
                    {
                        if ((!by_host && (nick.ToUpper() == xx.nick.ToUpper())) || (by_host && (nick.ToUpper() == xx.hostname.ToUpper())))
                        {
                            found = true;
                            nick = xx.nick;
                            Channel last;
                            switch (xx.LastAc)
                            {
                                case item.Action.Join:
                                    action = "joining the channel";
                                    last = Core.GetChannel(xx.lastplace);
                                    if (last != null)
                                    {
                                        if (last.ContainsUser(nick))
                                        {
                                            action += ", they are still in the channel";
                                        }
                                        else
                                        {
                                            action += ", but they are not in the channel now and I don't know why, in";
                                        }
                                    }
                                    break;
                                case item.Action.Kick:
                                    action = "kicked from the channel";
                                    break;
                                case item.Action.Nick:
                                    if (xx.newnick == null)
                                    {
                                        action = "error NULL pointer at record";
                                        break;
                                    }
                                    action = "changing the nickname to " + xx.newnick;
                                    last = Core.GetChannel(xx.lastplace);
                                    if (last.ContainsUser(xx.newnick))
                                    {
                                        action += " and " + xx.newnick + " is still in the channel";
                                    }
                                    else
                                    {
                                        action += ", but " + xx.newnick + " is no longer in channel";
                                    }
                                    item nick2 = getItem(xx.newnick);
                                    if (nick2 != null)
                                    {
                                        TimeSpan span3 = DateTime.Now - nick2.LastSeen;
                                        switch (nick2.LastAc)
                                        {
                                            case item.Action.Exit:
                                                action += " because he quit the network " + span3 + " ago. The nick change was done in";
                                                break;
                                            case item.Action.Kick:
                                                action += " because he was kicked from the channel " + span3 + " ago. The nick change was done in";
                                                break;
                                            case item.Action.Part:
                                                action += " because he left the channel " + span3 + " ago. The nick change was done in";
                                                break;
                                        }
                                    }
                                    break;
                                case item.Action.Part:
                                    action = "leaving the channel";
                                    break;
                                case item.Action.Talk:
                                    action = "talking in the channel";
                                    last = Core.GetChannel(xx.lastplace);
                                    if (last != null)
                                    {
                                        if (last.ContainsUser(nick))
                                        {
                                            action += ", they are still in the channel";
                                        }
                                        else
                                        {
                                            action += ", but they are not in the channel now and I don't know why, in";
                                        }
                                    }
                                    break;
                                case item.Action.Exit:
                                    string reason = xx.quit;
                                    if (reason == "")
                                    {
                                        reason = "no reason was given";
                                    }
                                    action = "quitting the network with reason: " + reason;
                                    break;
                            }
                            TimeSpan span = DateTime.Now - xx.LastSeen;
                            response = "Last time I saw " + nick + " they were " + action + " " + xx.lastplace + " at " + xx.LastSeen + " (" + FormatTimeSpan(span) + " ago)";
                            break;
                        }
                    }
                }
                string target = source;
                if (channel != null)
                {
                    target = channel.Name;
                }
                if (!by_host && nick.ToUpper() == source.ToUpper())
                {
                    response = "are you really looking for yourself?";
                    IRC.DeliverMessage(source + ": " + response, target);
                    return;
                }
                if (!by_host && nick.ToUpper() == Configuration.IRC.NickName.ToUpper())
                {
                    response = "I am right here";
                    IRC.DeliverMessage(source + ": " + response, target);
                    return;
                }
                if (!by_host && channel != null)
                {
                    if (channel.ContainsUser(nick))
                    {
                        response = nick + " is in here, right now";
                        found = true;
                    }
                }
                if (!by_host && !found)
                {
                    foreach (Channel Item in Configuration.ChannelList)
                    {
                        if (Item.ContainsUser(nick))
                        {
                            response = nick + " is in " + Item.Name + " right now";
                            break;
                        }
                    }
                }
                IRC.DeliverMessage(source + ": " + response, target);
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        public void Save()
        {
            try
            {
                XmlDocument stat = new XmlDocument();
                XmlNode xmlnode = stat.CreateElement("channel_stat");
                lock (GlobalList)
                {
                    foreach (item curr in GlobalList)
                    {
                        XmlAttribute name = stat.CreateAttribute("nick");
                        name.Value = curr.nick;
                        XmlAttribute host = stat.CreateAttribute("hostname");
                        host.Value = curr.hostname;
                        XmlAttribute last = stat.CreateAttribute("lastplace");
                        last.Value = curr.lastplace;
                        XmlAttribute action = stat.CreateAttribute("action");
                        XmlAttribute date = stat.CreateAttribute("date");
                        XmlAttribute newn = null;
                        XmlAttribute quit = stat.CreateAttribute("reason");
                        quit.Value = curr.quit;
                        if (!string.IsNullOrEmpty(curr.newnick))
                        {
                            newn = stat.CreateAttribute("newnick");
                            newn.Value = curr.newnick;
                        }
                        date.Value = curr.LastSeen.ToBinary().ToString();
                        action.Value = "Exit";
                        switch (curr.LastAc)
                        {
                            case item.Action.Nick:
                                action.Value = "Nick";
                                break;
                            case item.Action.Join:
                                action.Value = "Join";
                                break;
                            case item.Action.Part:
                                action.Value = "Part";
                                break;
                            case item.Action.Kick:
                                action.Value = "Kick";
                                break;
                            case item.Action.Talk:
                                action.Value = "Talk";
                                break;
                        }
                        XmlNode db = stat.CreateElement("user");
                        db.Attributes.Append(name);
                        db.Attributes.Append(host);
                        db.Attributes.Append(last);
                        db.Attributes.Append(action);
                        db.Attributes.Append(date);
                        if (newn != null && curr.newnick != "")
                        {
                            db.Attributes.Append(newn);
                        }
                        db.Attributes.Append(quit);
                        xmlnode.AppendChild(db);
                    }
                }
                stat.AppendChild(xmlnode);
                if (File.Exists(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "seen.db"))
                {
                    Core.BackupData(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "seen.db");
                }
                stat.Save(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "seen.db");
                if (File.Exists(Configuration.TempName(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "seen.db")))
                {
                    File.Delete(Configuration.TempName(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "seen.db"));
                }
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        public void LoadData()
        {
            SearchHostThread = new Thread(StartRegex) {Name = "Module:Seen/SearchHostThread"};
            Core.ThreadManager.RegisterThread(SearchHostThread);
            SearchHostThread.Start();
            try
            {
                Core.RecoverFile(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "seen.db");
                if (File.Exists(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "seen.db"))
                {
                    GlobalList = new List<item>();
                    lock (GlobalList)
                    {
                        XmlDocument stat = new XmlDocument();
                        stat.Load(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "seen.db");
                        if (stat.ChildNodes[0].ChildNodes.Count > 0)
                        {
                            foreach (XmlNode curr in stat.ChildNodes[0].ChildNodes)
                            {
                                try
                                {
                                    string user = curr.Attributes[0].Value;
                                    item.Action action = item.Action.Exit;
                                    switch (curr.Attributes[3].Value)
                                    {
                                        case "Join":
                                            action = item.Action.Join;
                                            break;
                                        case "Part":
                                            action = item.Action.Part;
                                            break;
                                        case "Talk":
                                            action = item.Action.Talk;
                                            break;
                                        case "Kick":
                                            action = item.Action.Kick;
                                            break;
                                        case "Nick":
                                            action = item.Action.Nick;
                                            break;
                                    }
                                    string Newnick = "";
                                    string Reason = "";
                                    if (curr.Attributes.Count > 4)
                                    {
                                        if (curr.Attributes[4].Name == "newnick")
                                        {
                                            Newnick = curr.Attributes[4].Value;
                                        }
                                        else if (curr.Attributes[4].Name == "reason")
                                        {
                                            Reason = curr.Attributes[5].Value;
                                        }
                                    }
                                    if (curr.Attributes.Count > 5)
                                    {
                                        if (curr.Attributes[5].Name == "reason")
                                        {
                                            Reason = curr.Attributes[5].Value;
                                        }
                                    }
                                    item User = new item(user, curr.Attributes[1].Value, curr.Attributes[2].Value, action, curr.Attributes[4].Value, Newnick, Reason);
                                    GlobalList.Add(User);
                                }
                                catch (Exception fail)
                                {
                                    HandleException(fail);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception f)
            {
                HandleException(f);
            }
        }

        public static string FormatTimeSpan(TimeSpan ts)
        {
            string newTimeString = "";
            if (ts.Days != 0)
                newTimeString += ts.Days + "d";
            if (ts.Hours != 0)
                newTimeString += ts.Hours + "h";
            if (ts.Minutes != 0)
                newTimeString += ts.Minutes + "m";
            return newTimeString + ts.Seconds + "s";
        }
    }
}
