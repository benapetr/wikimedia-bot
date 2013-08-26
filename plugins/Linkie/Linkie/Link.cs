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
            Version = "1.0.0.0";
            Name = "Linkie-Bottie";
            start = true;
            return true;
        }

        public static string URL2(string prefix, string Default)
        {
            string original = prefix;
            string link = prefix;
            if (prefix.Contains(":"))
            {
                link = prefix.Substring(prefix.IndexOf(":") + 1);
                if (!link.StartsWith("User:"))
                {
                    link = "Template:" + link;
                }
                prefix = prefix.Substring(0, prefix.IndexOf(":"));
                lock (Wiki)
                {
                    if (Wiki.ContainsKey(prefix))
                    {
                        return Wiki[prefix].Replace("$1", link);
                    }
                    else if (Wiki.ContainsKey(Default))
                    {
                        return Wiki[Default].Replace("$1", prefix + ":" + link);
                    }
                }
            }
            lock (Wiki)
            {
                if (Wiki.ContainsKey(Default))
                {
                    return Wiki[Default].Replace("$1", "Template:" + original);
                }
            }
            return "http://enwp.org/Template:" + original;
        }

        public static string URL(string prefix, string Default)
        {
            string link = prefix;
            string original = prefix;
            if (prefix.Contains(":"))
            {
                link = prefix.Substring(prefix.IndexOf(":") + 1);
                prefix = prefix.Substring(0, prefix.IndexOf(":"));
                lock (Wiki)
                {
                    if (Wiki.ContainsKey(prefix))
                    {
                        return Wiki[prefix].Replace("$1", link);
                    }
                    else if (Wiki.ContainsKey(Default))
                    {
                        return Wiki[Default].Replace("$1", original);
                    }
                }
            }
            lock (Wiki)
            {
                if (Wiki.ContainsKey(Default))
                {
                    return Wiki[Default].Replace("$1", original);
                }
            }
            return "http://enwp.org/" + original;
        }

        private static string MakeTemplate(string text, string Default, bool Ignore)
        {
            string link = "";
            if (text.Contains("{{"))
            {
                link = text.Substring(text.IndexOf("{{") + 2);
                if (link.Contains("}}"))
                {
                    string second = link.Substring(link.IndexOf("}}") + 2);
                    if (second.Contains("{{"))
                    {
                        second = MakeLink(second, Default, Ignore);
                    }
                    else
                    {
                        second = null;
                    }
                    link = link.Substring(0, link.IndexOf("}}"));
                    if (link.Contains("|"))
                    {
                        link = link.Substring(0, link.IndexOf("|"));
                    }
                    link = System.Web.HttpUtility.UrlEncode(link).Replace("%2f", "/")
                        .Replace("%3a", ":")
                        .Replace("%28", "#")
                        .Replace("+", "_");
                    if (second != null)
                    {
                        return URL2(link, Default) + " " + second;
                    }
                    return URL2(link, Default) + " ";
                }
            }
            return "";
        }

        private static string MakeLink(string text, string Default, bool Ignore)
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
                        second = MakeLink(second, Default, Ignore);
                    }
                    else
                    {
                        second = null;
                    }
                    link = link.Substring(0, link.IndexOf("]]"));
                    if (link.Contains("|"))
                    {
                        link = link.Substring(0, link.IndexOf("|"));
                    }
                    link = System.Web.HttpUtility.UrlEncode(link).Replace("%2f", "/")
                        .Replace("%3a", ":")
                        .Replace("+", "_");
                    if (second != null)
                    {
                        return URL(link, Default) + " " + second;
                    }
                    return URL(link, Default);
                }
            }
            return "";
        }

        public static bool ContainsLink(string message)
        {
            if (message.Contains("{{"))
            {
                if (message.Substring(message.IndexOf("{{") + 2).Contains("}}"))
                {
                    return true;
                }
            }
            if (message.Contains("[["))
            {
                if (message.Substring(message.IndexOf("[[") + 2).Contains("]]"))
                {
                    return true;
                }
            }
            return false;
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
                        channel.SaveConfig();
                        channel.instance.irc._SlowQueue.DeliverMessage(messages.get("Linkie-Off", channel.Language), channel);
                    }
                    else
                    {
                        channel.instance.irc._SlowQueue.DeliverMessage(messages.get("Linkie-Off2", channel.Language), channel);
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
                        channel.SaveConfig();
                        channel.instance.irc._SlowQueue.DeliverMessage(messages.get("Linkie-On", channel.Language), channel);
                    }
                    else
                    {
                        channel.instance.irc._SlowQueue.DeliverMessage(messages.get("Linkie-On2", channel.Language), channel);
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "link")
            {
                if (GetConfig(channel, "Link.Last", "") == "")
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("Linkie-E1", channel.Language), channel);
                    return;
                }
                string xx = MakeTemplate(GetConfig(channel, "Link.Last", ""), GetConfig(channel, "Link.Default", "en"), false) + MakeLink(GetConfig(channel, "Link.Last", ""), GetConfig(channel, "Link.Default", "en"), true);
                if (xx != "")
                {
                    core.irc._SlowQueue.DeliverMessage(xx, channel);
                    return;
                }
                core.irc._SlowQueue.DeliverMessage(messages.get("Linkie-E2", channel.Language), channel);
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "link "))
            {
                string link = message.Substring(6);
                string xx = MakeTemplate(link, GetConfig(channel, "Link.Default", "en"), false) + MakeLink(link, GetConfig(channel, "Link.Default", "en"), true);
                if (xx != "")
                {
                    core.irc._SlowQueue.DeliverMessage(xx, channel);
                    return;
                }
                core.irc._SlowQueue.DeliverMessage(messages.get("Linkie-E3", channel.Language), channel);
                return;
            }

            if (GetConfig(channel, "Link.Enable", false))
            {
                string result = MakeTemplate(message, GetConfig(channel, "Link.Default", "en"), false) + MakeLink(message, GetConfig(channel, "Link.Default", "en"), true);
                if (result != "")
                {
                    core.irc._SlowQueue.DeliverMessage(result, channel);
                    return;
                }
            }

            if (ContainsLink(message))
            {
                SetConfig(channel, "Link.Last", message);
            }
        }

        public override bool Hook_SetConfig(config.channel chan, User invoker, string config, string value)
        {
            if (config == "default_link_wiki")
            {
                if (value != "")
                {
                    SetConfig(chan, "Link.Default", value);
                    chan.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage(messages.get("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                    return true;
                }
                core.irc._SlowQueue.DeliverMessage(messages.get("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                return true;
            }
            return false;
        }

        public override void Load()
        {
            Log("Loading db of links");
            if (!System.IO.File.Exists(variables.config + "/linkie"))
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
                    Thread.Sleep(20000);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }
    }
}
