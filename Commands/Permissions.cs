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
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace wmib
{
    /// <summary>
    /// Kernel
    /// </summary>
    public partial class core
    {
        /// <summary>
        /// Change rights of user
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="channel">Channel</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <returns></returns>
        public static int ModifyRights(string message, config.channel channel, string user, string host)
        {
            try
            {
                if (message.StartsWith(config.CommandPrefix + "trustadd"))
                {
                    string[] rights_info = message.Split(' ');
                    if (channel.Users.isApproved(user, host, "trustadd"))
                    {
                        if (rights_info.Length < 3)
                        {
                            irc.Message(messages.get("Trust1", channel.Language), channel.Name);
                            return 0;
                        }
                        if (!(rights_info[2] == "admin" || rights_info[2] == "trusted"))
                        {
                            irc.Message(messages.get("Unknown1", channel.Language), channel.Name);
                            return 2;
                        }
                        if (rights_info[2] == "admin")
                        {
                            if (!channel.Users.isApproved(user, host, "admin"))
                            {
                                irc.Message(messages.get("PermissionDenied", channel.Language), channel.Name);
                                return 2;
                            }
                        }
                        if (channel.Users.addUser(rights_info[2], rights_info[1]))
                        {
                            irc.Message(messages.get("UserSc", channel.Language) + rights_info[1], channel.Name);
                            return 0;
                        }
                    }
                    else
                    {
                        irc._SlowQueue.DeliverMessage(messages.get("Authorization", channel.Language), channel.Name);
                        return 0;
                    }
                }
                if (message.StartsWith(config.CommandPrefix + "trusted"))
                {
                    channel.Users.listAll();
                    return 0;
                }
                if (message.StartsWith(config.CommandPrefix + "trustdel"))
                {
                    string[] rights_info = message.Split(' ');
                    if (rights_info.Length > 1)
                    {
                        string x = rights_info[1];
                        if (channel.Users.isApproved(user, host, "trustdel"))
                        {
                            channel.Users.delUser(channel.Users.getUser(user + "!@" + host), rights_info[1]);
                            return 0;
                        }
                        else
                        {
                            irc._SlowQueue.DeliverMessage(messages.get("Authorization", channel.Language), channel.Name);
                            return 0;
                        }
                    }
                    irc.Message(messages.get("InvalidUser", channel.Language), channel.Name);
                }
            }
            catch (Exception b)
            {
                handleException(b);
            }
            return 0;
        }
    }
}
