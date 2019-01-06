//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Copyright 2013 - 2018 Petr Bena (benapetr@gmail.com)

namespace wmib
{
    public partial class Commands
    {
        public class Processing
        {
            public static void ProcessCommands(Channel chan, string nick, string ident, string host, string message)
            {
                if (!message.StartsWith(Configuration.System.CommandPrefix, System.StringComparison.InvariantCulture))
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
                    p.Parameters = message.Substring(message.IndexOf(" ", System.StringComparison.InvariantCulture) + 1);
                    p.Command = message.Substring(0, message.IndexOf(" ", System.StringComparison.InvariantCulture));
                }

                GenericCommand command = CommandPool.GetCommand(p.Command);
                if (command != null)
                    command.Launch(p);
            }
        }
    }
}
