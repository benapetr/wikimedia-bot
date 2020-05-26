//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Copyright 2013 - 2018 Petr Bena (benapetr@gmail.com)

using System;

namespace wmib
{
    public partial class Commands
    {
        public static bool Trusted(string message, string user, string host, CommandParams parameters)
        {
            try
            {
                if (message.StartsWith(Configuration.System.CommandPrefix + "trusted ", System.StringComparison.InvariantCulture))
                {
                    Channel ch;
                    if (parameters.Parameters != null) {
                        string[] channel_info = parameters.Parameters.Split(' ');
                        ch = core.GetChannel(channel_info[0]);
                    }
                    else {
                        ch = Core.GetChannel(message.Substring("xtrusted ".Length));
                    }
                    if (ch != null)
                    {
                        IRC.DeliverMessage(messages.Localize("TrustedUserList", ch.Language) + ch.SystemUsers.ListAll(), user);
                        return true;
                    }
                    IRC.DeliverMessage("Sorry, I'm not currently in that channel.", user);
                    return true;
                }
            } catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            return false;
        }

        private static void TrustAdd(CommandParams parameters)
        {
            Channel chan;
            if (parameters.Parameters == null)
                return;
            string[] rights_info = parameters.Parameters.Split(' ');
            if (rights_info.Length < 2)
            {
                IRC.DeliverMessage(messages.Localize("Trust1", parameters.SourceChannel.Language), parameters.SourceChannel);
                return;
            }
            if (!Security.Roles.ContainsKey(rights_info[1]))
            {
                IRC.DeliverMessage(messages.Localize("Unknown1", parameters.SourceChannel.Language), parameters.SourceChannel);
                return;
            }
            int level = Security.GetLevelOfRole(rights_info[1]);
            // This optional hack disallow to grant roles like "root" to anyone so that this role can be granted only to users
            // with shell access to server and hard-inserting it to admins file. If you wanted to allow granting of root, just
            // change System.MaxGrantableRoleLevel to 65535, this isn't very secure though
            if (level > Configuration.System.MaxGrantableRoleLevel)
            {
                IRC.DeliverMessage("You can't grant this role because it's over the maximum grantable role level, sorry", parameters.SourceChannel);
                return;
            }
            // now we check if role that user is to grant doesn't have higher level than the role they have
            // if we didn't do that, users with low roles could grant admin to someone and exploit this
            // to grant admins to themselves
            if (level > parameters.SourceChannel.SystemUsers.GetLevel(parameters.User))
            {
                IRC.DeliverMessage(messages.Localize("RoleMismatch", parameters.SourceChannel.Language), parameters.SourceChannel);
                return;
            }
            if (rights_info[2] != null) {
                if ( channel.SystemUsers.IsApproved(invoker, "root") {
                    chan = core.GetChannel(rights_info[2]);
                }
                else {
                    IRC.DeliverMessage("Sorry, only root users can manage permissions for other channels.", parameters.SourceChannel);
                    return;
                }
            }
            else {
                chan = parameters.SourceChannel;
            }
            if (chan.SystemUsers.AddUser(rights_info[1], rights_info[0]))
            {
                IRC.DeliverMessage(messages.Localize("UserSc", parameters.SourceChannel.Language) + rights_info[0], parameters.SourceChannel);
                if ( rights_info[2] != null ) {
                    IRC.DeliverMessage(rights_info[0] + ", was added to the trust list in this channel. (" + parameters.SourceChannel + ")", chan);
                }
                return;
            }
        }

        private static void TrustDel(CommandParams parameters)
        {
            Channel chan;
            string[] rights_info = parameters.Parameters.Split(' ');
            if (rights_info[0] = null)
            {
                IRC.DeliverMessage(messages.Localize("InvalidUser", parameters.SourceChannel.Language), parameters.SourceChannel);
                return;
            }
            if (rights_info[1] != null) {
                if ( channel.SystemUsers.IsApproved(invoker, "root") {
                    chan = core.GetChannel(rights_info[1]);
                }
                else {
                    IRC.DeliverMessage("Sorry, only root users can manage permissions for other channels.", parameters.SourceChannel);
                    return;
                }
            }
            else {
                chan = parameters.SourceChannel;
            }
            chan.SystemUsers.DeleteUser(parameters.SourceChannel.SystemUsers.GetUser(parameters.User), rights_info[0]);
            return;
        }

        private static void TrustedList(CommandParams parameters)
        {
            IRC.DeliverMessage(messages.Localize("TrustedUserList", parameters.SourceChannel.Language) + parameters.SourceChannel.SystemUsers.ListAll(), parameters.SourceChannel);
        }
    }
}
