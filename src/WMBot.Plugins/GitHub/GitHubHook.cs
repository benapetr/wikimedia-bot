//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace wmib.Extensions
{
    public class GitHub : Module
    {
        public override bool Construct()
        {
            RestartOnModuleCrash = true;
            Version = new Version(1, 0, 0, 0);
            RegisterCommand(new GenericCommand("github-on", github_On, true, "admin"));
            RegisterCommand(new GenericCommand("github-off", github_Off, true, "admin"));
            RegisterCommand(new GenericCommand("github-", github_Remove, true, "trusted"));
            RegisterCommand(new GenericCommand("github+", github_Insert, true, "trusted"));
            HasSeparateThreadInstance = false;
            return true;
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("github-on");
            UnregisterCommand("github-off");
            UnregisterCommand("github+");
            UnregisterCommand("github-");
            return base.Hook_OnUnload();
        }

        private void github_Insert(CommandParams p)
        {
            if (string.IsNullOrEmpty(p.Parameters))
            {
                IRC.DeliverMessage("This command requires exactly 1 parameter", p.SourceChannel);
                return;
            }
            lock (Core.DB.DatabaseLock)
            {
                string error = "unknown";
                Core.DB.Connect();
                if (!Core.DB.IsConnected)
                {
                    if (Core.DB.ErrorBuffer != null)
                    {
                        error = Core.DB.ErrorBuffer;
                    }
                    IRC.DeliverMessage("Unable to connect to SQL: " + error, p.SourceChannel);
                    return;
                }
                // first check if repository isn't already there
                List<List<string>> result = Core.DB.Select("github_repo_info", "name, channel", "WHERE name = '" + Core.DB.EscapeInput(p.Parameters) + "' AND channel = '" +
                    Core.DB.EscapeInput(p.SourceChannel.Name) + "'", 2);
                if (result.Count != 0)
                {
                    IRC.DeliverMessage("This repository is already in DB", p.SourceChannel);
                    Core.DB.Disconnect();
                    return;
                }
                Core.DB.Disconnect();
                Core.DB.Connect();
                Database.Row row = new Database.Row();
                row.Values.Add(new Database.Row.Value(0));
                row.Values.Add(new Database.Row.Value(p.Parameters, Database.DataType.Varchar));
                row.Values.Add(new Database.Row.Value(p.SourceChannel.Name, Database.DataType.Varchar));
                row.Values.Add(new Database.Row.Value("", Database.DataType.Varchar));
                row.Values.Add(new Database.Row.Value(true));
                if (!Core.DB.InsertRow("github_repo_info", row))
                {
                    if (Core.DB.ErrorBuffer != null)
                    {
                        error = Core.DB.ErrorBuffer;
                    }
                    IRC.DeliverMessage("Failed to insert row: " + error, p.SourceChannel);
                    Core.DB.Disconnect();
                    return;
                }
                Core.DB.Commit();
                Core.DB.Disconnect();
            }
            IRC.DeliverMessage("Hooks from " + p.Parameters + " will be now displayed in this channel", p.SourceChannel);
        }

        private void github_Remove(CommandParams p)
        {
            if (string.IsNullOrEmpty(p.Parameters))
            {
                IRC.DeliverMessage("This command requires exactly 1 parameter", p.SourceChannel);
                return;
            }
            lock (Core.DB.DatabaseLock)
            {
                string error = "unknown";
                Core.DB.Connect();
                if (!Core.DB.IsConnected)
                {
                    if (Core.DB.ErrorBuffer != null)
                    {
                        error = Core.DB.ErrorBuffer;
                    }
                    IRC.DeliverMessage("Unable to connect to SQL: " + error, p.SourceChannel);
                    return;
                }
                // first check if repository isn't already there
                List<List<string>> result = Core.DB.Select("github_repo_info", "name, channel", "WHERE name = '" + Core.DB.EscapeInput(p.Parameters) + "' AND channel = '" +
                    Core.DB.EscapeInput(p.SourceChannel.Name) + "'", 2);
                // for some reason we need to reconnect db otherwise it stuck for like 20 seconds
                Core.DB.Disconnect();
                Core.DB.Connect();
                if (result.Count == 0)
                {
                    IRC.DeliverMessage("This repository is not in DB", p.SourceChannel);
                    Core.DB.Disconnect();
                    return;
                }
                Core.DB.Delete("github_repo_info", "name = '" + Core.DB.EscapeInput(p.Parameters)
                    + "' AND channel = '"
                    + Core.DB.EscapeInput(p.SourceChannel.Name) + "'");
                Core.DB.Commit();
                Core.DB.Disconnect();
            }
            IRC.DeliverMessage("Hooks from " + p.Parameters + " were disabled for this channel", p.SourceChannel);
        }

        private void github_On(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "github.enabled", false))
            {
                SetConfig(p.SourceChannel, "github.enabled", true);
                IRC.DeliverMessage("GitHub was enabled for this channel, use github+ to add some repos", p.SourceChannel);
                return;
            }
            else
            {
                IRC.DeliverMessage("GitHub is already enabled, nothing done", p.SourceChannel);
            }
        }

        private void github_Off(CommandParams p)
        {
            if (GetConfig(p.SourceChannel, "github.enabled", false))
            {
                SetConfig(p.SourceChannel, "github.enabled", false);
                IRC.DeliverMessage("GitHub was turned off for this channel", p.SourceChannel);
                return;
            }
            else
            {
                IRC.DeliverMessage("GitHub is already off, nothing done", p.SourceChannel);
            }
        }
    }
}
