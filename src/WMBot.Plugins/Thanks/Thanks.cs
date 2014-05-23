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
using System.Threading;

namespace wmib.Extensions
{
    public class Thanks : Module
    {
        public class Buffer
        {
            public class Item
            {
                public string User;
                public string Target;

                public Item(string user, string target)
                {
                    Target = target;
                    User = user;
                }
            }

            private readonly List<Item> data = new List<Item>();

            public const int Size = 8000;

            public void Add(Item item)
            {
                lock (data)
                {
                    data.Add(item);

                    if (data.Count > Size)
                    {
                        data.RemoveAt(0);
                    }
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

            public Item getUser(string user)
            {
                lock (data)
                {
                    user = user.ToLower();
                    foreach (Item item in data)
                    {
                        if (item.Target.ToLower() == user)
                        {
                            return item;
                        }
                    }
                }
                return null;
            }
        }

        public static Buffer Ring = new Buffer();

        public override bool Construct()
        {
            this.HasSeparateThreadInstance = false;
            return true;
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (message.StartsWith("!") && message.Contains("|"))
            {
                DebugLog("Parsing: " + message, 6);
                string user = message.Substring(message.IndexOf("|") + 1);
                user = user.Trim();
                DebugLog("Parsed user - " + user, 6);
                if (user.Contains(" "))
                {
                    user = user.Substring(0, user.IndexOf(" "));
                }
                if (user != "")
                {
                    DebugLog("Adding user to list " + user, 6);
                    Ring.Add(new Buffer.Item(invoker.Nick, user));
                }
            }
            else
            {
                message = message.ToLower();
                if (message.Contains(channel.PrimaryInstance.Nick) && !message.Contains("thanks to") && (message.Contains("thanks") || message.Contains("thank you")) && !message.Contains("no thank"))
                {
                    string response = "Hey " + invoker.Nick + ", you are welcome!";
                    Buffer.Item x = Ring.getUser(invoker.Nick);
                    DebugLog("Checking if user was recently informed using infobot");
                    if (x != null)
                    {
                        response = "Hey " + invoker.Nick + ", you are welcome, but keep in mind I am just a stupid bot, it was actually " + x.User + " who helped you :-)";
                        Ring.Delete(x);
                    }
                    IRC.DeliverMessage(response, channel);
                }
            }
        }
    }
}
