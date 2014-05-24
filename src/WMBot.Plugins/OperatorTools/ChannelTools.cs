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
using System.Threading;

namespace wmib.Extensions
{
    class ChannelTools : Module
    {
        private static libirc.User getUser(string name, Channel c)
        {
            return c.RetrieveUser(name);
        }

        public void GetOp(Channel chan)
        {
            if (!GetConfig(chan, "OP.Permanent", false))
            {
                chan.PrimaryInstance.Network.Transfer("CS op " + chan.Name, libirc.Defs.Priority.High);
                return;
            }
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

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (message.StartsWith(Configuration.System.CommandPrefix + "optools-on"))
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        IRC.DeliverMessage(messages.Localize("OpE1", channel.Language), channel);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("OpM1", channel.Language), channel.Name);
                    SetConfig(channel, "OP.Enabled", true);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "optools-permanent-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "OP.Permanent", false))
                    {
                        IRC.DeliverMessage(messages.Localize("OpE2", channel.Language), channel);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("OpM2", channel.Language), channel);
                    SetConfig(channel, "OP.Permanent", false);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "optools-permanent-on")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Permanent", false))
                    {
                        IRC.DeliverMessage(messages.Localize("OpE3", channel.Language), channel);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("OpM3", channel.Language), channel);
                    SetConfig(channel, "OP.Permanent", true);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "optools-off")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "OP.Enabled", false))
                    {
                        IRC.DeliverMessage(messages.Localize("OpE4", channel.Language), channel);
                        return;
                    }
                    IRC.DeliverMessage(messages.Localize("OpM4", channel.Language), channel);
                    SetConfig(channel, "OP.Enabled", false);
                    channel.SaveConfig();
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, libirc.Defs.Priority.Low);
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
            HasSeparateThreadInstance = false;
            return true;
        }
    }
}
