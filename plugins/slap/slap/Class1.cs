using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public class Slap : Module
    {
        public override bool Construct()
        {
            start = true;
            HasSeparateThreadInstance = false;
            Name = "Slap";
            Version = "1.0";
            return true;
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (!message.StartsWith(config.CommandPrefix) && GetConfig(channel, "Slap.Enabled", false))
            {
                string ms = message.Trim();
                ms = ms.Replace("!", "");
                ms = ms.Replace("?", "");
                ms = ms.ToLower();
                if (ms.StartsWith("hi "))
                {
                    ms = ms.Substring(3);
                }
                if (ms.StartsWith("hi, "))
                {
                    ms = ms.Substring(4);
                }
                if (ms.StartsWith("hello "))
                {
                    ms = ms.Substring(5);
                }
                if (ms.StartsWith("hello, "))
                {
                    ms = ms.Substring(6);
                }
                if (ms.EndsWith(":ping") || ms.EndsWith(": ping"))
                {
                    string target = message.Trim();
                    target = message.Substring(0, message.IndexOf(":"));
                    if (GetConfig(channel, "Slap.Ping." + target, false))
                    {
                        channel.instance.irc._SlowQueue.DeliverMessage("Hi " + invoker.Nick + ", you just managed to say pointless nick: ping. Now please try again with some proper meaning of your request, something like nick: I need this and that. Or don't do that at all, it's very annoying. Thank you", channel);
                        return;
                    }
                }

                if (ms == "i have a question" || ms == "can i ask a question" || ms == "can i ask" || ms == "i got a question" || ms == "can i have a question" || ms == "can someone help me" || ms == "i need help")
                {
                    channel.instance.irc._SlowQueue.DeliverMessage("Hi " + invoker.Nick + ", just ask! There is no need to ask if you can ask", channel);
                    return;
                }

                if (ms == "is anyone here" || ms == "is anybody here" || ms == "is anybody there" || ms == "is some one there" || ms == "is someone there" || ms == "is someone here")
                {
                    channel.instance.irc._SlowQueue.DeliverMessage("Hi " + invoker.Nick + ", I am here, if you need anything, please ask, otherwise no one is going to help you... Thank you", channel);
                    return;
                }
            }

            if (message == config.CommandPrefix + "slap")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    SetConfig(channel, "Slap.Enabled", true);
                    core.irc._SlowQueue.DeliverMessage("I will be slapping stupid people since now", channel);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage("Permission denied", channel);
                }
            }

            if (message == config.CommandPrefix + "noslap")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    SetConfig(channel, "Slap.Enabled", false);
                    core.irc._SlowQueue.DeliverMessage("I will not be slapping stupid people since now", channel);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage("Permission denied", channel);
                }
            }

            if (message == config.CommandPrefix + "nopingslap")
            {
                if (channel.Users.IsApproved(invoker, "trust"))
                {
                    SetConfig(channel, "Slap.Ping." + invoker.Nick.ToLower(), false);
                    core.irc._SlowQueue.DeliverMessage("I will not be slapping people who slap you now", channel);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage("Permission denied", channel);
                }
            }

            if (message == config.CommandPrefix + "pingslap")
            {
                if (channel.Users.IsApproved(invoker, "trust"))
                {
                    SetConfig(channel, "Slap.Ping." + invoker.Nick.ToLower(), true);
                    core.irc._SlowQueue.DeliverMessage("I will be slapping people who ping you now", channel);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage("Permission denied", channel);
                }
            }
        }
    }
}
