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
        public static bool Trusted(string message, string user, string host)
        {
            try
            {
                if (message.StartsWith(Configuration.System.CommandPrefix + "trusted "))
                {
                    Channel ch = Core.GetChannel(message.Substring("xtrusted ".Length));
                    if (ch != null)
                    {
                        IRC.DeliverMessage(messages.Localize("TrustedUserList", ch.Language) + ch.SystemUsers.ListAll(), user);
                        return true;
                    }
                    IRC.DeliverMessage("There is no such a channel I know of", user);
                    return true;
                }
            } catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            return false;
        }
        
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
                libirc.UserInfo invoker = new libirc.UserInfo(user, "", host);
                if (message.StartsWith(Configuration.System.CommandPrefix + "trustadd"))
                {
                    string[] rights_info = message.Split(' ');
                    if (channel.SystemUsers.IsApproved(invoker, "trustadd"))
                    {
                        if (rights_info.Length < 3)
                        {
                            IRC.DeliverMessage(messages.Localize("Trust1", channel.Language), channel);
                            return 0;
                        }
                        if (!Security.Roles.ContainsKey(rights_info[2]))
                        {
                            IRC.DeliverMessage(messages.Localize("Unknown1", channel.Language), channel);
                            return 2;
                        }
                        int level = Security.GetLevelOfRole(rights_info[2]);
                        // This optional hack disallow to grant roles like "root" to anyone so that this role can be granted only to users
                        // with shell access to server and hard-inserting it to admins file. If you wanted to allow granting of root, just
                        // change System.MaxGrantableRoleLevel to 65535, this isn't very secure though
                        if (level > Configuration.System.MaxGrantableRoleLevel)
                        {
                            IRC.DeliverMessage("You can't grant this role because it's over the maximum grantable role level, sorry", channel);
                            return 2;
                        }
                        // now we check if role that user is to grant doesn't have higher level than the role they have
                        // if we didn't do that, users with low roles could grant admin to someone and exploit this
                        // to grant admins to themselve
                        if (level > channel.SystemUsers.GetLevel(invoker))
                        {
                            IRC.DeliverMessage(messages.Localize("RoleMismatch", channel.Language), channel);
                            return 2;
                        }
                        if (channel.SystemUsers.AddUser(rights_info[2], rights_info[1]))
                        {
                            IRC.DeliverMessage(messages.Localize("UserSc", channel.Language) + rights_info[1], channel);
                            return 0;
                        }
                    }
                    else
                    {
                        IRC.DeliverMessage(messages.Localize("Authorization", channel.Language), channel);
                        return 0;
                    }
                }
                if (message.StartsWith(Configuration.System.CommandPrefix + "trusted"))
                {
                    IRC.DeliverMessage(messages.Localize("TrustedUserList", channel.Language) + channel.SystemUsers.ListAll(), channel);
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
                        IRC.DeliverMessage(messages.Localize("Authorization", channel.Language), channel);
                        return 0;
                    }
                    IRC.DeliverMessage(messages.Localize("InvalidUser", channel.Language), channel);
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
