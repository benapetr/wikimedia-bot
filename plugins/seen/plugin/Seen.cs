using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;

namespace wmib
{
    public class RegularModule : Module
    {
        private bool save = false;

        public override bool Construct()
        {
            Name = "SEEN";
            start = true;
            Version = "2.2.1.10";
            return true;
        }

        public override void Hook_ACTN(config.channel channel, User invoker, string message)
        {
            WriteStatus(invoker.Nick, invoker.Host, channel.Name, item.Action.Talk);
        }

        public override bool Hook_OnPrivateFromUser(string message, User user)
        {
            WriteStatus(user.Nick, user.Host, "<private message>", item.Action.Talk);
            if (message.StartsWith(config.CommandPrefix + "seen "))
            {
                    string parameter = "";
                        parameter = message.Substring(message.IndexOf(" ") + 1);
                    if (parameter != "")
                    {
                        RetrieveStatus(parameter, null, user.Nick);
                        return true;
                    }
            }

            if (message.StartsWith(config.CommandPrefix + "seenrx "))
            {
                    core.irc._SlowQueue.DeliverMessage("Sorry but this command can be used in channels only (it's cpu expensive so it can be used on public by trusted users only)", user.Nick, IRC.priority.low);
                    return true;
            }
            return false;
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            WriteStatus(invoker.Nick, invoker.Host, channel.Name, item.Action.Talk);
            if (message.StartsWith(config.CommandPrefix + "seen "))
            {
                if (GetConfig(channel, "Seen.Enabled", false))
                {
                    string parameter = "";
                    if (message.Contains(" "))
                    {
                        parameter = message.Substring(message.IndexOf(" ") + 1);
                    }
                    if (parameter != "")
                    {
                        RetrieveStatus(parameter, channel, invoker.Nick);
                        return;
                    }
                }
            }

            if (message.StartsWith(config.CommandPrefix + "seenrx "))
            {
                if (channel.Users.IsApproved(invoker, "trust"))
                {
                    if (GetConfig(channel, "Seen.Enabled", false))
                    {
                        string parameter = "";
                        if (message.Contains(" "))
                        {
                            parameter = message.Substring(message.IndexOf(" ") + 1);
                        }
                        if (parameter != "")
                        {
                            RegEx(parameter, channel, invoker.Nick);
                            return;
                        }
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "seen-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "Seen.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("seen-e2", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("seen-off", channel.Language), channel.Name, IRC.priority.high);
                        SetConfig(channel, "Seen.Enabled", false);
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

            if (message == config.CommandPrefix + "seen-on")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "Seen.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("seen-oe", channel.Language), channel.Name);
                        return;
                    }
                    SetConfig(channel, "Seen.Enabled", true);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage(messages.get("seen-on", channel.Language), channel.Name, IRC.priority.high);
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }
        }

        public override void Hook_Join(config.channel channel, User user)
        {
            WriteStatus(user.Nick, user.Host, channel.Name, item.Action.Join);
        }

        public override void Hook_Nick(config.channel channel, User Target, string OldNick)
        {
            WriteStatus(OldNick, Target.Host, channel.Name, item.Action.Nick, Target.Nick);
            return;
        }

        public override void Hook_Kick(config.channel channel, User source, User user)
        {
            WriteStatus(user.Nick, user.Host, channel.Name, item.Action.Kick);
        }

        public override void Hook_Part(config.channel channel, User user)
        {
            WriteStatus(user.Nick, user.Host, channel.Name, item.Action.Part);
        }

        public override void Hook_Quit(User user, string Message)
        {
            WriteStatus(user.Nick, user.Host, "N/A", item.Action.Exit, "", Message);
        }

        public class ChannelRequest
        {
            public config.channel channel;
            public string nick;
            public string source;
            public bool rg;
            public ChannelRequest(string _nick, string _source, config.channel Channel, bool regexp)
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

        public string temp_nick;
        public config.channel chan;
        public string temp_source;

        public override void Load()
        {
            try
            {
                LoadData();
                while (true)
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
                if (SearchThread.ThreadState == ThreadState.Running)
                {
                    SearchThread.Abort();
                }
                return;
            }
            catch (Exception fail)
            {
                handleException(fail);
            }
        }

        public List<item> global = new List<item>();

        public void WriteStatus(string nick, string host, string place, item.Action action, string newnick = "", string reason = "")
        {
            item user = null;
            lock (global)
            {
                foreach (item xx in global)
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
                    global.Add(user);
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
                    System.Text.RegularExpressions.Regex ex = new System.Text.RegularExpressions.Regex(temp_nick);
                    string response = "I have never seen " + temp_nick;
                    bool found = false;
                    bool multiple = false;
                    string results = "";
                    int cn = 0;
                    string action = "quitting the network with reason " ;
                    foreach (item xx in global)
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
                            config.channel last = null;
                            switch (xx.LastAc)
                            {
                                case item.Action.Join:
                                    action = "joining the channel";
                                    last = core.getChannel(xx.lastplace);
                                    if (last != null)
                                    {
                                        if (last.containsUser(xx.nick))
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
                                        last = core.getChannel(xx.lastplace);
                                        if (last.containsUser(xx.newnick))
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
                                                    action += " because he quitted the network " + span3.ToString() + " ago. The nick change was done in";
                                                    break;
                                                case item.Action.Kick:
                                                    action += " because he was kicked from the channel " + span3.ToString() + " ago. The nick change was done in";
                                                    break;
                                                case item.Action.Part:
                                                    action += " because he left the channel " + span3.ToString() + " ago. The nick change was done in";
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
                                    last = core.getChannel(xx.lastplace);
                                    if (last != null)
                                    {
                                        if (last.containsUser(xx.nick))
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
                                    if (reason == "")
                                    {
                                        reason = "no reason was given";
                                    }
                                    action = "quitting the network with reason: " + reason;
                                    break;
                            }
                            TimeSpan span2 = DateTime.Now - xx.LastSeen;
                            if (xx.lastplace == null)
                            {
                                xx.lastplace = "N/A";
                            }

                            if (xx.LastAc == item.Action.Exit)
                            {
                                response = "Last time I saw " + xx.nick + " they were " + action + " at " + xx.LastSeen.ToString() + " (" + RegularModule.FormatTimeSpan(span2) + " ago)";
                            }
                            else
                            {
                                response = "Last time I saw " + xx.nick + " they were " + action + " " + xx.lastplace + " at " + xx.LastSeen.ToString() + " (" + RegularModule.FormatTimeSpan(span2) + " ago)";
                            }
                        }
                    }
                    if (temp_nick.ToUpper() == temp_source.ToUpper())
                    {
                        response = "are you really looking for yourself?";
                        core.irc._SlowQueue.DeliverMessage(temp_source + ": " + response, chan.Name);
                        Working = false;
                        return;
                    }
                    if (temp_nick.ToUpper() == config.username.ToUpper())
                    {
                        response = "I am right here";
                        core.irc._SlowQueue.DeliverMessage(temp_source + ": " + response, chan.Name);
                        Working = false;
                        return;
                    }
                    if (chan.containsUser(temp_nick))
                    {
                        response = temp_nick + " is in here, right now";
                        found = true;
                    }
                    if (multiple)
                    {
                        if (results.Length > 2)
                        {
                            results = results.Substring(0, results.Length - 2);
                        }
                        if (cn > 5)
                        {
                            results = results + " and " + (cn - 5).ToString() + " more results";
                        }
                        response += " (multiple results were found: " + results + ")";
                    }
                    core.irc._SlowQueue.DeliverMessage(temp_source + ": " + response, chan.Name);
                    Working = false;
                    return;
                }
                core.irc._SlowQueue.DeliverMessage(messages.get("Error1", chan.Language), chan.Name);
                Working = false;
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                handleException(fail);
                Working = false;
            }
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
                            RetrieveStatus2(ch.nick, ch.channel, ch.source);
                        }
                        Requests.Clear();
                    }
                    Thread.Sleep(100);
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

        public void RegEx2(string nick, config.channel channel, string source)
        {
            try
            {
                temp_nick = nick;
                temp_source = source;
                chan = channel;
                SearchThread = new Thread(Search);
                SearchThread.Start();
                Working = true;
                int curr = 0;
                while (Working)
                {
                    Thread.Sleep(10);
                    curr++;
                    if (curr > 80)
                    {
                        SearchThread.Abort();
                        core.irc._SlowQueue.DeliverMessage("This search took too much time, please optimize query", channel.Name);
                        Working = false;
                        break;
                    }
                }
            }
            catch (Exception fail)
            {
                handleException(fail);
            }
        }

        public void RegEx(string nick, config.channel channel, string source)
        {
            lock (requests)
            {
                requests.Add(new ChannelRequest(nick, source, channel, true));
            }
        }

        public void RetrieveStatus(string nick, config.channel channel, string source)
        {
            lock (requests)
            {
                requests.Add(new ChannelRequest(nick, source, channel, false));
            }
        }

        public item getItem(string nick)
        {
            nick = nick.ToUpper();
            foreach (item xx in global)
            {
                if (nick == xx.nick.ToUpper())
                {
                    return xx;
                }
            }
            return null;
        }

        public override void Hook_BeforeSysWeb(ref string html)
        {
            html += "<br><p>Seen data: " + global.Count.ToString() + "</p>";
        }

        public void RetrieveStatus2(string nick, config.channel channel, string source)
        {
            try
            {
                string response = "I have never seen " + nick;
                bool found = false;
                string action = "quiting the network";
                foreach (item xx in global)
                {
                    if (nick.ToUpper() == xx.nick.ToUpper())
                    {
                        found = true;
                        config.channel last;
                        switch (xx.LastAc)
                        {
                            case item.Action.Join:
                                action = "joining the channel";
                                last = core.getChannel(xx.lastplace);
                                if (last != null)
                                {
                                    if (last.containsUser(nick))
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
                                last = core.getChannel(xx.lastplace);
                                if (last.containsUser(xx.newnick))
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
                                            action += " because he quitted the network " + span3.ToString() + " ago. The nick change was done in";
                                            break;
                                        case item.Action.Kick:
                                            action += " because he was kicked from the channel " + span3.ToString() + " ago. The nick change was done in";
                                            break;
                                        case item.Action.Part:
                                            action += " because he left the channel " + span3.ToString() + " ago. The nick change was done in";
                                            break;
                                    }
                                }
                                break;
                            case item.Action.Part:
                                action = "leaving the channel";
                                break;
                            case item.Action.Talk:
                                action = "talking in the channel";
                                last = core.getChannel(xx.lastplace);
                                if (last != null)
                                {
                                    if (last.containsUser(nick))
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
                        if (xx.LastAc == item.Action.Exit)
                        {
                            response = "Last time I saw " + nick + " they were " + action + " at " + xx.LastSeen.ToString() + " (" + RegularModule.FormatTimeSpan (span) + " ago)";
                        }
                        response = "Last time I saw " + nick + " they were " + action + " " + xx.lastplace + " at " + xx.LastSeen.ToString() + " (" + RegularModule.FormatTimeSpan (span) + " ago)";
                        break;
                    }
                }
                string target = source;
                if (channel != null)
                {
                    target = channel.Name;
                }
                if (nick.ToUpper() == source.ToUpper())
                {
                    response = "are you really looking for yourself?";
                    core.irc._SlowQueue.DeliverMessage(source + ": " + response, target, IRC.priority.normal);
                    return;
                }
                if (nick.ToUpper() == config.username.ToUpper())
                {
                    response = "I am right here";
                    core.irc._SlowQueue.DeliverMessage(source + ": " + response, target, IRC.priority.normal);
                    return;
                }
                if (channel != null)
                {
                    if (channel.containsUser(nick))
                    {
                        response = nick + " is in here, right now";
                        found = true;
                    }
                }
                if (!found)
                {
                    foreach (config.channel Item in config.channels)
                    {
                        if (Item.containsUser(nick))
                        {
                            response = nick + " is in " + Item.Name + " right now";
                            break;
                        }
                    }
                }
                core.irc._SlowQueue.DeliverMessage(source + ": " + response, target, IRC.priority.normal);
            }
            catch (Exception fail)
            {
                handleException(fail);
            }
        }

        public void Save()
        {
            try
            {
                XmlDocument stat = new XmlDocument();
                XmlNode xmlnode = stat.CreateElement("channel_stat");
                lock (global)
                {
                    foreach (item curr in global)
                    {
                        XmlAttribute name = stat.CreateAttribute("nick");
                        name.Value = curr.nick;
                        XmlAttribute host = stat.CreateAttribute("hostname");
                        host.Value = curr.hostname.ToString();
                        XmlAttribute last = stat.CreateAttribute("lastplace");
                        last.Value = curr.lastplace;
                        XmlAttribute action = stat.CreateAttribute("action");
                        XmlAttribute date = stat.CreateAttribute("date");
                        XmlAttribute newn = null;
                        XmlAttribute quit = stat.CreateAttribute("reason");
                        quit.Value = curr.quit;
                        if (curr.newnick != null && curr.newnick != "")
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
                if (System.IO.File.Exists(variables.config + System.IO.Path.DirectorySeparatorChar + "seen.db"))
                {
                    core.backupData(variables.config + System.IO.Path.DirectorySeparatorChar + "seen.db");
                }
                stat.Save(variables.config + System.IO.Path.DirectorySeparatorChar + "seen.db");
                if (System.IO.File.Exists(config.tempName(variables.config + System.IO.Path.DirectorySeparatorChar + "seen.db")))
                {
                    System.IO.File.Delete(config.tempName(variables.config + System.IO.Path.DirectorySeparatorChar + "seen.db"));
                }
            }
            catch (Exception fail)
            {
                handleException(fail);
            }
        }

        public void LoadData()
        {
            SearchHostThread = new Thread(StartRegex);
            SearchHostThread.Start();
            try
            {
                core.recoverFile(variables.config + System.IO.Path.DirectorySeparatorChar + "seen.db");
                if (System.IO.File.Exists(variables.config + System.IO.Path.DirectorySeparatorChar + "seen.db"))
                {
                    lock (global)
                    {
                        global = new List<item>();
                        XmlDocument stat = new XmlDocument();
                        stat.Load(variables.config + System.IO.Path.DirectorySeparatorChar + "seen.db");
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
                                    global.Add(User);
                                }
                                catch (Exception fail)
                                {
                                    handleException(fail);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception f)
            {
                handleException(f);
            }
        }

        public static string FormatTimeSpan(TimeSpan ts) {
            string newTimeString = "";

            if (ts.Days != 0) {
                newTimeString += ts.Days.ToString() + "d";
            }

            if (ts.Hours != 0) {
                newTimeString += ts.Hours.ToString() + "h";
            }

            if (ts.Minutes != 0) {
                newTimeString += ts.Minutes.ToString() + "m";
            }

            return newTimeString + ts.Seconds.ToString() + "s";
        }
    }
}
