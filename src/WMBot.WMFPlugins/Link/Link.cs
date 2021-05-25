using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;

namespace wmib
{
    public class Link : Module
    {
        public static Dictionary<string, string> Wiki = new Dictionary<string, string>();

        public override bool Construct()
        {
            Version = new System.Version(1, 0, 0, 3);
            return true;
        }

        public static string URL2(string prefix, string Default)
        {
            string original = prefix;
            if (prefix.Contains(":"))
            {
                string link = prefix.Substring(prefix.IndexOf(":", System.StringComparison.InvariantCulture) + 1);
                if (!link.StartsWith("User:", System.StringComparison.InvariantCulture))
                {
                    link = "Template:" + link;
                }
                prefix = prefix.Substring(0, prefix.IndexOf(":", System.StringComparison.InvariantCulture));
                lock (Wiki)
                {
                    if (Wiki.ContainsKey(prefix))
                    {
                        return Wiki[prefix].Replace("$1", link);
                    }
                    if (Wiki.ContainsKey(Default))
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
            return "https://enwp.org/Template:" + original;
        }

        public static string URL(string prefix, string Default)
        {
            string original = prefix;
            if (prefix.Contains(":"))
            {
                string link = prefix.Substring(prefix.IndexOf(":", System.StringComparison.InvariantCulture) + 1);
                prefix = prefix.Substring(0, prefix.IndexOf(":", System.StringComparison.InvariantCulture));
                lock (Wiki)
                {
                    if (Wiki.ContainsKey(prefix))
                    {
                        return Wiki[prefix].Replace("$1", link);
                    }
                    if (Wiki.ContainsKey(Default))
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
            return "https://enwp.org/" + original;
        }

        private static string MakeTemplate(string text, string Default, bool Ignore)
        {
            if (text.Contains("{{"))
            {
                string link = text.Substring(text.IndexOf("{{", System.StringComparison.InvariantCulture) + 2);
                if (link.Contains("}}"))
                {
                    string second = link.Substring(link.IndexOf("}}", System.StringComparison.InvariantCulture) + 2);
                    if (second.Contains("{{"))
                    {
                        second = MakeLink(second, Default, Ignore);
                    }
                    else
                    {
                        second = null;
                    }
                    link = link.Substring(0, link.IndexOf("}}", System.StringComparison.InvariantCulture));
                    if (link.Contains("|"))
                    {
                        link = link.Substring(0, link.IndexOf("|", System.StringComparison.InvariantCulture));
                    }
                    link = HttpUtility.UrlEncode(link).Replace("%2f", "/")
                        .Replace("%3a", ":")
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
            if (text.Contains("[["))
            {
                string link = text.Substring(text.IndexOf("[[", System.StringComparison.InvariantCulture) + 2);
                if (link.Contains("]]"))
                {
                    string second = link.Substring(link.IndexOf("]]", System.StringComparison.InvariantCulture) + 2);
                    if (second.Contains("[["))
                    {
                        second = MakeLink(second, Default, Ignore);
                    }
                    else
                    {
                        second = null;
                    }
                    link = link.Substring(0, link.IndexOf("]]", System.StringComparison.InvariantCulture));
                    if (link.Contains("|"))
                    {
                        link = link.Substring(0, link.IndexOf("|", System.StringComparison.InvariantCulture));
                    }
                    link = HttpUtility.UrlEncode(link).Replace("%2f", "/")
                        .Replace("%3a", ":")
                        .Replace("%23", "#")
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
                if (message.Substring(message.IndexOf("{{", System.StringComparison.InvariantCulture) + 2).Contains("}}"))
                {
                    return true;
                }
            }
            if (message.Contains("[["))
            {
                if (message.Substring(message.IndexOf("[[", System.StringComparison.InvariantCulture) + 2).Contains("]]"))
                {
                    return true;
                }
            }
            return false;
        }

        private void linkie_off(CommandParams p)
        {
            if (GetConfig(p.SourceChannel, "Link.Enable", false))
            {
                SetConfig(p.SourceChannel, "Link.Enable", false);
                p.SourceChannel.SaveConfig();
                IRC.DeliverMessage(Localization.Localize("Linkie-Off", p.SourceChannel.Language), p.SourceChannel);
            }
            else
            {
                IRC.DeliverMessage(Localization.Localize("Linkie-Off2", p.SourceChannel.Language), p.SourceChannel);
            }
        }

        private void linkie_on(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "Link.Enable", false))
            {
                SetConfig(p.SourceChannel, "Link.Enable", true);
                p.SourceChannel.SaveConfig();
                IRC.DeliverMessage(Localization.Localize("Linkie-On", p.SourceChannel.Language), p.SourceChannel);
            }
            else
            {
                IRC.DeliverMessage(Localization.Localize("Linkie-On2", p.SourceChannel.Language), p.SourceChannel);
            }
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("linkie-on");
            UnregisterCommand("linkie-off");
            return base.Hook_OnUnload();
        }

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("linkie-on", this.linkie_on, false, "admin"));
            RegisterCommand(new GenericCommand("linkie-off", this.linkie_off, false, "admin"));
            return base.Hook_OnRegister();
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (message == Configuration.System.CommandPrefix + "link")
            {
                if (GetConfig(channel, "Link.Last", "") == "")
                {
                    IRC.DeliverMessage(Localization.Localize("Linkie-E1", channel.Language), channel);
                    return;
                }
                string xx = MakeTemplate(GetConfig(channel, "Link.Last", ""), GetConfig(channel, "Link.Default", "en"), false) + MakeLink(GetConfig(channel, "Link.Last", ""), GetConfig(channel, "Link.Default", "en"), true);
                if (xx != "")
                {
                    IRC.DeliverMessage(xx, channel);
                    return;
                }
                IRC.DeliverMessage(Localization.Localize("Linkie-E2", channel.Language), channel);
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "link ", System.StringComparison.InvariantCulture))
            {
                string link = message.Substring(6);
                string xx = MakeTemplate(link, GetConfig(channel, "Link.Default", "en"), false) + MakeLink(link, GetConfig(channel, "Link.Default", "en"), true);
                if (xx.Length > 0)
                {
                    IRC.DeliverMessage(xx, channel);
                    return;
                }
                IRC.DeliverMessage(Localization.Localize("Linkie-E3", channel.Language), channel);
                return;
            }

            if (GetConfig(channel, "Link.Enable", false))
            {
                string result = MakeTemplate(message, GetConfig(channel, "Link.Default", "en"), false) + MakeLink(message, GetConfig(channel, "Link.Default", "en"), true);
                if (result.Length > 0)
                {
                    IRC.DeliverMessage(result, channel);
                    return;
                }
            }

            if (ContainsLink(message))
            {
                SetConfig(channel, "Link.Last", message);
            }
        }

        public override bool Hook_SetConfig(Channel chan, libirc.UserInfo invoker, string config, string value)
        {
            if (config == "default-link-wiki")
            {
                if (value != "")
                {
                    SetConfig(chan, "Link.Default", value);
                    chan.SaveConfig();
                    IRC.DeliverMessage(Localization.Localize("configuresave", chan.Language, new List<string> { value, config }), chan.Name);
                    return true;
                }
                IRC.DeliverMessage(Localization.Localize("configure-va", chan.Language, new List<string> { config, value }), chan.Name);
                return true;
            }
            return false;
        }

        public override void Load()
        {
            Log("Loading db of links");
            if (!File.Exists(Variables.ConfigurationDirectory + "/linkie"))
            {
                Log("Unable to load " + Variables.ConfigurationDirectory + "/linkie aborting module", true);
                Exit();
                return;
            }
            lock (Wiki)
            {
                foreach (string line in File.ReadAllLines(Variables.ConfigurationDirectory + "/linkie"))
                {
                    if (line.Contains("|"))
                    {
                        string prefix = line.Substring(0, line.IndexOf("|", System.StringComparison.InvariantCulture));
                        string link = line.Substring(line.IndexOf("|", System.StringComparison.InvariantCulture) + 1);
                        if (!Wiki.ContainsKey(prefix))
                        {
                            Wiki.Add(prefix, link);
                        }
                    }
                }
            }
            try
            {
                while (IsWorking)
                {
                    Thread.Sleep(20000);
                }
            }
            catch (ThreadAbortException)
            {
            }
        }
    }
}
