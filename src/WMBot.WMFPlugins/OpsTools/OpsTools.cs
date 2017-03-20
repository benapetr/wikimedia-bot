using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;

namespace wmib
{
    public class OpsTools : Module
    {
        public override bool Construct()
        {
            HasSeparateThreadInstance = false;
            Version = new System.Version(1, 0, 0, 0);
            return true;
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            List<string> channels = new List<string> { "#huggle", "#wikidata", "#wikimedia", "#wikitech", "#wikipedia" };

            if (message.StartsWith("!ops"))
            {
                foreach (string channel_name in channels)
                {
                    if (channel.Name.StartsWith(channel_name))
                    {
                        DebugLog(invoker.ToString() + " used !ops in " + channel.Name + " forwarding message to op bot");
                        IRC.DeliverMessage("OPS " + invoker.ToString() + " " + message, "wmopbot");
                        return;
                    }
                }
            }
        }
    }
}
