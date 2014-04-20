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

namespace WMBot.Bouncer
{
    class Server
    {
        public static string network = "irc.freenode.net";
        public static int port = 6667;
        private static bool IsConnectedOnRemote;
        private static StreamReader local_reader;
        private static StreamWriter local_writer;
        private static StreamWriter remote_writer;
        private static StreamReader remote_reader;
        private static NetworkStream stream;
        private static TcpClient client;
        private static Thread listener;
        private static Thread irc;
        private static DateTime Ping;

        public static void Listen()
        {
            TcpListener cache = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            cache.Start();
            Syslog.Log("Bouncer is listening on port " + port);
            Ping = DateTime.Now;
            while (true)
            {
                client = cache.AcceptTcpClient();
                NetworkStream temp = client.GetStream();
                local_writer = new StreamWriter(temp);
                local_reader = new StreamReader(temp, Encoding.UTF8);
                Syslog.Log("New client has connected to bouncer");
                try
                {
                    while (!local_reader.EndOfStream)
                    {
                        string data = local_reader.ReadLine();
                        if (string.IsNullOrEmpty(data))
                        {
                            continue;
                        }
                        if (data[0] != 'C' || !data.StartsWith("CONTROL: "))
                        {
                            Buffer.Out(data);
                        }
                        else
                        {
                            string code = data.Replace("\r", "").Substring("CONTROLxx".Length);
                            string parameter = "";
                            if (code.Contains(" "))
                            {
                                int sidx = code.IndexOf(" ");
                                parameter = code.Substring(sidx + 1);
                                code = code.Substring(0, sidx);
                            }
                            switch (code)
                            {
                                case "STATUS":
                                    if (IsConnectedOnRemote)
                                        Buffer.In("CONTROL: TRUE", true);
                                    else
                                        Buffer.In("CONTROL: FALSE", true);
                                    break;
                                case "CONNECT":
                                case "CREATE":
                                    Syslog.Log("Connecting to remote server: " + parameter);
                                    StartIRC(parameter);
                                    break;
                                case "DISCONNECT":
                                    Disconnect();
                                    SendDisconnectOnRemote();
                                    break;
                            }
                        }
                        Thread.Sleep(20);
                    }
                    Syslog.Log("Client has disconnect on EOF");
                }
                catch (IOException)
                {
                    Syslog.Log("Client has disconnected on IOEX term");
                }
                Thread.Sleep(20);
            }
        }

        private static void SendDisconnectOnRemote()
        {
            Buffer.In("CONTROL: DC");
            IsConnectedOnRemote = false;
        }

        private static void Disconnect()
        {
            if (IsConnectedOnRemote)
            {
                Syslog.Log("Disconnecting from remote " + network);
                IsConnectedOnRemote = false;
                stream.Close();
                remote_writer.Close();
                remote_reader.Close();
            }
        }

        private static bool StartIRC(string server)
        {
            try
            {
                if (IsConnectedOnRemote)
                    return false;
                if (server != "")
                    network = server;
                stream = new TcpClient(network, 6667).GetStream();
                remote_reader = new StreamReader(stream, Encoding.UTF8);
                remote_writer = new StreamWriter(stream);
                Ping = DateTime.Now;
                IsConnectedOnRemote = true;
            }
            catch (Exception fail)
            {
                Console.Write(fail + "\n");
                IsConnectedOnRemote = false;
            }
            return false;
        }

        public static void Init()
        {
            while (true)
            {
                try
                {
                    if (IsConnectedOnRemote)
                    {
                        while (!remote_reader.EndOfStream)
                        {
                            string text = remote_reader.ReadLine();
                            Ping = DateTime.Now;
                            Buffer.In(text);
                            Thread.Sleep(20);
                        }
                        SendDisconnectOnRemote();
                    }
                    Thread.Sleep(20);
                }
                catch (IOException)
                {
                    SendDisconnectOnRemote();
                }
                Thread.Sleep(10);
            }
        }

        public static void Connect()
        {
            listener = new Thread(Listen);
            listener.Start();
            irc = new Thread(Init);
            irc.Start();
            int ping = 0;
            while (true)
            {
                try
                {
                    if (client != null && client.Connected && Buffer.IncomingData.Count > 0)
                    {
                        BufferItem lastitem;
                        lock (Buffer.IncomingData)
                        {
                            lastitem = Buffer.IncomingData[0];
                            foreach (BufferItem Item in Buffer.IncomingData)
                            {
                                if (Item.important)
                                {
                                    lastitem = Item;
                                    break;
                                }
                            }
                            Buffer.IncomingData.Remove(lastitem);
                        }
                        local_writer.WriteLine(lastitem.Text);
                        local_writer.Flush();
                    }

                    if (IsConnectedOnRemote)
                    {
                        if (Buffer.OutgoingData.Count > 0)
                        {
                            BufferItem lastitem;
                            lock (Buffer.OutgoingData)
                            {
                                lastitem = Buffer.OutgoingData[0];
                                Buffer.OutgoingData.Remove(lastitem);
                            }
                            remote_writer.WriteLine(lastitem.Text);
                            remote_writer.Flush();
                        }
                        ping++;
                        if (ping > 2000)
                        {
                            if ((DateTime.Now - Ping).Minutes > 2)
                            {
                                // no response from server within 2 minutes
                                SendDisconnectOnRemote();
                                Syslog.Log("Remote didn't respond for long time, closing connection");
                                Disconnect();
                                ping = 0;
                                continue;
                            }
                            ping = 0;
                            remote_writer.WriteLine("PING :" + DateTime.Now.ToBinary());
                            remote_writer.Flush();
                        }
                    }
                }
                catch (Exception fail)
                { 
                    Console.Write(fail.ToString());
                }
                Thread.Sleep(10);
            }
        }
    }
}
