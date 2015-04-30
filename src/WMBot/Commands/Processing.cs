namespace wmib
{
    public partial class Commands
    {
        public class Processing
        {
            public static void ProcessCommands(Channel chan, string nick, string ident, string host, string message)
            {
                if (!message.StartsWith(Configuration.System.CommandPrefix))
                    return;
                CommandParams p = new CommandParams
                {
                    SourceChannel = chan,
                    User = new libirc.UserInfo(nick, ident, host),
                    Message = message
                };
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
