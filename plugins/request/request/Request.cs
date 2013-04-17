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
        public static List<RequestLabs> DB = new List<RequestLabs>();
        public Thread notifications = null;

        public RequestLabs Contains(string user)
        {
            lock (DB)
            {
                foreach (RequestLabs r in DB)
                {
                    if (r.user == user)
                    {
                        return r;
                    }
                }
            }
            return null;
        }

        public RequestLabs getUser(string user, ref List<RequestLabs> list)
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
            Version = "1.0.2";
            return true;
        }

        public void DisplayWaiting()
        {
            if (DB.Count > 0)
            {
                int requestCount = DB.Count;
                int displayed = 0;
                string info = "";
                foreach (RequestLabs u in DB)
                {
                    displayed++;
                    info += u.user + " (waiting " + u.WaitingTime() + ") ";
                    if (info.Length > 160)
                    {
                        break;
                    }
                }
                info = "Warning: There are " + requestCount.ToString() + " users waiting for shell, displaying last " + displayed.ToString() + ": " + info;
                core.irc._SlowQueue.DeliverMessage(info, "#wikimedia-labs");
            }
        }

        public void Run()
        {
            try
            {
                Thread.Sleep(60000);
                while (true)
                {
                    lock (DB)
                    {
                        DisplayWaiting();
                    }
                    if (DB.Count > 0)
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
                core.handleException(fail);
            }
        }

        public override void Load()
        {
            try
            {
                RequestCache.Load();
                notifications = new Thread(Run);
                notifications.Start();
                while (true)
                {
                    try
                    {
                        List<RequestLabs> delete = new List<RequestLabs>();
                        lock (DB)
                        {
                            delete.AddRange(DB);
                        }
                        Site wikitech = new Site("https://wikitech.wikimedia.org", "wmib", "");
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
                            if (!(page.text.Contains("|Completed=No") || page.text.Contains("|Completed=false")))
                            {
                                RequestCache.Insert(title);
                                lock (DB)
                                {
                                    RequestLabs previous = Contains(title);
                                    if (previous != null)
                                    {
                                        DB.Remove(previous);
                                    }
                                }
                                continue;
                            }
                            else
                            {
                                lock (DB)
                                {
                                    // we don't want to remove request that is still active
                                    RequestLabs xx = getUser(title, ref delete);
                                    if (xx != null)
                                    {
                                        delete.Remove(xx);
                                    }
                                    if (Contains(title) == null)
                                    {
                                        DB.Add(new RequestLabs(title));
                                    }
                                }
                            }
                            // now we need to remove all processed requests that were in a list
                            lock (DB)
                            {
                                foreach (RequestLabs r in delete)
                                {
                                    if (DB.Contains(r))
                                    {
                                        DB.Remove(r);
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
                        core.handleException(fail);
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
                notifications.Abort();
            }
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (channel.Name != "#wikimedia-labs")
            {
                return;
            }
            if (message == "@requests-off")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
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
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
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
                lock (DB)
                {
                    if (DB.Count > 0)
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
