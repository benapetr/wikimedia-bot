using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

namespace wmib
{
    public class RequestLabs
    {
        public string user = null;
        public DateTime time;
        public static readonly string RequestCh = "#wikimedia-labs-requests";
    }

    public class RegularModule : Module
    {
        public Thread notifications = null;
        public config.channel ch = null;

        public override bool Construct()
        {
            Name = "Requests";
            start = true;
            Version = "1.10.0";
            return true;
        }

        static ArrayList getWaitingUsernames(string categoryName, string usernamePrintout)
        {
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "wm-bot (https://meta.wikimedia.org/wiki/Wm-bot)");

            // TODO: When wikitech is updated to SMW 1.9, use
            // "action=askargs" and add "?Modification date#ISO" to
            // the printouts to calculate "Waiting for x minutes"
            // data.
            string Url = "https://wikitech.wikimedia.org/w/api.php" +
                         "?action=ask" +
                         "&query=" + Uri.EscapeUriString("[[Category:" + categoryName + "]] [[Is Completed::No]]|?" + usernamePrintout) +
                         "&format=wddx";

            // Get the query results.
            string Result = client.DownloadString(Url);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Result);

            // Fill ArrayList.
            ArrayList r = new ArrayList();
            foreach (XmlElement r1 in doc.SelectNodes("//var[@name='results']/struct/var"))
            {
                string username = r1.SelectNodes("struct/var[@name = 'printouts']/struct/var[@name = '" + usernamePrintout + "']/array/string").Item(0).InnerText;
                r.Add(username);
            }

            return r;
        }

        static string formatReportLine(ArrayList usernames, string requestedAccess)
        {
            int displayed = 0;
            string info = "";

            foreach (string username in usernames)
            {
                if (info != "")
                    info += ", ";
                info += username;   // TODO: Add " (waiting " + (time since Modification_date) + ")".
                displayed++;
                if (info.Length > 160)
                    break;
            }

            if (usernames.Count == 0)
                info = "There are no users waiting for " + requestedAccess + ".";
            else if (usernames.Count == 1)
                info = "There is one user waiting for " + requestedAccess + ": " + info + ".";
            else if (displayed == usernames.Count)
                info = "There are " + usernames.Count.ToString() + " users waiting for " + requestedAccess + ": " + info + ".";
            else
                info = "There are " + usernames.Count.ToString() + " users waiting for " + requestedAccess + ", displaying last " + displayed.ToString() + ": " + info + ".";

            return info;
        }

        static Boolean displayWaiting(Boolean reportNoUsersWaiting)
        {
            ArrayList shellRequests = getWaitingUsernames("Shell Access Requests", "Shell Request User Name");
            ArrayList toolsRequests = getWaitingUsernames("Tools Access Requests", "Tools Request User Name");

            if (shellRequests.Count != 0 || reportNoUsersWaiting)
                core.irc._SlowQueue.DeliverMessage(formatReportLine(shellRequests, "shell access"), RequestLabs.RequestCh);
            if (toolsRequests.Count != 0 || reportNoUsersWaiting)
                core.irc._SlowQueue.DeliverMessage(formatReportLine(toolsRequests, "Tools access"), RequestLabs.RequestCh);

            return shellRequests.Count != 0 || toolsRequests.Count != 0;
        }

        public void Run()
        {
            try
            {
                Thread.Sleep(60000);
                while (true)
                {
                    if (GetConfig(ch, "Requests.Enabled", false) && displayWaiting(false))
                        Thread.Sleep(800000);
                    else
                        Thread.Sleep(20000);
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

        public override void Load()
        {
            try
            {
                // TODO: Install CA certificate used by wikitech to
                // Mono.
                ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => { return true; };

                ch = core.getChannel(RequestLabs.RequestCh);
                if (ch == null)
                {
                    Log("CRITICAL: the bot isn't in " + RequestLabs.RequestCh + " unloading requests", true);
                    return;
                }
                RequestCache.Load();
                notifications = new Thread(Run);
                notifications.Start();
            }
            catch (Exception fail)
            {
                handleException(fail);
                notifications.Abort();
            }
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (channel.Name != RequestLabs.RequestCh)
            {
                return;
            }

            if (message == "@requests-off")
            {
                if (channel.Users.IsApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!GetConfig(channel, "Requests.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Requests are already disabled", channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage("Requests were disabled", channel.Name, IRC.priority.high);
                        SetConfig(channel, "Requests.Enabled", false);
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

            if (message == "@requests-on")
            {
                if (channel.Users.IsApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (GetConfig(channel, "Requests.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Requests system is already enabled", channel.Name);
                        return;
                    }
                    SetConfig(channel, "Requests.Enabled", true);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage("Requests were enabled", channel.Name, IRC.priority.high);
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@requests")
            {
                displayWaiting(true);

                return;
            }
        }
    }
}
