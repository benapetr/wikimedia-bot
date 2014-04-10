using System;

namespace tcp_io
{
    public class Syslog
    {
        public static void Log(string message)
        {
            Console.WriteLine(DateTime.Now.ToString() + ": " + message);
        }
    }
}

