using System.Threading;

namespace wmib
{
    class ChannelTools : Module
    {
        private static User getUser(string name, Channel c)
        {
            lock (c.UserList)
            {
                foreach (User user in c.UserList)
                {
                    if (name.ToLower() == user.Nick.ToLower())
                    {
                        return user;
                    }
                }
            }
            return null;
        }

        public void GetOp(Channel chan)
        {
            if (!GetConfig(chan, "OP.Permanent", false))
            {
                chan.PrimaryInstance.irc.Queue.Send("CS op " + chan.Name, IRC.priority.high);
                return;
            }
            // get our user
            User user = chan.RetrieveUser(chan.PrimaryInstance.Nick);
            if (user == null)
            {
                chan.PrimaryInstance.irc.Queue.Send("CS op " + chan.Name, IRC.priority.high);
                return;
            }
            if (!user.IsOperator)
            {
                chan.PrimaryInstance.irc.Queue.Send("CS op " + chan.Name, IRC.priority.high);
            }
        }

        public override void Hook_PRIV(Channel channel, User invoker, string message)
        {
            if (message.StartsWith(Configuration.System.CommandPrefix + "optools-on"))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("OpE1", channel.Language), channel);
                        return;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("OpM1", channel.Language), channel.Name);
                    SetConfig(channel, "OP.Enabled", true);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "optools-permanent-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "OP.Permanent", false))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("OpE2", channel.Language), channel);
                        return;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("OpM2", channel.Language), channel);
                    SetConfig(channel, "OP.Permanent", false);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "optools-permanent-on")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Permanent", false))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("OpE3", channel.Language), channel);
                        return;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("OpM3", channel.Language), channel);
                    SetConfig(channel, "OP.Permanent", true);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "optools-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "OP.Enabled", false))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("OpE4", channel.Language), channel);
                        return;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("OpM4", channel.Language), channel);
                    SetConfig(channel, "OP.Enabled", false);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "kick "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(6);
                        string reason = "Removed from the channel";
                        if (nick.Contains(" "))
                        {
                            reason = nick.Substring(nick.IndexOf(" ") + 1);
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("OpE5", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        channel.PrimaryInstance.irc.Queue.Send("KICK " + channel.Name + " " + user.Nick + " :" + reason, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "kb "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(4);
                        string reason = "Removed from the channel";
                        if (nick.Contains(" "))
                        {
                            reason = nick.Substring(nick.IndexOf(" ") + 1);
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("OpE5", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("OpE6", channel.Language), channel, IRC.priority.high);
                        }
                        else
                        {
                            channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " +b *!*@" + user.Host, IRC.priority.high);
                        }
                        channel.PrimaryInstance.irc.Queue.Send("KICK " + channel.Name + " " + user.Nick + " :" + reason, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "unkb "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(6);
                        GetOp(channel);
                        channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -b *!*@" + nick, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "unq "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(5);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("OpE5", channel.Language), channel, IRC.priority.high);
                            return;
                        }

                        if (string.IsNullOrEmpty(user.Host))
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("OpE6", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -q *!*@" + user.Host, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "q "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(3);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("OpE5", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("OpE6", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        GetOp(channel);
                        channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " +q *!*@" + user.Host, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "jb "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(4);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user != null)
                        {
                            nick = user.Nick;
                        }
                        // op self
                        GetOp(channel);
                        channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " +b " + nick + "!*@*$##fix_your_connection", IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "unjb "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(6);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user != null)
                        {
                            nick = user.Nick;
                        }
                        // op self
                        GetOp(channel);
                        channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -b " + nick + "!*@*$##fix_your_connection", IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.irc.Queue.Send("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
            }
        }

        public override bool Construct()
        {
            Version = "1.0.20";
            HasSeparateThreadInstance = false;
            Name = "Operator tools";
            return true;
        }
    }
}
