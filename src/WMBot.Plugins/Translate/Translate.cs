//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Threading;

namespace wmib.Extensions
{
    public class Translate : Module
    {
        private string URL = "https://translate.yandex.net/api/v1.5/tr/";
        public class Buffer
        {
            public class Item
            {
                public Channel Channel;
                public string SourceLang;
                public string TargetLang;
                public string Message;

                public Item(Channel channel, string sl, string tl, string message)
                {
                    this.SourceLang = sl;
                    this.TargetLang = tl;
                    this.Message = message;
                    this.Channel = channel;
                }
            }

            public readonly List<Item> data = new List<Item>();
            public void Add(Item item)
            {
                lock (data)
                {
                    data.Add(item);
                }
            }

            public void Delete(Item item)
            {
                lock (data)
                {
                    if (data.Contains(item))
                    {
                        data.Remove(item);
                    }
                }
            }
        }

        public static Buffer Ring = new Buffer();

        public override bool Construct()
        {
            return true;
        }

        public override void Load()
        {
			System.Net.WebClient wx = new System.Net.WebClient();
			string key = Configuration.RetrieveConfig("yandex");
			key = System.Web.HttpUtility.UrlEncode(key);
			if (key == null)
			{
				Syslog.ErrorLog("Unable to load translate module because there is no valid key");
				this.IsWorking = false;
				return;
			}
            while (IsWorking)
            {
				if (Ring.data.Count > 0)
				{
					List<Buffer.Item> data = new List<Buffer.Item>();
					lock(Ring.data)
					{
						data.AddRange(Ring.data);
						Ring.data.Clear();
					}
					foreach (Buffer.Item request in data)
					{
						try
						{
							// get a translation for this item
							string result = wx.DownloadString(this.URL + "translate?key=" + key + "&lang=" + System.Web.HttpUtility.UrlEncode(request.SourceLang) + "-" +
							                                  System.Web.HttpUtility.UrlEncode(request.TargetLang) + "&text=" + 
							                                  System.Web.HttpUtility.UrlEncode(request.Message));
							XmlDocument xd = new XmlDocument();
							xd.LoadXml(result);
							bool ok = false;
							foreach(XmlNode n1 in xd.ChildNodes)
							{
								if (n1.Name == "Translation" && n1.ChildNodes.Count > 0)
								{
									foreach (XmlNode n2 in n1.ChildNodes)
									{
										if (n2.Name == "text")
										{
											ok = true;
											IRC.DeliverMessage("Translating from " + request.SourceLang + " to " + request.TargetLang + " (powered by Yandex): " + n2.InnerText, request.Channel);
										}
									}
								}
							}
							if (!ok)
							{
								DebugLog(result);
								IRC.DeliverMessage("Error - unable to translate the message (wrong language?) check debug logs for more information", request.Channel);
							}


						} catch (ThreadAbortException)
						{
							return;
						} catch (System.Exception fail)
						{
							HandleException(fail);
						}
					}
				}
                Thread.Sleep(100);
            }
        }

		private static bool IsValid(string language_code)
		{
			if (language_code.Contains("+") ||
				language_code.Contains("-") ||
				language_code.Contains("_") ||
				language_code.Contains("&") ||
				language_code.Contains("?"))
				return false;
			return true;
		}

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (message.StartsWith(Configuration.System.CommandPrefix + "translate "))
            {
				message = message.Substring(11);
                List<string> parts = new List<string>(message.Split(' '));
                if (parts.Count < 3)
                {
                    IRC.DeliverMessage("Invalid number of parameters", channel);
                    return;
                }
                string source_language = parts[0];
                string target_language = parts[1];
				if (!IsValid(source_language) || !IsValid(target_language))
				{
					IRC.DeliverMessage(invoker.Nick + ": invalid language!", channel);
					return;
				}
                string text = message.Substring(message.IndexOf(parts[1]) + parts[1].Length + 1);
                // schedule a message
                Ring.Add(new Buffer.Item(channel, source_language, target_language, text));
            }
        }
    }
}
