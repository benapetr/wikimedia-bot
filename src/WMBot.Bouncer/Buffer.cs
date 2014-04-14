using System;
using System.Collections.Generic;

namespace WMBot.Bouncer
{
    public static class Buffer
    {
        public static List<BufferItem> OutgoingData = new List<BufferItem>();
        public static List<BufferItem> IncomingData = new List<BufferItem>();

        public static bool Out(string message)
        {
            try
            {
                BufferItem item = new BufferItem {_datetime = DateTime.Now, Text = message};
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
                BufferItem item = new BufferItem {_datetime = DateTime.Now, important = control, Text = message};
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
}