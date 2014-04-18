using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace wmib
{
    public class DynamicRoles : Module
    {
        public bool IsUpdated = false;

        public override bool Construct()
        {
            Name = "DynamicRole";
            Version = "1.0.0.0";
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

        public override void Hook_PRIV(Channel channel, User invoker, string message)
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
                        Core.irc.Queue.DeliverMessage("Invalid number of parameters", channel);
                        return;
                    }
                    string role = channel_name + "." + parameters[0];
                    lock(Security.Roles)
                    {
                        if (!Security.Roles.ContainsKey(role))
                        {
                            Core.irc.Queue.DeliverMessage("There is no role of that name", channel);
                            return;
                        }
                        Security.Roles[role].Revoke(parameters[1]);
                    }
                    Core.irc.Queue.DeliverMessage("Successfuly revoked " + parameters[1] + " from " + role, channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, IRC.priority.low);
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
                        Core.irc.Queue.DeliverMessage("Invalid number of parameters", channel);
                        return;
                    }
                    switch (parameters[1])
                    {
                        case "root":
                        case "terminal":
                        case "halt":
                            Core.irc.Queue.DeliverMessage("This permission can't be granted to anyone, sorry", channel);
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
                    Core.irc.Queue.DeliverMessage("Successfuly granted " + parameters[1] + " to " + role, channel);
                    return;
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }
        }
    }
}

