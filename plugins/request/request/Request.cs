using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;
using DotNetWikiBot;

namespace wmib
{
    public class RequestLabs
    {
        public string user = null;
        public DateTime time;
        public static readonly string RequestCh = "#wikimedia-labs-requests";

        public RequestLabs(string us)
        {
            time = DateTime.Now;
            user = us;
        }

        public string WaitingTime()
        {
            string text = (DateTime.Now - time).TotalMinutes.ToString();
            if (text.Contains("."))
            {
                text = text.Substring(0, text.IndexOf("."));
            }
            return text + " minutes";
        }
    }

    public class RegularModule : Module
    {
        public static List<RequestLabs> Shell = new List<RequestLabs>();
        public static List<RequestLabs> Tools = new List<RequestLabs>();
        public Thread notifications = null;
        public config.channel ch = null;

        public RequestLabs Contains(string user)
        {
            lock (Shell)
            {
                foreach (RequestLabs r in Shell)
                {
                    if (r.user == user)
                    {
                        return r;
                    }
                }
            }
            return null;
        }

        

        public RequestLabs getUser(string user, List<RequestLabs> list)
        {
            foreach (RequestLabs r in list)
            {
                if (r.user == user)
                {
                    return r;
                }
            }
            return null;
        }

        public override bool Construct()
        {
            Name = "Requests";
            start = true;
            Version = "1.10.0";
            return true;
        }

        public RequestLabs ContainsLabs(string user)
        {
            lock (Tools)
            {
                foreach (RequestLabs r in Tools)
                {
                    if (r.user == user)
                    {
                        return r;
                    }
                }
            }
            return null;
        }

        public void DisplayWaiting()
        {
            if (Shell.Count > 0)
            {
                int requestCount = Shell.Count;
                int displayed = 0;
                string info = "";
                foreach (RequestLabs u in Shell)
                {
                    displayed++;
                    info += u.user + " (waiting " + u.WaitingTime() + ") ";
                    if (info.Length > 160)
                    {
                        break;
                    }
                }
                if (displayed == 1)
                {
                    info = "Warning: There is " + requestCount.ToString() + " user waiting for shell: " + info;
                }
                else
                {
                    info = "Warning: There are " + requestCount.ToString() + " users waiting for shell, displaying last " + displayed.ToString() + ": " + info;
                }
                core.irc._SlowQueue.DeliverMessage(info, RequestLabs.RequestCh);
            }

            if (Tools.Count > 0)
            {
                int requestCount = Tools.Count;
                int displayed = 0;
                string info = "";
                foreach (RequestLabs u in Tools)
                {
                    displayed++;
                    info += u.user + " (waiting " + u.WaitingTime() + ") ";
                    if (info.Length > 160)
                    {
                        break;
                    }
                }
                if (displayed == 1)
                {
                    info = "Warning: There is " + requestCount.ToString() + " user waiting for access to tools project: " + info;
                }
                else
                {
                    info = "Warning: There are " + requestCount.ToString() + " users waiting for access to tools project, displaying last " + displayed.ToString() + ": " + info;
                }
                core.irc._SlowQueue.DeliverMessage(info, RequestLabs.RequestCh);
            }
        }

        public void Run()
        {
            try
            {
                Thread.Sleep(60000);
                while (true)
                {
                    if (GetConfig(ch, "Requests.Enabled", false))
                    {
                        lock (Shell)
                        {
                            DisplayWaiting();
                        }
                    }
                    if (Shell.Count > 0 || Tools.Count > 0)
                    {
                        Thread.Sleep(800000);
                    }
                    else
                    {
                        Thread.Sleep(20000);
                    }
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

        public bool Matches(string text)
        {
            if (text.Contains("|Completed=No") || text.Contains("|Completed=false") || text.Contains("|Completed=no") || text.Contains("|Completed=False"))
            {
                return true;
            }
            return false;
        }

        public override void Load()
        {
            try
            {
                ch = core.getChannel(RequestLabs.RequestCh);
                if (ch == null)
                {
                    Log("CRITICAL: the bot isn't in " + RequestLabs.RequestCh + " unloading requests", true);
                    return;
                }
                RequestCache.Load();
                notifications = new Thread(Run);
                notifications.Start();
                Site wikitech = new Site("https://wikitech.wikimedia.org", "wmib", "");
                while (true)
                {
                    try
                    {
                        List<string> shell = new List<string>();
                        List<string> tooldata = new List<string>();
                        PageList requests = new PageList(wikitech);
                        requests.FillAllFromCategory("Shell Access Requests");
                        foreach (Page page in requests)
                        {
                            string title = page.title.Replace("Shell Request/", "");
                            if (RequestCache.Contains(title))
                            {
                                continue;
                            }
                            page.Load();
                            if (!Matches(page.text))
                            {
                                RequestCache.Insert(title);
                                lock (Shell)
                                {
                                    // this one was already processed
                                    RequestLabs previous = Contains(title);
                                    if (previous != null)
                                    {
                                        Shell.Remove(previous);
                                    }
                                }
                                continue;
                            }
                            else
                            {
                                if (!shell.Contains(title))
                                {
                                    shell.Add(title);
                                }
                                lock (Shell)
                                {
                                    if (Contains(title) == null)
                                    {
                                        Shell.Add(new RequestLabs(title));
                                    }
                                }
                            }
                        }

                        requests = new PageList(wikitech);
                        requests.FillAllFromCategory("Tools_Access_Requests");
                        foreach (Page page in requests)
                        {
                            string title = page.title.Replace("Nova Resource:Tools/Access Request/", "");
                            if (RequestCache.ContainsLabs(title))
                            {
                                continue;
                            }
                            page.Load();
                            if (!(Matches(page.text)))
                            {
                                RequestCache.InsertLabs(title);
                                lock (Tools)
                                {
                                    // this one was already processed
                                    RequestLabs previous = ContainsLabs(title);
                                    if (previous != null)
                                    {
                                        Tools.Remove(previous);
                                    }
                                }
                                continue;
                            }
                            else
                            {
                                if (!tooldata.Contains(title))
                                {
                                    tooldata.Add(title);
                                }
                                lock (Tools)
                                {
                                    if (ContainsLabs(title) == null)
                                    {
                                        Tools.Add(new RequestLabs(title));
                                    }
                                }
                            }
                        }
                        Thread.Sleep(60000);
                    }
                    catch (ThreadAbortException)
                    {
                        notifications.Abort();
                        return;
                    }
                    catch (Exception fail)
                    {
                        handleException(fail);
                    }
                }
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
                lock (Shell)
                {
                    if (Shell.Count > 0 || Tools.Count > 0)
                    {
                        DisplayWaiting();
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage("There are no shell requests waiting", "#wikimedia-labs");
                    }
                }
                return;
            }
        }
    }
}
