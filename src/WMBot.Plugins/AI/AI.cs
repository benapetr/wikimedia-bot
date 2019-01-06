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
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace wmib.Extensions
{
    public class AI : Module
    {
        public AI()
        {
        }

        public override bool Construct()
        {
            this.Version = new Version(1, 0, 0, 0);
            this.HasSeparateThreadInstance = false;
            this.RestartOnModuleCrash = true;
            return true;
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("ai-on");
            UnregisterCommand("ai-off");
            //UnregisterCommand("kick");
            return base.Hook_OnUnload();
        }

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("ai-on", this.aion, false, "admin"));
            RegisterCommand(new GenericCommand("ai-off", this.aioff, false, "admin"));
            return base.Hook_OnRegister();
        }

        private void aion(CommandParams p)
        {
            if (GetConfig(p.SourceChannel, "AI.Enabled", false))
            {
                IRC.DeliverMessage("AI is already enabled", p.SourceChannel);
                return;
            }
            IRC.DeliverMessage("AI enabled", p.SourceChannel.Name);
            SetConfig(p.SourceChannel, "AI.Enabled", true);
            p.SourceChannel.SaveConfig();
        }

        private void aioff(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "AI.Enabled", false))
            {
                IRC.DeliverMessage("AI was already disabled", p.SourceChannel);
                return;
            }
            IRC.DeliverMessage("AI disabled", p.SourceChannel.Name);
            SetConfig(p.SourceChannel, "AI.Enabled", false);
            p.SourceChannel.SaveConfig();
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (!GetConfig(channel, "AI.Enabled", false))
                return;

            // These are commands sent directly to bot
            if (!message.StartsWith(channel.PrimaryInstance.Nick + ": ", StringComparison.InvariantCulture))
                return;

            message = message.Substring(channel.PrimaryInstance.Nick.Length + 2);


        }
    }
}
