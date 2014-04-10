using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace WMBot.Bouncer
{
    public struct BufferItem
    {
        public string Text;
        public DateTime _datetime;
        public bool important;
    }

    public static class Buffer
    {
        public static List<BufferItem> OutgoingData = new List<BufferItem>();
        public static List<BufferItem> IncomingData = new List<BufferItem>();

        public static bool Out(string message)
        {
            try
            {
                BufferItem item = new BufferItem();
                item._datetime = DateTime.Now;
                item.Text = message;
                lock (OutgoingData)
                {
                    OutgoingData.Add(item);
                }
                return true;
            }
            catch (Exception fail)
            {
                Console.WriteLine(fail.ToString());
                return false;
            }
        }

        public static bool In(string message, bool control = false)
        {
            try
            {
                BufferItem item = new BufferItem();
                item._datetime = DateTime.Now;
                item.important = control;
                item.Text = message;
                lock (IncomingData)
                {
                    IncomingData.Add(item);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    class Server
    {
        public static string network = "irc.freenode.net";
        public static int port = 6667;

        private static bool IsConnectedOnRemote = false;
        private static System.IO.StreamReader local_reader;
        private static System.IO.StreamWriter local_writer;

        private static System.IO.StreamWriter remote_writer;
        private static System.IO.StreamReader remote_reader;
        private static System.Net.Sockets.NetworkStream stream;
        private static TcpClient client;

        private static System.Threading.Thread listener;
        private static System.Threading.Thread irc;
        private static DateTime Ping;

        public static void Listen()
        {
            TcpListener cache = new TcpListener(IPAddress.Parse("127.0.0.1"), port);

            cache.Start();
            Syslog.Log("Bouncer is listening on port " + port.ToString());
            Ping = DateTime.Now;
            
            while (true)
            {
                client = cache.AcceptTcpClient();
                NetworkStream temp = client.GetStream();
                local_writer = new System.IO.StreamWriter(temp);
                local_reader = new System.IO.StreamReader(temp, System.Text.Encoding.UTF8);
                Syslog.Log("New client has connected to bouncer");

                try
                {
                    while (!local_reader.EndOfStream)
                    {
                        //byte[] text = new byte[8000];
                        //int i = _socket.Receive(text);
                        //text = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, text);
                        string data = local_reader.ReadLine();
                        if (data == "")
                        {
                            continue;
                        }
                        if (!data.StartsWith("CONTROL: "))
                        {
                            Buffer.Out(data);
                        }
                        else
                        {
                            string code = data.Replace("\r", "").Substring("CONTROLxx".Length);
                            string parameter = "";
                            if (code.Contains(" "))
                            {
                                parameter = code.Substring(code.IndexOf(" ") + 1);
                                code = code.Substring(0, code.IndexOf(" "));
                            }
                            switch (code)
                            {
                                case "STATUS":
                                    if (IsConnectedOnRemote)
                                    {
                                        Buffer.In("CONTROL: TRUE", true);
                                    } else
                                    {
                                        Buffer.In("CONTROL: FALSE", true);
                                    }
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
                        System.Threading.Thread.Sleep(20);
                    }
                    Syslog.Log("Client has disconnect on EOF");
                }
                catch (System.IO.IOException)
                {
                    Syslog.Log("Client has disconnected on IOEX term");
                }
                System.Threading.Thread.Sleep(20);
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
                {
                    return false;
                }
                if (server != "")
                {
                    network = server;
                }
                stream = new System.Net.Sockets.TcpClient(network, 6667).GetStream();
                remote_reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
                remote_writer = new System.IO.StreamWriter(stream);
                Ping = DateTime.Now;
                IsConnectedOnRemote = true;
            }
            catch (Exception fail)
            {
                Console.Write(fail.ToString() + "\n");
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
                            System.Threading.Thread.Sleep(20);
                        }
                        SendDisconnectOnRemote();
                    }
                    System.Threading.Thread.Sleep(20);
                }
                catch (System.IO.IOException)
                {
                    SendDisconnectOnRemote();
                }
                System.Threading.Thread.Sleep(10);
            }
        }

        public static void Connect()
        {
            listener = new System.Threading.Thread(Listen);
            listener.Start();
            irc = new System.Threading.Thread(Init);
            irc.Start();
            int ping = 0;
            while (true)
            {
                try
                {
                    if (client != null)
                    {
                        if (client.Connected)
                        {
                            if (Buffer.IncomingData.Count > 0)
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
                        }
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
                            remote_writer.WriteLine("PING :" + DateTime.Now.ToBinary().ToString());
                            remote_writer.Flush();
                        }
                    }
                }
                catch (Exception fail)
                { 
                    Console.Write(fail.ToString());
                }
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
