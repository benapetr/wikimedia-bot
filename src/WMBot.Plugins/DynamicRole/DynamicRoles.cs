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
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace wmib.Extensions
{
    public class DynamicRoles : Module
    {
        public bool IsUpdated = false;

        public override bool Construct()
        {
            Version = new Version(1, 0, 0, 3);
            return true;
        }

        public override void RegisterPermissions()
        {
            if (Security.Roles.ContainsKey("admin"))
            {
                Security.Roles["admin"].Grant("grant");
                Security.Roles["admin"].Grant("revoke");
            }
        }

        private void Save()
        {
            string file = Configuration.Paths.Security;
            Core.BackupData(file);
            File.WriteAllText(file, Security.Dump());
            if (File.Exists(Configuration.TempName(file)))
            {
                File.Delete(Configuration.TempName(file));
            }
        }

        public override void Load()
        {
            try
            {
                while (IsWorking)
                {
                    if (IsUpdated)
                    {
                        IsUpdated = false;
                        Save();
                    }
                    Thread.Sleep(2000);
                }
            }
            catch (ThreadAbortException)
            {
                Save();
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            // security hardening
            string channel_name = channel.Name;
            if (string.IsNullOrEmpty(channel_name)) return;
            if (message.StartsWith(Configuration.System.CommandPrefix + "revoke "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "revoke"))
                {
                    string name = message.Substring("@revoke ".Length);
                    List<string> parameters = new List<string>(name.Split(' '));
                    if (parameters.Count != 2)
                    {
                        IRC.DeliverMessage("Invalid number of parameters", channel);
                        return;
                    }
                    string role = channel_name + "." + parameters[0];
                    lock(Security.Roles)
                    {
                        if (!Security.Roles.ContainsKey(role))
                        {
                            IRC.DeliverMessage("There is no role of that name", channel);
                            return;
                        }
                        Security.Roles[role].Revoke(parameters[1]);
                    }
                    IRC.DeliverMessage("Successfuly revoked " + parameters[1] + " from " + role, channel);
                    IsUpdated = true;
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                }
                return;
            }
            if (message.StartsWith(Configuration.System.CommandPrefix + "grant "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "grant"))
                {
                    string name = message.Substring("@grant ".Length);
                    List<string> parameters = new List<string>(name.Split(' '));
                    if (parameters.Count != 2)
                    {
                        IRC.DeliverMessage("Invalid number of parameters", channel);
                        return;
                    }
                    switch (parameters[1])
                    {
                        case "root":
                        case "terminal":
                        case "halt":
                            IRC.DeliverMessage("This permission can't be granted to anyone, sorry", channel);
                            return;
                    }
                    string role = channel_name + "." + parameters[0];
                    lock(Security.Roles)
                    {
                        if (!Security.Roles.ContainsKey(role))
                        {
                            Security.Roles.Add(role, new Security.Role(1));
                        }
                        Security.Roles[role].Grant(parameters[1]);
                    }
                    IsUpdated = true;
                    IRC.DeliverMessage("Successfuly granted " + parameters[1] + " to " + role, channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);
                }
                return;
            }
            if (message.StartsWith(Configuration.System.CommandPrefix + "revokerole "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "grant"))
                {
                    string name = message.Substring("@revokerole ".Length);
                    List<string> parameters = new List<string>(name.Split(' '));
                    if (parameters.Count != 2)
                    {
                        IRC.DeliverMessage("Invalid number of parameters", channel);
                        return;
                    }
                    string role = channel_name + "." + parameters[0];
                    Security.Role _role;
                    lock (Security.Roles)
                    {
                        // now we need to get the role
                        if (!Security.Roles.ContainsKey(parameters[1]))
                        {
                            IRC.DeliverMessage("There is no such a role", channel);
                            return;
                        }
                        _role = Security.Roles[parameters[1]];
                        if (!Security.Roles.ContainsKey(role))
                        {
                            IRC.DeliverMessage("There is no such a role", channel);
                            return;
                        }
                        if (!Security.Roles[role].Roles.Contains(_role))
                        {
                            IRC.DeliverMessage("This role doesn't has this role so I can't revoke it!!", channel);
                            return;
                        }
                        Security.Roles[role].Revoke(_role);
                    }
                    IsUpdated = true;
                    IRC.DeliverMessage("Successfuly revoked role" + parameters[1] + " to " + role, channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);

                return;
            }
            if (message.StartsWith(Configuration.System.CommandPrefix + "grantrole "))
            {
                if (channel.SystemUsers.IsApproved(invoker, "grant"))
                {
                    string name = message.Substring("@grantrole ".Length);
                    List<string> parameters = new List<string>(name.Split(' '));
                    if (parameters.Count != 2)
                    {
                        IRC.DeliverMessage("Invalid number of parameters", channel);
                        return;
                    }
                    string role = channel_name + "." + parameters[0];
                    Security.Role _role;
                    lock (Security.Roles)
                    {
                        // now we need to get the role
                        if (!Security.Roles.ContainsKey(parameters[1]))
                        {
                            IRC.DeliverMessage("There is no such a role", channel);
                            return;
                        }
                        _role = Security.Roles[parameters[1]];
                        if (_role.IsPermitted("root"))
                        {
                            IRC.DeliverMessage("Sorry but this role can't be granted", channel);
                            return;
                        }
                        if (!Security.Roles.ContainsKey(role))
                            Security.Roles.Add(role, new Security.Role(_role.Level));
                        else if (Security.Roles[role].Level < _role.Level)
                            Security.Roles[role].Level = _role.Level;
                        if (Security.Roles[role].Roles.Contains(_role))
                        {
                            IRC.DeliverMessage("This role already has this role as well", channel);
                            return;
                        }
                        Security.Roles[role].Grant(_role);
                    }
                    IsUpdated = true;
                    IRC.DeliverMessage("Successfuly granted role" + parameters[1] + " to " + role, channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                    IRC.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel);

                return;
            }
        }
    }
}

