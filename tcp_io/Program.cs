// Part of wmib

using System;
using System.Collections.Generic;
using System.Text;

namespace tcp_io
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Server.network = args[1];
            }

            if (args.Length > 0)
            {
                Server.port = int.Parse(args[0]);
            }
            
            Server.Connect();
        }
    }
}
