using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Text;

namespace wmib
{
    public class RegularModule : Module
    {
        public static readonly string NAME = "Statistics";
        public override bool Hook_OnRegister()
        {
            bool success = true;
            lock (config.channels)
            {
                foreach (config.channel xx in config.channels)
                {
                    if (!xx.RegisterObject(new Statistics(xx), NAME))
                    {
                        success = false;
                    }
                }
            }
            return success;
        }

        public override bool Construct()
        {
            Name = NAME;
            start = true;
            Version = "1.0.22";
            return true;
        }

        public override string Extension_DumpHtml(config.channel channel)
        {
            string HTML = "";
            Statistics list = (Statistics)channel.RetrieveObject(NAME);
            if (list != null)
            {
                HTML += "\n<br>\n<h4>Most active users :)</h4>\n<br>\n\n<table class=\"infobot\" width=100% border=1>";
                HTML += "<tr><td>N.</td><th>Nick</th><th>Messages (average / day)</th><th>Number of posted messages</th><th>Active since</th></tr>";
                int id = 0;
                int totalms = 0;
                DateTime startime = DateTime.Now;
                lock (list.data)
                {
                    list.data.Sort();
                    list.data.Reverse();
                    foreach (Statistics.list user in list.data)
                    {
                        id++;
                        totalms += user.messages;
                        if (id > 100)
                        {
                            continue;
                        }
                        if (startime > user.logging_since)
                        {
                            startime = user.logging_since;
                        }
                        System.TimeSpan uptime = System.DateTime.Now - user.logging_since;
                        float average = user.messages;
                        average = ((float)user.messages / (float)(uptime.Days + 1));
                        if (user.URL != "")
                        {
                            HTML += "<tr><td>" + id.ToString() + ".</td><td><a target=\"_blank\" href=\"" + user.URL + "\">" + user.user + "</a></td><td>" + average.ToString() + "</td><td>" + user.messages.ToString() + "</td><td>" + user.logging_since.ToString() + "</td></tr>";
                        }
                        else
                        {
                            HTML += "<tr><td>" + id.ToString() + ".</td><td>" + user.user + "</td><td>" + average.ToString() + "</td><td>" + user.messages.ToString() + "</td><td>" + user.logging_since.ToString() + "</td></tr>";
                        }
                        HTML += "  \n";
                    }
                }
                System.TimeSpan uptime_total = System.DateTime.Now - startime;
                float average2 = totalms;
                average2 = (float)totalms / (1 + uptime_total.Days);
                HTML += "<tr><td>N/A</td><th>Total:</th><th>" + average2.ToString() + "</th><th>" + totalms.ToString() + "</th><td>N/A</td></tr>";
                HTML += "  \n";
                HTML += "</table>";
            }
            return HTML;
        }

        public override void Hook_Channel(config.channel channel)
        {
            if (channel.RetrieveObject("Statistics") == null)
            {
                channel.RegisterObject(new Statistics(channel), NAME);
            }
        }

        public override bool Hook_OnUnload()
        {
            bool success = true;
            lock (config.channels)
            {
                foreach (config.channel xx in config.channels)
                {
                    if (!xx.UnregisterObject(NAME))
                    {
                        success = false;
                    }
                }
            }
            return success;
        }

        public override void Load()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        foreach (config.channel chan in config.channels)
                        {
                            Statistics st = (Statistics)chan.RetrieveObject(NAME);
                            if (st != null)
                            {
                                if (st.Stored == false)
                                {
                                    st.Save();
                                }
                                st.Stored = true;
                                continue;
                            }
                            core.Log("NULL pointer at statistics for " + chan.Name, true);
                        }
                        Thread.Sleep(8000);
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception f)
                    {
                        core.handleException(f);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (Module.GetConfig(channel, "Statistics.Enabled", false))
            {
                Statistics st = (Statistics)channel.RetrieveObject("Statistics");
                if (st != null)
                {
                    st.Stat(invoker.Nick, message, invoker.Host);
                }
            }

            if (message == "@statistics-off")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!Module.GetConfig(channel, "Statistics.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("StatE2", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        Module.SetConfig(channel, "Statistics.Enabled", false);
                        channel.SaveConfig();
                        core.irc._SlowQueue.DeliverMessage(messages.get("Stat-off", channel.Language), channel.Name);
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@statistics-reset")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    Statistics st = (Statistics)channel.RetrieveObject("Statistics");
                    if (st != null)
                    {
                        st.Delete();
                    }
                    core.irc._SlowQueue.DeliverMessage(messages.get("Statdt", channel.Language), channel.Name);
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@statistics-on")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (Module.GetConfig(channel, "Statistics.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("StatE1", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        Module.SetConfig(channel, "Statistics.Enabled", true);
                        channel.SaveConfig();
                        core.irc._SlowQueue.DeliverMessage(messages.get("Stat-on", channel.Language), channel.Name);
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
    }

    public class Statistics
    {
        public config.channel channel;
        public bool enabled = true;
        public bool changed = false;
        public bool Stored = false;
        public static Thread db;

        public class list : IComparable
        {
            public string user;
            public int messages = 0;
            public int longest_message = 0;
            public int average_message;
            public DateTime logging_since;
            public string URL = "";

            public int CompareTo(object O)
            {
                if (O is list)
                {
                    return this.messages.CompareTo((O as list).messages);
                }
                return 0;
            }
        }

        public List<list> data;

        public Statistics(config.channel _channel)
        {
            data = new List<list>();
            channel = _channel;
            Load();
        }

        public void Stat(string nick, string message, string host)
        {
            if (Module.GetConfig(channel, "Statistics.Enabled", false))
            {
                list user = null;
                lock (data)
                {
                    foreach (list item in data)
                    {
                        if (nick.ToUpper() == item.user.ToUpper())
                        {
                            user = item;
                            break;
                        }
                    }
                }
                if (user == null)
                {
                    user = new list();
                    user.user = nick;
                    user.logging_since = DateTime.Now;
                    lock (data)
                    {
                        data.Add(user);
                    }
                }
                user.URL = core.Host.Host2Name(host);
                user.messages++;
                Module.SetConfig(channel, "HTML.Update", true);
                changed = true;
                Stored = false;
            }
        }

        public void Delete()
        {
            data = new List<list>();
            Save();
        }

        public bool Save()
        {
            XmlDocument stat = new XmlDocument();
            XmlNode xmlnode = stat.CreateElement("channel_stat");

            foreach (list curr in data)
            {
                XmlAttribute name = stat.CreateAttribute("username");
                name.Value = curr.user;
                XmlAttribute messages = stat.CreateAttribute("messages");
                messages.Value = curr.messages.ToString();
                XmlAttribute longest_message = stat.CreateAttribute("longest_message");
                longest_message.Value = "0";
                XmlAttribute logging_since = stat.CreateAttribute("logging_since");
                logging_since.Value = curr.logging_since.ToBinary().ToString();
                XmlAttribute link = stat.CreateAttribute("link");
                link.Value = curr.URL;
                XmlNode db = stat.CreateElement("user");
                db.Attributes.Append(name);
                db.Attributes.Append(messages);
                db.Attributes.Append(longest_message);
                db.Attributes.Append(logging_since);
                db.Attributes.Append(link);
                xmlnode.AppendChild(db);
            }
            stat.AppendChild(xmlnode);
            if (System.IO.File.Exists(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics"))
            {
                core.backupData(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics");
            }
            stat.Save(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics");
            if (System.IO.File.Exists(config.tempName(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics")))
            {
                System.IO.File.Delete(config.tempName(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics"));
            }
            return false;
        }

        public bool Load()
        {
            try
            {
                core.recoverFile(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics", channel.Name);
                if (System.IO.File.Exists(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics"))
                {
                    lock (data)
                    {
                        data = new List<list>();
                        XmlDocument stat = new XmlDocument();
                        stat.Load(variables.config + System.IO.Path.DirectorySeparatorChar + channel.Name + ".statistics");
                        if (stat.ChildNodes[0].ChildNodes.Count > 0)
                        {
                            foreach (XmlNode curr in stat.ChildNodes[0].ChildNodes)
                            {
                                list item = new list();
                                item.user = curr.Attributes[0].Value;
                                item.messages = int.Parse(curr.Attributes[1].Value);
                                item.logging_since = DateTime.FromBinary(long.Parse(curr.Attributes[3].Value));
                                if (curr.Attributes.Count > 4)
                                {
                                    item.URL = curr.Attributes[4].Value;
                                }
                                data.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception f)
            {
                core.handleException(f);
            }
            return false;
        }
    }
}
