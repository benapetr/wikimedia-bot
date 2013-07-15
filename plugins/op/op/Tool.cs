using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace wmib
{
    class ChannelTools : Module
    {
        public override void Load()
        {
            try
            {
                while (working)
                {
                    System.Threading.Thread.Sleep(1000000);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }

        private static User getUser(string name, config.channel c)
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

        public void GetOp(config.channel chan)
        {
            if (!GetConfig(chan, "OP.Permanent", false))
            {
                core.irc._SlowQueue.DeliverMessage("op " + chan.Name, "ChanServ", IRC.priority.high);
                return;
            }
            // get our user
            User user = chan.RetrieveUser(config.username);
            if (user == null)
            {
                core.irc._SlowQueue.DeliverMessage("op " + chan.Name, "ChanServ", IRC.priority.high);
                return;
            }
            if (!user.IsOperator)
            {
                core.irc._SlowQueue.DeliverMessage("op " + chan.Name, "ChanServ", IRC.priority.high);
            }
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (message.StartsWith(config.CommandPrefix + "optools-on"))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Operator tools were already enabled on this channel", channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage("Operator tools have been enabled on this channel", channel.Name);
                        SetConfig(channel, "OP.Enabled", true);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "optools-permanent-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "OP.Permanent", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Operator tools were already not in permanent mode on this channel", channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage("Operator tools are now not in permanent mode on this channel", channel.Name);
                        SetConfig(channel, "OP.Permanent", false);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "optools-permanent-on")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Permanent", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Operator tools were already in permanent mode on this channel", channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage("Operator tools are now in permanent mode on this channel", channel.Name);
                        SetConfig(channel, "OP.Permanent", true);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "optools-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "OP.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Operator tools were already disabled on this channel", channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage("Operator tools have been disabled on this channel", channel.Name);
                        SetConfig(channel, "OP.Enabled", false);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "kick "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
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
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't see this user in a channel", channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        core.irc._SlowQueue.Send("KICK " + channel.Name + " " + user.Nick + " :" + reason, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            core.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + config.username, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "kb "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
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
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't see this user in a channel", channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't know hostname of this user... you will need to issue the ban yourself", channel, IRC.priority.high);
                        }
                        else
                        {
                            core.irc._SlowQueue.Send("MODE " + channel.Name + " +b *!*@" + user.Host, IRC.priority.high);
                        }
                        core.irc._SlowQueue.Send("KICK " + channel.Name + " " + user.Nick + " :" + reason, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            core.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + config.username, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "unkb "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(6);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't see this user in a channel", channel, IRC.priority.high);
                            return;
                        }
                        
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't know hostname of this user... you will need to issue the ban yourself", channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        core.irc._SlowQueue.Send("MODE " + channel.Name + " -b *!*@" + user.Host, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            core.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + config.username, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "unq "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
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
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't see this user in a channel", channel, IRC.priority.high);
                            return;
                        }

                        if (string.IsNullOrEmpty(user.Host))
                        {
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't know hostname of this user... you will need to issue the ban yourself", channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        core.irc._SlowQueue.Send("MODE " + channel.Name + " -q *!*@" + user.Host, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            core.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + config.username, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "q "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
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
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't see this user in a channel", channel, IRC.priority.high);
                            return;
                        }
                        
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            core.irc._SlowQueue.DeliverMessage("Sorry but I don't know hostname of this user... you will need to issue the ban yourself", channel, IRC.priority.high);
                            return;
                        }
                        GetOp(channel);
                        core.irc._SlowQueue.Send("MODE " + channel.Name + " +q *!*@" + user.Host, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            core.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + config.username, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "jb "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
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
                        core.irc._SlowQueue.Send("MODE " + channel.Name + " +b " + nick + "!*@*$##fix_your_connection", IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            core.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + config.username, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "unjb "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
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
                        core.irc._SlowQueue.Send("MODE " + channel.Name + " -b " + nick + "!*@*$##fix_your_connection", IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            core.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + config.username, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }
        }

        public override bool Construct()
        {
            Version = "1.0.8";
            start = true;
            Name = "Operator tools";
            return true;
        }
    }
}
