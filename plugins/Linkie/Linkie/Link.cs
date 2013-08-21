using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace wmib
{
    public class Link : Module
    {
        public static Dictionary<string, string> Wiki = new Dictionary<string, string>();

        public override bool Construct()
        {
            start = true;
            Name = "Linkie-Bottie";
            Version = "1.0.0.0";
            return true;
        }

        public static string URL(string prefix, string Default)
        {
            if (prefix.Contains(":"))
            {
                lock (Wiki)
                {
                    prefix = prefix.Substring(0, prefix.IndexOf(":"));
                    if (Wiki.ContainsKey(prefix))
                    {
                        return Wiki[prefix].Replace("$1", "");
                    }
                    else if (Wiki.ContainsKey(Default))
                    {
                        return Wiki[Default].Replace("$1", "");
                    }
                }
            }
            return "https://enwp.org/";
        }

        private static string MakeLink(string text, string Default)
        {
            string link = "";
            if (text.Contains("[["))
            {
                link = text.Substring(text.IndexOf("[[") + 2);
                if (link.Contains("]]"))
                {
                    string second = link.Substring(link.IndexOf("]]") + 2);
                    if (second.Contains("[["))
                    {
                        second = MakeLink(second, Default);
                    }
                    else
                    {
                        second = null;
                    }
                    link = link.Substring(0, link.IndexOf("]]"));
                    link = System.Web.HttpUtility.UrlEncode(link).Replace("+", "_");
                    if (second != null)
                    {
                        return URL(link, Default) + link + " " + second;
                    }
                    return link;
                }
            }
            return "This string can't be converted to a wiki link";
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (message == config.CommandPrefix + "linkie-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "Link.Enable", false))
                    {
                        SetConfig(channel, "Link.Enable", false);
                        channel.instance.irc._SlowQueue.DeliverMessage("Links will not be automatically translated in this channel now", channel);
                    }
                    else
                    {
                        channel.instance.irc._SlowQueue.DeliverMessage("Links are already not automatically translated in this channel", channel);
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "linkie-on")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "Link.Enable", false))
                    {
                        SetConfig(channel, "Link.Enable", true);
                        channel.instance.irc._SlowQueue.DeliverMessage("Links will be automatically translated in this channel now", channel);
                    }
                    else
                    {
                        channel.instance.irc._SlowQueue.DeliverMessage("Links are already automatically translated in this channel", channel);
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "link "))
            {
                string link = message.Substring(6);
                core.irc._SlowQueue.DeliverMessage(MakeLink(link, GetConfig(channel, "Link.Default", "en")), channel);
                return;
            }

            if (GetConfig(channel, "Link.Enable", false))
            {
                if (message.Contains("[["))
                {
                    string link = message.Substring(message.IndexOf("[[") + 2);
                    if (link.Contains("]]"))
                    {
                        core.irc._SlowQueue.DeliverMessage(MakeLink(link, GetConfig(channel, "Link.Default", "en")), channel);
                        return;
                    }
                }
            }
        }

        public override void Load()
        {
            Log("Loading db of links");
            if (System.IO.File.Exists(variables.config + "/linkie"))
            {
                Log("Unable to load " + variables.config + "/linkie aborting module", true);
                Exit();
                return;
            }
            lock (Wiki)
            {
                foreach (string line in System.IO.File.ReadAllLines(variables.config + "/linkie"))
                {
                    if (line.Contains("|"))
                    {
                        string prefix = line.Substring(0, line.IndexOf("|"));
                        string link = line.Substring(line.IndexOf("|") + 1);
                        if (!Wiki.ContainsKey(prefix))
                        {
                            Wiki.Add(prefix, link);
                        }
                    }
                }
            }
            try
            {
                while (working)
                {
                    Thread.Sleep(200);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }
    }
}
