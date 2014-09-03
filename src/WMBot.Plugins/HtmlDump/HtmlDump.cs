//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;

namespace wmib.Extensions
{
    public class HtmlDump : Module
    {
        public override bool Construct()
        {
            Version = new Version(1, 0, 8, 6);
            return true;
        }

        public override bool Hook_OnRegister()
        {
            foreach (Channel chan in Configuration.ChannelList)
            {
                SetConfig(chan, "HTML.Update", true);
            }
            return true;
        }

        // This function is called on start of bot
        public override void Load()
        {
            Thread.Sleep(10000);
            while (true)
            {
                foreach (Channel chan in Configuration.ChannelList)
                {
                    if (GetConfig(chan, "HTML.Update", true))
                    {
                        HtmlDumpObj dump = new HtmlDumpObj(chan);
                        dump.Make();
                        Syslog.DebugLog("Making dump for " + chan.Name);
                        SetConfig(chan, "HTML.Update", false);
                    }
                }
                HtmlDumpObj.Stat();
                Thread.Sleep(320000);
            }
        }
    }

    public class HtmlDumpObj
    {
        /// <summary>
        /// Channel name
        /// </summary>
        public Channel _Channel;

        /// <summary>
        /// Dump
        /// </summary>
        public string dumpname;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="channel"></param>
        public HtmlDumpObj(Channel channel)
        {
            dumpname = Configuration.Paths.DumpDir + "/" + channel.Name + ".htm";
            if (!Directory.Exists(Configuration.Paths.DumpDir))
            {
                Syslog.Log("Creating a directory for dump");
                Directory.CreateDirectory(Configuration.Paths.DumpDir);
            }
            _Channel = channel;
        }

        /// <summary>
        /// Html code
        /// </summary>
        /// <returns></returns>
        private static string CreateFooter()
        {
            return "</body></html>\n";
        }

        /// <summary>
        /// Html
        /// </summary>
        /// <returns></returns>
        private static string CreateHeader(string page_name)
        {
            string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\n\n\n\n<html><head>";
            if (Configuration.WebPages.Css != "")
            {
                html += "<link rel=\"stylesheet\" href=\"" + Configuration.WebPages.Css + "\" type=\"text/css\"/>";
            }
            html += "<title>" + page_name + "</title>\n\n<meta charset=\"UTF-8\"></head><body>\n";
            return html;
        }

        /// <summary>
        /// Remove html
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Encode(string text)
        {
            text = text.Replace("<", "&lt;");
            text = text.Replace(">", "&gt;");
            return text;
        }

        /// <summary>
        /// Insert another table row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string AddLine(string name, string value)
        {
            return "<tr><td>" + Encode(name) + "</td><td>" + Encode(value) + "</td></tr>\n";
        }

        /// <summary>
        /// Insert another table row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string AddLink(string name, string value)
        {
            return "<tr><td>" + Encode(name) + "</td><td><a href=\"#" + Encode(value) + "\">" + Encode(value) + "</a></td></tr>\n";
        }

        /// <summary>
        /// Insert another table row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string AddKey(string name, string value)
        {
            return "<tr id=\"" + Encode(name) + "\"><td>" + Encode(name) + "</td><td>" + Encode(value) + "</td></tr>\n";
        }

        public static string getSize()
        {
            string[] a = Directory.GetFiles("configuration");
            // 2
            // Calculate total bytes of all files in a loop.
            long b = 0;
            foreach (string name in a)
            {
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            return b.ToString();
        }

        /// <summary>
        /// Create stat for bot
        /// </summary>
        public static void Stat()
        {
            try
            {
                Thread.Sleep(2000);
                StringBuilder builder = new StringBuilder(CreateHeader("System info"));
                builder.AppendLine("<h1>System data</h1><p class=info>List of channels:</p>");
                builder.AppendLine("<table class=\"channels\">");
                builder.AppendLine("<tr><th>Channel name</th><th>Options</th></tr>");
                foreach (Channel chan in Configuration.ChannelList)
                {
                    builder.AppendLine("<tr>");
                    builder.AppendFormat("<td><a href=\"{0}.htm\">{1}</a></td><td>\n", HttpUtility.UrlEncode(chan.Name),
                        chan.Name);
                    builder.AppendLine("infobot: " + Module.GetConfig(chan, "Infobot.Enabled", true)
                                       + ", Recent Changes: " + Module.GetConfig(chan, "RC.Enabled", false)
                                       + ", Logs: " + Module.GetConfig(chan, "Logging.Enabled", false)
                                       + ", Suppress: " + chan.Suppress
                                       + ", Seen: " + Module.GetConfig(chan, "Seen.Enabled", false)
                                       + ", rss: " + Module.GetConfig(chan, "Rss.Enabled", false)
                                       + ", statistics: " + Module.GetConfig(chan, "Statistics.Enabled", false)
                                       + " Instance: " + chan.PrimaryInstance.Nick + "</td></tr>");
                }
                builder.AppendLine("</table>Uptime: " + Core.getUptime() + " Memory usage: " +
                        (Process.GetCurrentProcess().PrivateMemorySize64/1024) + "kb Database size: " + getSize());
                lock (ExtensionHandler.Extensions)
                {
                    foreach (Module mm in ExtensionHandler.Extensions)
                    {
                        string text = string.Empty;
                        mm.Hook_BeforeSysWeb(ref text);
                        builder.AppendLine(text);
                    }
                    builder.AppendFormat("<br />Core version: {0}<br />\n", Configuration.System.Version);
                    builder.AppendFormat("<h2>Bots</h2>");
                    builder.AppendFormat("<table class=\"text\"><th>Name</th><th>Status</th><th>Bouncer</th>");
                    lock (Instance.Instances)
                    {
                        foreach (Instance xx in Instance.Instances.Values)
                        {
                            string status = "Online in " + xx.ChannelCount + " channels";
                            if (!xx.IsWorking || !xx.IsConnected)
                            {
                                status = "Disconnected";
                            }
                            builder.AppendLine("<tr><td>" + xx.Nick + "</td><td>" + status + "</td><td>" + xx.Port + "</td></tr>");
                        }
                    }
                    builder.AppendLine("</table>");
                    builder.AppendLine("<h2>Permissions</h2>");
                    builder.AppendLine("<table class=\"permissions\">");
                    builder.AppendLine("  <tr><th>Role</th><th>Permissions</th><th>Roles</th></tr>");
                    lock (Security.Roles)
                    {
                        foreach (string rn in Security.Roles.Keys)
                        {
                            Security.Role role = Security.Roles[rn];
                            builder.AppendLine("<tr><td valign=top>" + rn + "</td><td valign=top>");
                            foreach (string permission in role.Permissions)
                                builder.AppendLine(permission + "<br>");
                            builder.AppendLine("</td><td valign=top>");
                            foreach (Security.Role sr in role.Roles)
                                builder.AppendLine(Security.GetNameOfRole(sr) + "</br>");
                            builder.AppendLine("</td></tr>");
                        }
                    }
                    builder.AppendLine("</table>");
                    builder.AppendLine("<h2>Plugins</h2>");
                    builder.AppendLine("<table class=\"modules\">");
                    foreach (Module module in ExtensionHandler.Extensions)
                    {
                        string status = "Terminated";
                        if (module.IsWorking)
                        {
                            status = "OK";
                            if (module.Warning)
                            {
                                status += " - RECOVERING";
                            }
                        }
                        builder.AppendLine("<tr><td>" + module.Name + " (" + module.Version + ")</td><td>" + status +
                                           " (startup date: " + module.Date + ")</td></tr>");
                    }
                }
                builder.AppendLine("</table>");
                builder.AppendLine();
                builder.AppendLine(CreateFooter());
                File.WriteAllText(Configuration.Paths.DumpDir + "/systemdata.htm", builder.ToString());
            }
            catch (Exception b)
            {
                Core.HandleException(b, "HtmlDump");
            }
        }

        /// <summary>
        /// Generate a dump file
        /// </summary>
        public void Make()
        {
            try
            {
                Dictionary<string, string> ModuleData = new Dictionary<string, string>();
                lock (ExtensionHandler.Extensions)
                {
                    foreach (Module xx in ExtensionHandler.Extensions)
                    {
                        try
                        {
                            if (xx.IsWorking)
                            {
                                string html = xx.Extension_DumpHtml(_Channel);
                                if (!string.IsNullOrEmpty(html))
                                {
                                    ModuleData.Add(xx.Name.ToLower(), html);
                                }
                            }
                        }
                        catch (Exception fail)
                        {
                            Syslog.Log("Unable to retrieve web data", true);
                            Core.HandleException(fail, "HtmlDump");
                        }
                    }
                }
                StringBuilder text = new StringBuilder(CreateHeader(_Channel.Name));
                if (ModuleData.ContainsKey("infobot core"))
                {
                    if (Module.GetConfig(_Channel, "Infobot.Enabled", true))
                    {
                        text.Append("<h4>Infobot</h4>\n");
                        if (_Channel.SharedDB != "" && _Channel.SharedDB != "local")
                        {
                            Channel temp = Core.GetChannel(_Channel.SharedDB);
                            if (temp != null)
                                text.Append("Linked to <a href=" + HttpUtility.UrlEncode(temp.Name) + ".htm>" + temp.Name + "</a>\n");
                            else
                                text.AppendLine("Channel is linked to " + _Channel.SharedDB + " which isn't in my db, that's weird");
                        }
                        else
                            text.AppendLine(ModuleData["infobot core"]);
                    }
                    ModuleData.Remove("infobot core");
                }
                if (ModuleData.ContainsKey("rc"))
                {
                    if (Module.GetConfig(_Channel, "RC.Enabled", false))
                    {
                        text.AppendLine("\n<br /><h4>Recent changes</h4>");
                        text.AppendLine(ModuleData["rc"]);
                        ModuleData.Remove("rc");
                    }
                }
                if (ModuleData.ContainsKey("statistics") && ModuleData["statistics"] != null)
                {
                    if (Module.GetConfig(_Channel, "Statistics.Enabled", false))
                    {
                        text.AppendLine(ModuleData["statistics"]);
                        ModuleData.Remove("statistics");
                    }
                }
                if (ModuleData.ContainsKey("feed"))
                {
                    if (Module.GetConfig(_Channel, "Rss.Enable", false))
                    {
                        text.AppendLine(ModuleData["feed"]);
                        ModuleData.Remove("feed");
                    }
                }
                foreach (KeyValuePair<string, string> item in ModuleData)
                {
                    if (!string.IsNullOrEmpty(item.Value))
                        text.AppendLine(item.Value);
                }
                text.Append(CreateFooter());
                File.WriteAllText(dumpname, text.ToString());
            }
            catch (Exception b)
            {
                Core.HandleException(b, "HtmlDump");
            }
        }
    }
}
