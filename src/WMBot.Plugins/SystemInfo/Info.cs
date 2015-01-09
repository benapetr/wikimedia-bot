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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace wmib.Extensions
{
    class SystemInfo : Module
    {
        public void DisplayInfo(CommandParams pm)
        {
            if (pm.SourceChannel.Name == Configuration.System.DebugChan)
            {
                foreach (Instance instance in Instance.Instances.Values)
                {
                    instance.Network.Act("is online; channels: " + instance.ChannelCount +
                        " connected: " + instance.IsConnected + " working: " +
                        instance.IsWorking + " queue: " + instance.QueueSize(), pm.SourceChannel.Name);
                }
            }
        }

        public override bool Construct()
        {
            this.Version = new Version(1, 0, 0, 0);
            return base.Construct();
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("systeminfo");
            return true;
        }

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("systeminfo", DisplayInfo));
            return true;
        }
    }
}
