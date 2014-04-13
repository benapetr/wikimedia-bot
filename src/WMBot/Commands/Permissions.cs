//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena

using System;

namespace wmib
{
    /// <summary>
    /// Kernel
    /// </summary>
    public partial class Commands
    {
        /// <summary>
        /// Change rights of user
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="channel">Channel</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <returns></returns>
        public static int ModifyRights(string message, Channel channel, string user, string host)
        {
            try
            {
                if (message.StartsWith(Configuration.System.CommandPrefix + "trustadd"))
                {
                    string[] rights_info = message.Split(' ');
                    if (channel.SystemUsers.IsApproved(user, host, "trustadd"))
                    {
                        if (rights_info.Length < 3)
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("Trust1", channel.Language), channel);
                            return 0;
                        }
                        if (!(rights_info[2] == "admin" || rights_info[2] == "trusted"))
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("Unknown1", channel.Language), channel);
                            return 2;
                        }
                        if (rights_info[2] == "admin")
                        {
                            if (!channel.SystemUsers.IsApproved(user, host, "admin"))
                            {
                                Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                                return 2;
                            }
                        }
                        if (channel.SystemUsers.AddUser(rights_info[2], rights_info[1]))
                        {
                            Core.irc.Queue.DeliverMessage(messages.Localize("UserSc", channel.Language) + rights_info[1], channel);
                            return 0;
                        }
                    }
                    else
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("Authorization", channel.Language), channel.Name);
                        return 0;
                    }
                }
                if (message.StartsWith(Configuration.System.CommandPrefix + "trusted"))
                {
                    channel.SystemUsers.ListAll();
                    return 0;
                }
                if (message.StartsWith(Configuration.System.CommandPrefix + "trustdel"))
                {
                    string[] rights_info = message.Split(' ');
                    if (rights_info.Length > 1)
                    {
                        if (channel.SystemUsers.IsApproved(user, host, "trustdel"))
                        {
                            channel.SystemUsers.DeleteUser(channel.SystemUsers.GetUser(user + "!@" + host), rights_info[1]);
                            return 0;
                        }
                        Core.irc.Queue.DeliverMessage(messages.Localize("Authorization", channel.Language), channel);
                        return 0;
                    }
                    Core.irc.Queue.DeliverMessage(messages.Localize("InvalidUser", channel.Language), channel);
                }
            }
            catch (Exception b)
            {
                Core.HandleException(b);
            }
            return 0;
        }
    }
}
