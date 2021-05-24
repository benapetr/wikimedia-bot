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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace wmib.Extensions
{
    public class NetworkBridge : wmib.Module
    {
        // NetCat port of bot on first network
        public int Port = 64834;
        private List<string> itemsToSend = new List<string>();

        public override bool Construct()
        {
            Version = new Version(1, 0, 0, 0);
            this.Port = Configuration.RetrieveConfig("networkbridge.port", this.Port);
            return true;
        }

        public override void Load()
        {
            while (IsWorking)
            {
                try
                {
                    List<string> items = new List<string>();
                    lock(this.itemsToSend)
                    {
                        if (this.itemsToSend.Count > 0)
                        {
                            items.AddRange(this.itemsToSend);
                            this.itemsToSend.Clear();
                        }
                    }
                    if (items.Count > 0)
                    {
                        try
                        {
                            TcpClient client = new TcpClient("127.0.0.1", this.Port);
                            NetworkStream nwStream = client.GetStream();
                            string input = "";
                            foreach (string i in items)
                                input += i + "\n";
                            
                            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(input);
                            nwStream.Write(bytesToSend, 0, bytesToSend.Length);
                            client.Close();
                        } catch (Exception e)
                        {
                            HandleException(e);
                        }  
                    }
                    Thread.Sleep(200);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception fail)
                {
                    HandleException(fail);
                }
            }
        }

        private void sendMsg(string target, string message)
        {
            lock (this.itemsToSend)
            {
                this.itemsToSend.Add(target + " " + message);
            }
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
             if (!GetConfig(channel, "NetworkBridge.Enabled", false))
                return;

             // Send message over
             this.sendMsg(channel.Name, "<" + invoker.Nick + "> " + message);
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("bridge-on");
            UnregisterCommand("bridge-off");
            return base.Hook_OnUnload();
        }

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("bridge-on", this.bridge_on, false, "admin"));
            RegisterCommand(new GenericCommand("bridge-off", this.bridge_off, false, "admin"));
            return base.Hook_OnRegister();
        }

        private void bridge_on(CommandParams p)
        {
            if (GetConfig(p.SourceChannel, "NetworkBridge.Enabled", false))
            {
                IRC.DeliverMessage("NetworkBridge is already enabled", p.SourceChannel.Name);
                return;
            }
            SetConfig(p.SourceChannel, "NetworkBridge.Enabled", true);
            p.SourceChannel.SaveConfig();
            IRC.DeliverMessage("NetworkBridge was enabled", p.SourceChannel.Name);
        }

        private void bridge_off(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "NetworkBridge.Enabled", false))
            {
                IRC.DeliverMessage("NetworkBridge is already disabled", p.SourceChannel.Name);
                return;
            }
            SetConfig(p.SourceChannel, "NetworkBridge.Enabled", false);
            p.SourceChannel.SaveConfig();
            IRC.DeliverMessage("NetworkBridge was disabled", p.SourceChannel.Name);
        }
    }
}
