using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

namespace wmib
{
    public class RequestsPlugin : Module
    {
		/// <summary>
		/// This is a reference / pointer to channel object where we want to report
		/// the requests to if it exists
		/// </summary>
        public Channel pRequestsChannel = null;
		private Thread PendingRequests = null;
		private List<string> WaitingRequests = new List<string>();
		public static readonly string RequestChannel = "#wikimedia-labs-requests";

        public override bool Construct()
        {
            Name = "Requests";
            Version = "1.20.0";
            return true;
        }

        private static ArrayList getWaitingUsernames(string categoryName, string usernamePrintout)
        {
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "wm-bot (https://meta.wikimedia.org/wiki/Wm-bot)");

            // TODO: When wikitech is updated to SMW 1.9, use
            // "action=askargs" and add "?Modification date#ISO" to
            // the printouts to calculate "Waiting for x minutes"
            // data.
            string Url = "https://wikitech.wikimedia.org/w/api.php" + "?action=ask" + "&query=" + 
				Uri.EscapeUriString("[[Category:" + categoryName + "]] [[Is Completed::No]]|?" + 
				                    usernamePrintout) + "&format=wddx";

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

        private static string formatReportLine(ArrayList usernames, string requestedAccess)
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

        private static Boolean displayWaiting(Boolean reportNoUsersWaiting)
        {
            ArrayList shellRequests = getWaitingUsernames("Shell Access Requests", "Shell Request User Name");
            ArrayList toolsRequests = getWaitingUsernames("Tools Access Requests", "Tools Request User Name");

            if (shellRequests.Count != 0 || reportNoUsersWaiting)
                Core.irc.Queue.DeliverMessage(formatReportLine(shellRequests, "shell access"), RequestChannel);
            if (toolsRequests.Count != 0 || reportNoUsersWaiting)
                Core.irc.Queue.DeliverMessage(formatReportLine(toolsRequests, "Tools access"), RequestChannel);

            return shellRequests.Count != 0 || toolsRequests.Count != 0;
        }

		private void Run()
		{
			try
            {
				while (this.IsWorking)
				{
					List<string> requests = new List<string>();
					// first copy all requests so that we don't keep the array locked for too long
					// because it can be locked by main thread, we need to acquire the lock for shortest time
					lock (this.WaitingRequests)
					{
						requests.AddRange(this.WaitingRequests);
						this.WaitingRequests.Clear();
					}

					foreach (string channel in requests)
					{
						// TODO: here we should implement the channel parameter so that we could use this module
						// in more channels than one
						displayWaiting(true);
					}
					Thread.Sleep(600);
				}
			}
			catch (Exception fail)
            {
                HandleException(fail);
            }
		}

        public override void Load()
        {
            try
            {
                // TODO: Install CA certificate used by wikitech to
                // Mono.
                ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => { return true; };

                pRequestsChannel = Core.GetChannel(RequestChannel);
                if (pRequestsChannel == null)
                {
                    Log("CRITICAL: the bot isn't in " + RequestChannel + " unloading requests", true);
                    return;
                }

				this.PendingRequests = new Thread(Run);
				this.PendingRequests.Name = "Pending queries thread for requests extension";
				this.PendingRequests.Start();

                Thread.Sleep(60000);
                while (this.IsWorking)
                {
                    if (GetConfig(pRequestsChannel, "Requests.Enabled", false) && displayWaiting(false))
                        Thread.Sleep(800000);
                    else
                        Thread.Sleep(20000);
                }
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        public override void Hook_PRIV(Channel channel, User invoker, string message)
        {
            if (channel.Name != RequestChannel)
            {
                return;
            }

            if (message == "@requests-off")
            {
                if (channel.SystemUsers.IsApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!GetConfig(channel, "Requests.Enabled", false))
                    {
                        Core.irc.Queue.DeliverMessage("Requests are already disabled", channel.Name);
                        return;
                    }
                    else
                    {
                        Core.irc.Queue.DeliverMessage("Requests were disabled", channel.Name, IRC.priority.high);
                        SetConfig(channel, "Requests.Enabled", false);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@requests-on")
            {
                if (channel.SystemUsers.IsApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (GetConfig(channel, "Requests.Enabled", false))
                    {
                        Core.irc.Queue.DeliverMessage("Requests system is already enabled", channel.Name);
                        return;
                    }
                    SetConfig(channel, "Requests.Enabled", true);
                    channel.SaveConfig();
                    Core.irc.Queue.DeliverMessage("Requests were enabled", channel.Name, IRC.priority.high);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == "@requests")
			{
				if (!GetConfig(channel, "Requests.Enabled", false))
                {
					Core.irc.Queue.DeliverMessage("You need to enable requests in this channel for this command to work", channel.Name);
					return;
				}
                lock (this.WaitingRequests)
				{
					if (this.WaitingRequests.Contains(channel.Name))
				    {
						Core.irc.Queue.DeliverMessage("I am already fetching the list of waiting users for this channel", channel.Name);
						return;
					} else
					{
						Core.irc.Queue.DeliverMessage("I am fetching the list of waiting users...", channel.Name);
					}

					this.WaitingRequests.Add(channel.Name);
				}
                return;
            }
        }
    }
}
