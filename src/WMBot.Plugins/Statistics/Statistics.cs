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
using System.Text;
using System.Threading;
using System.Xml;

namespace wmib.Extensions
{
    public class StatisticsMod : Module
    {
        public static readonly string NAME = "Statistics";
        public override bool Hook_OnRegister()
        {
            bool success = true;
            lock (Configuration.Channels)
            {
                foreach (Channel xx in Configuration.Channels)
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
            Version = new Version(1, 0, 28, 0);
            return true;
        }

        public override string Extension_DumpHtml(Channel channel)
        {
            StringBuilder builder = new StringBuilder();
            if (GetConfig(channel, "Statistics.Enabled", false))
            {
                Statistics list = (Statistics) channel.RetrieveObject(NAME);
                if (list != null)
                {
                    builder.AppendLine("<br />");
                    builder.AppendLine("<h4>Most active users :)</h4>");
                    builder.AppendLine("<br />");
                    builder.AppendLine();
                    builder.AppendLine("<table class=\"infobot\" width=100% border=1>");
                    builder.AppendLine(
                        "<tr><td>N.</td><th>Nick</th><th>Messages (average / day)</th><th>Number of posted messages</th><th>Active since</th></tr>");
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
                            TimeSpan uptime = DateTime.Now - user.logging_since;
                            float average = (user.messages/(float) (uptime.Days + 1));
                            if (user.URL != "")
                            {
                                builder.AppendFormat(
                                    "<tr><td>{0}.</td><td><a target=\"_blank\" href=\"{1}\">{2}</a></td><td>{3}</td><td>{4}</td><td>{5}</td></tr>\n",
                                    id, user.URL, user.user, average, user.messages, user.logging_since
                                    );
                            }
                            else
                            {
                                builder.AppendFormat("<tr><td>{0}.</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>\n",
                                                   id, user.user, average, user.messages, user.logging_since);
                            }
                            builder.AppendLine();
                        }
                    }
                    TimeSpan uptime_total = DateTime.Now - startime;
                    float average2 = (float) totalms/(1 + uptime_total.Days);
                    builder.AppendFormat("<tr><td>N/A</td><th>Total:</th><th>{0}</th><th>{1}</th><td>N/A</td></tr>\n", average2, totalms);
                    builder.AppendLine();
                    builder.AppendLine("</table>");
                }
            }
            return builder.ToString();
        }

        public override void Hook_Channel(Channel channel)
        {
            if (channel.RetrieveObject("Statistics") == null)
            {
                channel.RegisterObject(new Statistics(channel), NAME);
            }
        }

        public override bool Hook_OnUnload()
        {
            bool success = true;
            lock (Configuration.Channels)
            {
                foreach (Channel xx in Configuration.Channels)
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
            while (Core.IsRunning)
            {
                Thread.Sleep(8000);
                try
                {
                    lock (Configuration.Channels)
                    {
                        foreach (Channel chan in Configuration.Channels)
                        {
                            Statistics st = (Statistics)chan.RetrieveObject(NAME);
                            if (st != null)
                            {
                                if (st.Stored == false)
                                {
                                    st.Save();
                                }
                                st.Stored = true;
                            }
                        }
                    }
                    Thread.Sleep(8000);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception f)
                {
                    HandleException(f);
                }
            }
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (GetConfig(channel, "Statistics.Enabled", false))
            {
                Statistics st = (Statistics)channel.RetrieveObject("Statistics");
                if (st != null)
                {
                    st.Stat(invoker.Nick, message, invoker.Host);
                }
            }

            if (message == Configuration.System.CommandPrefix + "statistics-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "Statistics.Enabled", false))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("StatE2", channel.Language), channel);
                        return;
                    }
                    SetConfig(channel, "Statistics.Enabled", false);
                    channel.SaveConfig();
                    Core.irc.Queue.DeliverMessage(messages.Localize("Stat-off", channel.Language), channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "statistics-reset")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    Statistics st = (Statistics)channel.RetrieveObject("Statistics");
                    if (st != null)
                    {
                        st.Delete();
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("Statdt", channel.Language), channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "statistics-on")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "Statistics.Enabled", false))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("StatE1", channel.Language), channel);
                        return;
                    }
                    SetConfig(channel, "Statistics.Enabled", true);
                    channel.SaveConfig();
                    Core.irc.Queue.DeliverMessage(messages.Localize("Stat-on", channel.Language), channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
            }
        }
    }

    public class Statistics
    {
        public Channel channel;
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
                    return messages.CompareTo((O as list).messages);
                }
                return 0;
            }
        }

        public List<list> data;

        public Statistics(Channel _channel)
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
                    user = new list {user = nick, logging_since = DateTime.Now};
                    lock (data)
                    {
                        data.Add(user);
                    }
                }
                user.URL = Core.Host.Host2Name(host);
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

            lock(data)
            {
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
                    XmlNode userNode = stat.CreateElement("user");
                    userNode.Attributes.Append(name);
                    userNode.Attributes.Append(messages);
                    userNode.Attributes.Append(longest_message);
                    userNode.Attributes.Append(logging_since);
                    userNode.Attributes.Append(link);
                    xmlnode.AppendChild(userNode);
                }
            }
            stat.AppendChild(xmlnode);
            if (File.Exists(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".statistics"))
            {
                Core.BackupData(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".statistics");
            }
            stat.Save(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".statistics");
            if (File.Exists(Configuration.TempName(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".statistics")))
            {
                File.Delete(Configuration.TempName(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".statistics"));
            }
            return false;
        }

        public bool Load()
        {
            try
            {
                Core.RecoverFile(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".statistics", channel.Name);
                if (File.Exists(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".statistics"))
                {
                    lock (data)
                    {
                        data = new List<list>();
                        XmlDocument stat = new XmlDocument();
                        stat.Load(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + channel.Name + ".statistics");
                        if (stat.ChildNodes[0].ChildNodes.Count > 0)
                        {
                            foreach (XmlNode curr in stat.ChildNodes[0].ChildNodes)
                            {
                                list item = new list
                                {
                                    user = curr.Attributes[0].Value,
                                    messages = int.Parse(curr.Attributes[1].Value),
                                    logging_since = DateTime.FromBinary(long.Parse(curr.Attributes[3].Value))
                                };
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
                Core.HandleException(f, "statistics");
            }
            return false;
        }
    }
}
