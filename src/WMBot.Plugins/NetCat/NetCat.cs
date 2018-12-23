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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace wmib.Extensions
{
    public class NetCat : wmib.Module
    {
        public int Port = 64834;

        public override bool Construct()
        {
            Version = new Version(1, 0, 2, 0);
            this.Port = Configuration.RetrieveConfig("netcat.port", this.Port);
            return true;
        }

        private void SendMessage(ref StreamWriter writer, string text)
        {
            writer.WriteLine(text);
            writer.Flush();
        }

        private void Client(object data)
        {
            try
            {
                // #channel token message goes here and needs a newline on end
                DebugLog("Accepted connection");
                TcpClient client = (TcpClient)data;
                string IP = client.Client.RemoteEndPoint.ToString();
                DebugLog("Incoming connection from: " + IP);
                NetworkStream ns = client.GetStream();
                StreamWriter _StreamWriter = new StreamWriter(ns);
                StreamReader _StreamReader = new StreamReader(ns, Encoding.UTF8);
                while (!_StreamReader.EndOfStream)
                {
                    string text = _StreamReader.ReadLine();
                    if (!text.Contains(" "))
                    {
                        DebugLog("Invalid text from " + IP + ": " + text, 2);
                        SendMessage(ref _StreamWriter, "ERROR1 (invalid text): " + text);
                        continue;
                    }
                    string channel = text.Substring(0, text.IndexOf(" ", StringComparison.InvariantCulture));
                    string value = text.Substring(text.IndexOf(" ", StringComparison.InvariantCulture) + 1);
                    DebugLog("Request to send text to channel " + channel + " text: " + value, 4);
                    Channel ch = Core.GetChannel(channel);
                    if (ch == null)
                    {
                        DebugLog("Nonexistent channel " + channel + " message was rejected");
                        SendMessage(ref _StreamWriter, "ERROR2 (invalid channel " + channel + "): " + value);
                        continue;
                    }
                    if (!GetConfig(ch, "NetCat.Enabled", false))
                    {
                        DebugLog("Channel doesn't allow relay " + channel + " message was rejected");
                        SendMessage(ref _StreamWriter, "ERROR3 (disallowed): " + channel + " :" + value);
                        continue;
                    }
                    if (GetConfig(ch, "NetCat.Token", false))
                    {
                        DebugLog("Channel requires the token for relay " + channel, 6);
                        if (!value.Contains(" "))
                        {
                            DebugLog("Invalid token from " + IP + " to " + channel);
                            SendMessage(ref _StreamWriter, "ERROR4 (invalid token): " + channel + " :" + value);
                            continue;
                        }
                        string token = value.Substring(0, value.IndexOf(" ", StringComparison.InvariantCulture));
                        value = value.Substring(value.IndexOf(" ", StringComparison.InvariantCulture) + 1);
                        if (token != GetConfig(ch, "NetCat.TokenData", "<invalid>"))
                        {
                            DebugLog("Channel requires the token for relay " + channel, 6);
                            SendMessage(ref _StreamWriter, "ERROR4 (invalid token): " + channel + " :" + value);
                            continue;
                        }
                    }
                    DebugLog("Relaying message from " + IP + " to " + channel + ":" + value, 2);
                    IRC.DeliverMessage(value, ch, libirc.Defs.Priority.Low);
                }
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        public override void Load()
        {
            try
            {
                Log("NetCat listening on port " + Port);
                TcpListener server = new TcpListener(IPAddress.Any, Port);
                server.Start();
                while (IsWorking)
                {
                    try
                    {
                        TcpClient connection = server.AcceptTcpClient();
                        Thread _client = new Thread(Client);
                        //threads.Add(_client);
                        _client.Start(connection);
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
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }

        private static string GenerateToken()
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 40; i++)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("relay-on");
            UnregisterCommand("relay-off");
            UnregisterCommand("token-on");
            UnregisterCommand("token-off");
            UnregisterCommand("token-remind");
            return base.Hook_OnUnload();
        }

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("relay-on", this.relay_on, false, "admin"));
            RegisterCommand(new GenericCommand("relay-off", this.relay_off, false, "admin"));
            RegisterCommand(new GenericCommand("token-remind", this.token_remind, true, "admin"));
            RegisterCommand(new GenericCommand("token-on", this.token_on, false, "admin"));
            RegisterCommand(new GenericCommand("token-off", this.token_off, false, "admin"));
            return base.Hook_OnRegister();
        }

        private void token_off(CommandParams p)
        {
            SetConfig(p.SourceChannel, "NetCat.Token", false);
            p.SourceChannel.SaveConfig();
            IRC.DeliverMessage("This channel will no longer require a token in order to relay messages into it", p.SourceChannel.Name);
        }

        private void token_on(CommandParams p)
        {
            string token = GenerateToken();
            SetConfig(p.SourceChannel, "NetCat.Token", true);
            SetConfig(p.SourceChannel, "NetCat.TokenData", token);
            p.SourceChannel.SaveConfig();
            IRC.DeliverMessage("New token was generated for this channel, and it was sent to you in a private message", p.SourceChannel.Name);
            IRC.DeliverMessage("Token for " + p.SourceChannel.Name + " is: " + token, p.SourceUser.Nick);
        }

        private void token_remind(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "NetCat.Token", false))
            {
                IRC.DeliverMessage("This channel doesn't require a token", p.SourceChannel.Name);
                return;
            }
            string token = GetConfig(p.SourceChannel, "NetCat.TokenData", "<invalid>");
            IRC.DeliverMessage("Token for " + p.SourceChannel.Name + " is: " + token, p.SourceUser.Nick);
        }

        private void relay_on(CommandParams p)
        {
            if (GetConfig(p.SourceChannel, "NetCat.Enabled", false))
            {
                IRC.DeliverMessage("Relay is already enabled", p.SourceChannel.Name);
                return;
            }
            SetConfig(p.SourceChannel, "NetCat.Enabled", true);
            p.SourceChannel.SaveConfig();
            IRC.DeliverMessage("Relay was enabled", p.SourceChannel.Name);
        }

        private void relay_off(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "NetCat.Enabled", false))
            {
                IRC.DeliverMessage("Relay is already disabled", p.SourceChannel.Name);
                return;
            }
            SetConfig(p.SourceChannel, "NetCat.Enabled", false);
            p.SourceChannel.SaveConfig();
            IRC.DeliverMessage("Relay was disabled", p.SourceChannel.Name);
        }
    }
}
