using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace tcp_io
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
            catch (Exception)
            {
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

        private static bool IsConnected = false;
        private static System.IO.StreamReader local_reader;
        private static System.IO.StreamWriter local_writer;

        private static System.IO.StreamWriter _w;
        private static System.IO.StreamReader _r;
        private static System.Net.Sockets.NetworkStream stream;
        private static TcpClient client;

        private static System.Threading.Thread listener;
        private static System.Threading.Thread irc;

        public static void Listen()
        {
            TcpListener cache = new TcpListener(IPAddress.Parse("127.0.0.1"), port);

            cache.Start();
            Console.WriteLine("Cache is up");
            
            while (true)
            {
                client = cache.AcceptTcpClient();
                NetworkStream temp = client.GetStream();
                local_writer = new System.IO.StreamWriter(temp);
                local_reader = new System.IO.StreamReader(temp, System.Text.Encoding.UTF8);

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
                                switch (code)
                                {
                                    case "STATUS":
                                        if (IsConnected)
                                        {
                                            Buffer.In("CONTROL: TRUE", true);
                                        } else
                                        {
                                            Buffer.In("CONTROL: FALSE", true);
                                        }
                                        break;
                                    case "CREATE":
                                        StartIRC();
                                        Console.WriteLine("Connecting wait");
                                        break;
                                }
                            }
                        System.Threading.Thread.Sleep(20);
                    }
                }
                catch (System.IO.IOException)
                {
                    Console.WriteLine("Remote dced");
                }

                System.Threading.Thread.Sleep(20);
            }
        }

        public static bool StartIRC()
        {
            try
            {
                if (IsConnected)
                {
                    return false;
                }
                stream = new System.Net.Sockets.TcpClient(network, 6667).GetStream();
                _r = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
                _w = new System.IO.StreamWriter(stream);
                IsConnected = true;
            }
            catch (Exception fail)
            {
                Console.Write(fail.ToString() + "\n");
                IsConnected = false;
            }
            return false;
        }

        public static void Init()
        {
            while (true)
            {
                try
                {
                    if (IsConnected)
                    {
                        while (!_r.EndOfStream)
                        {
                            string text = _r.ReadLine();
                            Buffer.In(text);
                            System.Threading.Thread.Sleep(20);
                        }
                        Buffer.In("CONTROL: DC");
                        IsConnected = false;
                    }
                }
                catch (System.IO.IOException)
                {
                    IsConnected = false;
                    Buffer.In("CONTROL: DC");
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

                    if (IsConnected)
                    {
                        if (Buffer.OutgoingData.Count > 0)
                        {
                            BufferItem lastitem;
                            lock (Buffer.OutgoingData)
                            {
                                lastitem = Buffer.OutgoingData[0];
                                Buffer.OutgoingData.Remove(lastitem);
                            }
                            _w.WriteLine(lastitem.Text);
                            _w.Flush();
                        }
                    }
                    ping++;
                    if (ping > 2000)
                    {
                        ping = 0;
                        if (IsConnected)
                        {
                            _w.WriteLine("PING :" + Server.network);
                            _w.Flush();
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
