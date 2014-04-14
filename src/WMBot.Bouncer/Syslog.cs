using System;

namespace WMBot.Bouncer
{
    public class Syslog
    {
        public static void Log(string message)
        {
            Console.WriteLine(DateTime.Now + ": " + message);
        }
    }
}

