using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace wmib
{
    public class r : Module
    {
        private static string Double(int n)
        {
            if (n < 10)
            {
                return "0" + n.ToString();
            }
            return n.ToString();
        }

        public string GetReplag()
        {
            try
            {
                if (!core.DatabaseServerIsAvailable)
                {
                    return "There is no database server to retrieve data from";
                }
                core.DB.Connect();
                string time = core.DB.Select("recentchanges", "rc_timestamp", "order by rc_timestamp desc limit 1", 1);
                core.DB.Disconnect();
                if (time == null)
                {
                    return "ERROR: unable to retrieve the data because " + core.DB.ErrorBuffer;
                }
                DebugLog(time);
                DateTime n = DateTime.Now;
                string x = n.Year.ToString() + Double(n.Month) + Double(n.Day) + Double(n.Hour) + Double(n.Minute) + Double(n.Second);
                DebugLog(x);
                double now = double.Parse(x);
                return (now - double.Parse(time)).ToString() + " seconds";
            }
            catch (Exception fail)
            {
                return "ERROR: unable to retrieve data because: " + fail.ToString();
            }
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (message == "@replag")
            {
                core.irc._SlowQueue.DeliverMessage("Replication lag is approximately " + GetReplag(), channel);
                return;
            }
        }

        public override bool Construct()
        {
            Version = "1.0";
            HasSeparateThreadInstance = false;
            Name = "Replag";
            start = true;
            return true;
        }
    }
}
