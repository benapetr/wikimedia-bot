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

namespace wmib.Extensions
{
    class ChannelTools : Module
    {
        private static libirc.User getUser(string name, Channel c)
        {
            return c.RetrieveUser(name);
        }

        private void PermanentOn(CommandParams p)
        {
            if (GetConfig(p.SourceChannel, "OP.Permanent", false))
            {
                IRC.DeliverMessage(messages.Localize("OpE3", p.SourceChannel.Language), p.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("OpM3", p.SourceChannel.Language), p.SourceChannel);
            SetConfig(p.SourceChannel, "OP.Permanent", true);
            p.SourceChannel.SaveConfig();
        }

        private void PermanentOff(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "OP.Permanent", false))
            {
                IRC.DeliverMessage(messages.Localize("OpE2", p.SourceChannel.Language), p.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("OpM2", p.SourceChannel.Language), p.SourceChannel);
            SetConfig(p.SourceChannel, "OP.Permanent", false);
            p.SourceChannel.SaveConfig();
        }

        private void Off(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "OP.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("OpE4", p.SourceChannel.Language), p.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("OpM4", p.SourceChannel.Language), p.SourceChannel);
            SetConfig(p.SourceChannel, "OP.Enabled", false);
            p.SourceChannel.SaveConfig();
        }

        private void On(CommandParams p)
        {
            if (GetConfig(p.SourceChannel, "OP.Enabled", false))
            {
                IRC.DeliverMessage(messages.Localize("OpE1", p.SourceChannel.Language), p.SourceChannel);
                return;
            }
            IRC.DeliverMessage(messages.Localize("OpM1", p.SourceChannel.Language), p.SourceChannel.Name);
            SetConfig(p.SourceChannel, "OP.Enabled", true);
            p.SourceChannel.SaveConfig();
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("optools-on");
            UnregisterCommand("optools-off");
            UnregisterCommand("optools-permanent-on");
            UnregisterCommand("optools-permanent-off");
            UnregisterCommand("topic");
            //UnregisterCommand("kick");
            return base.Hook_OnUnload();
        }

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("optools-on", this.On, false, "admin"));
            RegisterCommand(new GenericCommand("optools-permanent-on", this.PermanentOn, true, "admin"));
            RegisterCommand(new GenericCommand("optools-permanent-off", this.PermanentOff, true, "admin"));
            RegisterCommand(new GenericCommand("optools-off", this.Off, true, "admin"));
            RegisterCommand(new GenericCommand("topic", this.Topic, true, "admin"));
            return base.Hook_OnRegister();
        }

        public void GetOp(Channel chan)
        {
            // get our user
            libirc.User user = chan.RetrieveUser(chan.PrimaryInstance.Nick);
            if (user == null)
            {
                chan.PrimaryInstance.Network.Transfer("CS op " + chan.Name, libirc.Defs.Priority.High);
                return;
            }
            if (!user.IsOp)
            {
                chan.PrimaryInstance.Network.Transfer("CS op " + chan.Name, libirc.Defs.Priority.High);
            }
        }

        private void Topic(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "OP.Enabled", false))
                return;

            if (string.IsNullOrEmpty(p.Parameters))
            {
                IRC.DeliverMessage("You must provide some topic for this to work", p.SourceChannel);
                return;
            }

            // Set the topic
            GetOp(p.SourceChannel);
            p.SourceChannel.PrimaryInstance.Network.Transfer("TOPIC " + p.SourceChannel.Name + " :" + p.Parameters);
            // Remove op if we don't use permanent ops
            if (!GetConfig(p.SourceChannel, "OP.Permanent", false))
            {
                p.SourceChannel.PrimaryInstance.Network.Transfer("MODE " + p.SourceChannel.Name + " -o " + p.SourceChannel.PrimaryInstance.Nick, libirc.Defs.Priority.Low);
            }
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
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
                        libirc.User user = getUser(nick, channel);
                        if (user == null)
                        {
                            IRC.DeliverMessage(messages.Localize("OpE5", channel.Language), channel, libirc.Defs.Priority.High);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        channel.PrimaryInstance.Network.Transfer("KICK " + channel.Name + " " + user.Nick + " :" + reason, libirc.Defs.Priority.High);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name);
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
                        libirc.User user = getUser(nick, channel);
                        if (user == null)
                        {
                            IRC.DeliverMessage(messages.Localize("OpE5", channel.Language), channel);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            IRC.DeliverMessage(messages.Localize("OpE6", channel.Language), channel, libirc.Defs.Priority.High);
                        }
                        else
                        {
                            channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " +b *!*@" + user.Host, libirc.Defs.Priority.High);
                        }
                        channel.PrimaryInstance.Network.Transfer("KICK " + channel.Name + " " + user.Nick + " :" + reason, libirc.Defs.Priority.High);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, libirc.Defs.Priority.Low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, libirc.Defs.Priority.Low);
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
                        channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -b *!*@" + nick, libirc.Defs.Priority.High);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, libirc.Defs.Priority.Low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, libirc.Defs.Priority.Low);
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
                        libirc.User user = getUser(nick, channel);
                        if (user == null)
                        {
                            IRC.DeliverMessage(messages.Localize("OpE5", channel.Language), channel, libirc.Defs.Priority.High);
                            return;
                        }

                        if (string.IsNullOrEmpty(user.Host))
                        {
                            IRC.DeliverMessage(messages.Localize("OpE6", channel.Language), channel, libirc.Defs.Priority.High);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -q *!*@" + user.Host, libirc.Defs.Priority.High);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, libirc.Defs.Priority.Low);
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
                        libirc.User user = getUser(nick, channel);
                        if (user == null)
                        {
                            IRC.DeliverMessage(messages.Localize("OpE5", channel.Language), channel, libirc.Defs.Priority.High);
                            return;
                        }
                        
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            IRC.DeliverMessage(messages.Localize("OpE6", channel.Language), channel, libirc.Defs.Priority.High);
                            return;
                        }
                        GetOp(channel);
                        channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " +q *!*@" + user.Host, libirc.Defs.Priority.High);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, libirc.Defs.Priority.Low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, libirc.Defs.Priority.Low);
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
                        libirc.User user = getUser(nick, channel);
                        if (user != null)
                        {
                            nick = user.Nick;
                        }
                        // op self
                        GetOp(channel);
                        channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " +b " + nick + "!*@*$##fix_your_connection", libirc.Defs.Priority.High);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, libirc.Defs.Priority.Low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, libirc.Defs.Priority.Low);
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
                        libirc.User user = getUser(nick, channel);
                        if (user != null)
                        {
                            nick = user.Nick;
                        }
                        // op self
                        GetOp(channel);
                        channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -b " + nick + "!*@*$##fix_your_connection", libirc.Defs.Priority.High);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.PrimaryInstance.Network.Transfer("MODE " + channel.Name + " -o " + channel.PrimaryInstance.Nick, libirc.Defs.Priority.Low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, libirc.Defs.Priority.Low);
                }
            }
        }

        public override bool Construct()
        {
            Version = new Version(1, 0, 20);
            this.HasSeparateThreadInstance = false;
            return true;
        }
    }
}
