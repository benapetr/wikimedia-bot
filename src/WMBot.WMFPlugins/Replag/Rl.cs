using System;

namespace wmib
{
    public class Replag : Module
    {
        private static string Double(int n)
        {
            if (n < 10)
            {
                return "0" + n;
            }
            return n.ToString();
        }

        public string GetReplag()
        {
            try
            {
                if (!Core.DatabaseServerIsAvailable)
                {
                    return "There is no database server to retrieve data from";
                }
                Core.DB.Connect();
                string time = Core.DB.Select("recentchanges", "rc_timestamp", "order by rc_timestamp desc limit 1")[0][0];
                Core.DB.Disconnect();
                if (time == null)
                {
                    return "ERROR: unable to retrieve the data because " + Core.DB.ErrorBuffer;
                }
                DateTime n = DateTime.Now;
                DateTime replica = new DateTime(int.Parse(time.Substring(0, 4)), int.Parse(time.Substring(4, 2)), int.Parse(time.Substring(6, 2)), int.Parse(time.Substring(8, 2)), int.Parse(time.Substring(10, 2)), int.Parse(time.Substring(12, 2)));
                //string x = n.Year.ToString() + Double(n.Month) + Double(n.Day) + Double(n.Hour) + Double(n.Minute) + Double(n.Second);
                return (n - replica).ToString();
            }
            catch (Exception fail)
            {
                return "ERROR: unable to retrieve data because: " + fail;
            }
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (message == "@replag")
            {
                IRC.DeliverMessage("Replication lag is approximately " + GetReplag(), channel);
            }
        }

        public override bool Construct()
        {
            HasSeparateThreadInstance = false;
            return true;
        }
    }
}
