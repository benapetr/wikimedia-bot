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
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace wmib.Extensions
{
    public class AI : Module
    {
        public string Server = "ns.insw.cz";
        public uint Port = 8082;

        public AI()
        {
        }

        public override bool Construct()
        {
            this.Version = new Version(1, 0, 0, 0);
            this.HasSeparateThreadInstance = false;
            this.RestartOnModuleCrash = true;
            return true;
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (!message.StartsWith(channel.PrimaryInstance.Nick + ": "))
                return;

            message = message.Substring(channel.PrimaryInstance.Nick.Length + 2);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + this.Server + ":" + this.Port + "/cakechat_api/v1/actions/get_response");
            request.ContentType = "application/json";
            request.Method = "POST";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json = "{ \"context\": [\"" + message + "\"], \"emotion\": \"neutral\" }";

                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                string result = streamReader.ReadToEnd().Replace("\n", "");
                // We don't have JSON in older .Net where bot is actually running, so let's do it hacky way
                if (!result.Contains("\"response\":"))
                {
                    // error?
                    return;
                }
                result = result.Substring(result.IndexOf("\"response\":") + 13);
                result = result.Substring(0, result.Length - 2);

                IRC.DeliverMessage(invoker.Nick + ": " + result, channel);
            }
        }
    }
}
