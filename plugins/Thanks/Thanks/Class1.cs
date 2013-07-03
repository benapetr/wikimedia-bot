using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
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

            private List<Item> data = new List<Item>();

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
            Name = "Thanks";
            start = true;
            Version = "1.0.0.0";
            return true;
        }

        public override void Load()
        {
            while (working)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
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
                if (message.Contains("wm-bot") && !message.Contains("thanks to") && (message.Contains("thanks") || message.Contains("thank you")))
                {
                    string response = "Hey " + invoker.Nick + ", you are welcome!";
                    Buffer.Item x = Ring.getUser(invoker.Nick);
                    DebugLog("Checking if user was recently informed using infobot");
                    if (x != null)
                    {
                        response = "Hey " + invoker.Nick + ", you are welcome, but keep in mind I am just a stupid bot, it was actually " + x.User + " who helped you :-)";
                        Ring.Delete(x);
                    }
                    core.irc._SlowQueue.DeliverMessage(response, channel);
                }
            }
        }
    }
}
