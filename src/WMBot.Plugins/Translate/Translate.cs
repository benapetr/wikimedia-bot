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

using System.Collections.Generic;
using System.Xml;
using System.Threading;

namespace wmib.Extensions
{
    public class Translate : Module
    {
        public static Buffer Ring = new Buffer();
        private string URL = "https://translate.yandex.net/api/v1.5/tr/";

        public class Buffer
        {
            public class Item
            {
                public Channel Channel;
                public string SourceLang;
                public string TargetLang;
                public string TargetName;
                public string Message;

                public Item(Channel channel, string sl, string tl, string tn, string message)
                {
                    this.SourceLang = sl;
                    this.TargetLang = tl;
                    this.Message = message;
                    this.Channel = channel;
                    this.TargetName = tn;
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

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("translate", this.translate));
            return base.Hook_OnRegister();
        }

        private void translate(CommandParams p)
        {
            if (string.IsNullOrEmpty(p.Parameters))
                return;


            List<string> parts = new List<string>(p.Parameters.Split(' '));
            if (parts.Count < 3)
            {
                IRC.DeliverMessage("Invalid number of parameters", p.SourceChannel);
                return;
            }
            string target = null;
            string source_language = parts[0];
            string target_language = parts[1];
            if (!IsValid(source_language) || !IsValid(target_language))
            {
                IRC.DeliverMessage(p.User.Nick + ": invalid language!", p.SourceChannel);
                return;
            }
            string text = p.Parameters.Substring(p.Parameters.IndexOf(parts[1]) + parts[1].Length + 1);
            if (text.Contains("|"))
            {
                target = text.Substring(text.IndexOf("|") + 1).Trim();
                text = text.Substring(0, text.IndexOf("|"));
            }
            // schedule a message
            Ring.Add(new Buffer.Item(p.SourceChannel, source_language, target_language, target, text));
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("translate");
            return base.Hook_OnUnload();
        }

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
                                            if (request.TargetName == null)
                                            {
                                                IRC.DeliverMessage("Translating from " + request.SourceLang + " to " + request.TargetLang + ": " + n2.InnerText + " (powered by Yandex)", request.Channel);
                                            } else
                                            {
                                                IRC.DeliverMessage(request.TargetName + ": " + n2.InnerText + " (powered by Yandex)", request.Channel);
                                            }
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
    }
}
