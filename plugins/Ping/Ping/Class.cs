using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace wmib
{
    public class Class : Module
    {
        public class Info
        {
            public Channel channel;
        }

        public override bool Construct()
        {
            Name = "Ping";
            Version = "1.0";
			HasSeparateThreadInstance = false;
            return true;
        }

        public override void Hook_PRIV(Channel channel, User invoker, string message)
        {
            if (message == Configuration.System.CommandPrefix + "ping")
            {
                Info i = new Info();
                i.channel = channel;
                Thread thread = new Thread(Ping);
                thread.Start(i);
            }
        }

        public void Ping(object info)
        {
            try
            {
                Info i = (Info)info;
                Core.irc.Queue.DeliverMessage("Pinging all local filesystems, hold on", i.channel, IRC.priority.high);
                DateTime blah = DateTime.Now;
                System.IO.File.WriteAllText("/tmp/wm-bot-test", "test");
                System.IO.File.Delete("/tmp/wm-bot-test");
                Core.irc.Queue.DeliverMessage("Written and deleted 4 bytes on /tmp in " + (DateTime.Now - blah).ToString(), i.channel);
                blah = DateTime.Now;
                System.IO.File.WriteAllText("/data/project/wm-bot/wm-bot-test", "test");
                System.IO.File.Delete("/data/project/wm-bot/wm-bot-test");
                Core.irc.Queue.DeliverMessage("Written and deleted 4 bytes on /data/project in " + (DateTime.Now - blah).ToString(), i.channel);
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }
    }
}
