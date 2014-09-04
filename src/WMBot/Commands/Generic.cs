using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public partial class Commands
    {
        public class Generic
        {
            public static void ProcessCommands(Channel chan, string nick, string ident, string host, string message)
            {
                if (!message.StartsWith(Configuration.System.CommandPrefix))
                    return;
                CommandParams p = new CommandParams();
                p.SourceChannel = chan;
                p.User = new libirc.UserInfo(nick, ident, host);
                p.Message = message;
                message = message.Substring(1);
                p.Command = message;
                if (message.Contains(" "))
                {
                    p.Parameters = message.Substring(message.IndexOf(" ") + 1);
                    p.Command = message.Substring(0, message.IndexOf(" "));
                }

                GenericCommand command = CommandPool.GetCommand(p.Command);
                if (command != null)
                    command.Launch(p);
            }
        }
    }
}
