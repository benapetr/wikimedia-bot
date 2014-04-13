namespace WMBot.Bouncer
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
            Syslog.Log("wm-bnc v. 1.0.0.0");
            
            Server.Connect();
        }
    }
}
