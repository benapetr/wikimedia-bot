using System;

namespace wmib
{
    public class Network : libirc.Network
    {
        public Network (string server) : base(server, WmIrcProtocol.Protocol)
        {
        }

		public override void __evt_CTCP (NetworkCTCPEventArgs args)
		{
			switch (args.CTCP)
			{
				case "FINGER":
					Transfer("NOTICE " + args.SourceInfo.Nick + " :" + _Protocol.Separator + "FINGER" + 
				    	     " I am a bot don't finger me");
					return;
				case "TIME":
					Transfer("NOTICE " + args.SourceInfo.Nick + " :" + _Protocol.Separator + "TIME " + DateTime.Now.ToString());
					return;
                case "PING":
                    Transfer("NOTICE " + args.SourceInfo.Nick + " :" + _Protocol.Separator + "PING" + message.Substring(
                        args.Message.IndexOf(_Protocol.Separator + "PING") + 5));
                    return;
                case "VERSION":
                    Transfer("NOTICE " + args.SourceInfo.Nick + " :" + _Protocol.Separator + "VERSION " 
                             + Configuration.System.Version);
                   return;
			}
            Syslog.DebugLog("Ignoring uknown CTCP from " + args.Source + ": " + args.CTCP + args.Message);
		}

		public override void __evt_PRIVMSG (NetworkPRIVMSGEventArgs args)
		{
			if (args.ChannelName == null)
			{
				// private message
			} else
			{
				if (args.IsAct)
				{
					Core.GetAction(args.Message, args.ChannelName, args.SourceInfo.Host, args.SourceInfo.Nick);
					return;
				}
				Core.GetMessage(args.ChannelName, args.SourceInfo.Nick, args.SourceInfo.Host, args.Message);
				continue;
			}
		
				
				// store which instance this message was from so that we can send it using same instance
				lock(WmIrcProtocol.TargetBuffer)
				{
					if (!Core.TargetBuffer.ContainsKey(nick))
					{
						Core.TargetBuffer.Add(nick, ParentInstance);
					} else
					{
						Core.TargetBuffer[nick] = ParentInstance;
					}
				}
				bool respond = !Commands.Trusted(message.Substring(2), nick, host);
				string modules = "";
				lock(ExtensionHandler.Extensions)
				{
					foreach (Module module in ExtensionHandler.Extensions)
					{
						if (module.IsWorking)
						{
							try
							{
								if (module.Hook_OnPrivateFromUser(message.Substring(2), new libirc.UserInfo(nick, Ident, host)))
								{
									respond = false;
									modules += module.Name + " ";
								}
							} catch (Exception fail)
							{
								Core.HandleException(fail);
							}
						}
					}
				}
				if (respond)
				{
					Queue.DeliverMessage("Hi, I am robot, this command was not understood." +
					                     " Please bear in mind that every message you send" +
					                     " to me will be logged for debuging purposes. See" +
					                     " documentation at http://meta.wikimedia.org/wiki" +
					                     "/WM-Bot for explanation of commands", nick,
					                     priority.low);
					Syslog.Log("Ignoring private message: (" + nick + ") " + message.Substring(2), false);
				} else
				{
					Syslog.Log("Private message: (handled by " + modules + " from " + nick + ") " + 
					           message.Substring(2), false);
				}
				continue;
			}
		}
    }
}

