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
using System.IO;
using System.Threading;

namespace wmib
{
    public class HtmlDump
    {
        /// <summary>
        /// Channel name
        /// </summary>
        public config.channel Channel;

        /// <summary>
        /// Dump
        /// </summary>
        public string dumpname;

        // This function is called on start of bot
        public static void Start()
        {
            while (true)
            {
                foreach (config.channel chan in config.channels)
                {
                    if (chan.Keys.update)
                    {
                        HtmlDump dump = new HtmlDump(chan);
                        dump.Make();
                        chan.Keys.update = false;
                    }
                }
				Stat();
                Thread.Sleep(320000);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="channel"></param>
        public HtmlDump(config.channel channel)
        {
            dumpname = config.DumpDir + "/" + channel.name + ".htm";
            Channel = channel;
        }

        /// <summary>
        /// Html code
        /// </summary>
        /// <returns></returns>
        public string CreateFooter()
        {
            return "</body></html>\n";
        }

        /// <summary>
        /// Html
        /// </summary>
        /// <returns></returns>
        public string CreateHeader()
        {
            return "<html><head><title>" + Channel.name + "</title></head><body>\n";
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
        public string AddLine(string name, string value)
        {
            return "<tr><td>" + Encode(name) + "</td><td>" + Encode(value) + "</td></tr>\n";
        }

        /// <summary>
        /// Insert another table row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string AddLink(string name, string value)
        {
            return "<tr><td>" + Encode(name) + "</td><td><a href=\"#" + Encode(value) + "\">" + Encode(value) + "</a></td></tr>\n";
        }

        /// <summary>
        /// Insert another table row
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string AddKey(string name, string value)
        {
            return "<tr id=\"" + Encode(name) + "\"><td>" + Encode(name) + "</td><td>" + Encode(value) + "</td></tr>\n";
        }
		
		/// <summary>
		/// Create stat for bot
		/// </summary>
		public static void Stat()
		{
			try
            {
				string text = "<html><head><title>System info</title></head><body>\n";
				text += "<h1>System data</h1><p>List of channels:</p>";
				foreach (config.channel chan in config.channels)
                {
					text = text + "" + chan.name + " (infobot: " + chan.info.ToString () + ", recentchanges: " + chan.feed.ToString () + ", logs: " + chan.logged.ToString () +  ")<br>\n";
				}
				text += "Uptime: " + core.getUptime ();
				text += "</body></html>";
				File.WriteAllText(config.DumpDir + "/systemdata.htm" , text);
			}catch (Exception b)
            {
                Console.WriteLine(b.Message);
            }
		}
		
        /// <summary>
        /// Generate a dump file
        /// </summary>
        public void Make()
        {
            try
            {
                string text = CreateHeader();
                text = text + "<h4>Infobot</h4>\n";
                text = text + "<table border=1 width=100%>\n<tr><th width=10%>Key</th><th>Value</th></tr>\n";
                Channel.Keys.locked = true;
				List<dictionary.item> list = new List<dictionary.item>();
				list.AddRange(Channel.Keys.text);
				list.Sort();
                if (Channel.Keys.text.Count > 0)
                {
                    foreach (dictionary.item Key in list)
                    {
                        text += AddKey(Key.key, Key.text);
                    }
                }
                text = text + "</table>\n";
                text = text + "<h4>Aliases</h4>\n<table border=1 width=100%>\n";
                foreach (dictionary.staticalias data in Channel.Keys.Alias)
                {
                    text += AddLink(data.Name, data.Key);
                }
                text = text + "</table>\n";
                Channel.Keys.locked = false;
                if (Channel.feed)
                {
                    text += "<h4>Recent changes</h4>";
                    text = text + Channel.RC.ToTable();
                }
                text = text + CreateFooter();
                File.WriteAllText(dumpname, text);
            }
            catch (Exception b)
            {
                Channel.Keys.locked = false;
                Console.WriteLine(b.Message);
            }
        }
    }
}
