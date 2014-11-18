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
                        DebugLog("Making dump for " + chan.Name);
                        HtmlDumpObj dump = new HtmlDumpObj(chan);
                        dump.Make();
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
        private string dumpname;

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
            double b = 0;
            foreach (string name in a)
            {
                if (name.EndsWith("~"))
                    continue;
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            b = b / 1024;
            return b.ToString() + "kb";
        }

        /// <summary>
        /// Create stat for bot
        /// </summary>
        public static void Stat()
        {
            int line = 0;
            try
            {
                
                Thread.Sleep(2000);
                StringBuilder builder = new StringBuilder(CreateHeader("System info"));
                line = 3;
                builder.AppendLine("<h1>System data</h1><p class=info>List of channels:</p>");
                builder.AppendLine("<table class=\"channels\">");
                builder.AppendLine("<tr><th>Channel name</th><th>Options</th></tr>");
                line = 6;
                foreach (Channel chan in Configuration.ChannelList)
                {
                    builder.AppendLine("<tr>");
                    line = 8;
                    builder.AppendFormat("<td><a href=\"{0}.htm\">{1}</a> (" + chan.IrcChannel.UserCount.ToString() + ")</td><td>\n", HttpUtility.UrlEncode(chan.Name),
                        chan.Name);
                    line = 9;
                    builder.AppendLine("infobot: " + Module.GetConfig(chan, "Infobot.Enabled", true)
                                       + ", Recent Changes: " + Module.GetConfig(chan, "RC.Enabled", false)
                                       + ", Logs: " + Module.GetConfig(chan, "Logging.Enabled", false)
                                       + ", Suppress: " + chan.Suppress
                                       + ", Seen: " + Module.GetConfig(chan, "Seen.Enabled", false)
                                       + ", rss: " + Module.GetConfig(chan, "Rss.Enabled", false)
                                       + ", statistics: " + Module.GetConfig(chan, "Statistics.Enabled", false)
                                       + " Instance: " + chan.PrimaryInstance.Nick + "</td></tr>");
                }
                line = 10;
                builder.AppendLine("</table>Uptime: " + Core.getUptime() + " Memory usage: " +
                        (Process.GetCurrentProcess().PrivateMemorySize64 / 1024) + "kb Database size: " + getSize());
                line = 11;
                foreach (Module mm in ExtensionHandler.ExtensionList)
                {
                    string text = string.Empty;
                    line = 12;
                    mm.Hook_BeforeSysWeb(ref text);
                    builder.AppendLine(text);
                }
                line = 13;
                builder.AppendFormat("<br />Core version: {0}<br />\n", Configuration.System.Version);
                builder.AppendFormat("<h2>Bots</h2>");
                builder.AppendFormat("<table class=\"text\"><th>Name</th><th>Status</th><th>Bouncer</th>");
                line = 16;
                foreach (Instance xx in Instance.Instances.Values)
                {
                    string status = "Online in " + xx.ChannelCount + " channels";
                    line = 17;
                    if (!xx.IsWorking || !xx.IsConnected)
                    {
                        status = "Disconnected";
                    }
                    line = 18;
                    builder.AppendLine("<tr><td>" + xx.Nick + "</td><td>" + status + "</td><td>" + xx.Port + "</td></tr>");
                }
                line = 20;
                builder.AppendLine("</table>");
                builder.AppendLine("<h2>Permissions</h2>");
                builder.AppendLine("<table class=\"permissions\">");
                builder.AppendLine("  <tr><th>Role</th><th>Permissions</th><th>Roles</th></tr>");
                line = 24;
                lock (Security.Roles)
                {
                    foreach (string rn in Security.Roles.Keys)
                    {
                        Security.Role role = Security.Roles[rn];
                        line = 25;
                        builder.AppendLine("<tr><td valign=top>" + rn + "</td><td valign=top>");
                        line = 26;
                        foreach (string permission in role.Permissions)
                            builder.AppendLine(permission + "<br>");
                        line = 27;
                        builder.AppendLine("</td><td valign=top>");
                        line = 28;
                        foreach (Security.Role sr in role.Roles)
                            builder.AppendLine(Security.GetNameOfRole(sr) + "</br>");
                        line = 29;
                        builder.AppendLine("</td></tr>");
                    }
                }
                line = 30;
                builder.AppendLine("</table>");
                builder.AppendLine("<h2>Plugins</h2>");
                builder.AppendLine("<table class=\"modules\">");
                foreach (Module module in ExtensionHandler.ExtensionList)
                {
                    string status = "Terminated";
                    line = 40;
                    if (module.IsWorking)
                    {
                        status = "OK";
                        if (module.Warning)
                        {
                            status += " - RECOVERING";
                        }
                    }
                    line = 41;
                    builder.AppendLine("<tr><td>" + module.Name + " (" + module.Version + ")</td><td>" + status +
                                       " (startup date: " + module.Date + ")</td></tr>");
                }
                line = 42;
                builder.AppendLine("</table>");
                builder.AppendLine();
                builder.AppendLine(CreateFooter());
                line = 43;
                File.WriteAllText(Configuration.Paths.DumpDir + "/systemdata.htm", builder.ToString());
            }
            catch (Exception b)
            {
                Syslog.Log("HTMLDUMP debug line " + line.ToString());
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
                foreach (Module xx in ExtensionHandler.ExtensionList)
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
