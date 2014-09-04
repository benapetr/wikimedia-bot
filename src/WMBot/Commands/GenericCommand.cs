//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Copyright 2013 - 2014 Petr Bena (benapetr@gmail.com)

using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public class GenericCommand
    {
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        private Action<CommandParams> action;
        private string name;
        private bool canIgnore = true;
        private string requiredPermission = null;
        private bool channelOnly = true;
        public bool SilentErrors = false;
        public bool ChannelOnly
        {
            get
            {
                return this.channelOnly;
            }
        }
        public string RequiredPermission
        {
            get
            {
                return this.requiredPermission;
            }
        }
        /// <summary>
        /// Whether this command can be ignored
        /// </summary>
        public bool IsIgnorable
        {
            get
            {
                return this.canIgnore;
            }
        }

        public GenericCommand(string name_, Action<CommandParams> function)
        {
            this.action = function;
            this.name = name_;
        }

        public GenericCommand(string name_, Action<CommandParams> function, bool ignorable, string permissions = null, bool channel = true, bool silent = false)
        {
            this.action = function;
            this.channelOnly = channel;
            this.name = name_;
            this.canIgnore = ignorable;
            this.SilentErrors = silent;
            this.requiredPermission = permissions;
        }

        public virtual void Launch(CommandParams parameter)
        {
            if (this.channelOnly && parameter.SourceChannel == null)
                return;
            if (this.RequiredPermission != null)
            {
                if (parameter.SourceChannel != null)
                {
                    if (!parameter.SourceChannel.SystemUsers.IsApproved(parameter.User, requiredPermission))
                    {
                        if (!parameter.SourceChannel.SuppressWarnings && !SilentErrors)
                            IRC.DeliverMessage(messages.Localize("PermissionDenied", parameter.SourceChannel.Language), parameter.SourceChannel);
                        // user doesn't have permission to run this command
                        return;
                    }
                }
                else if (!Security.IsGloballyApproved(parameter.User, RequiredPermission))
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied"), parameter.User, libirc.Defs.Priority.Low);
                }
            }
            this.action(parameter);
        }
    }
}
