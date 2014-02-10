using System;

namespace wmib
{
	public class SystemHooks
	{
		public static void IrcReloadChannelConf(Channel Channel)
		{
			lock(ExtensionHandler.Extensions)
			{
				foreach (Module xx in ExtensionHandler.Extensions)
				{
					try
					{
						if (xx.IsWorking)
						{
							xx.Hook_ReloadConfig(Channel);
						}
					} catch (Exception fail)
					{
						Syslog.Log("MODULE: exception at Hook_Reload in " + xx.Name);
						Core.HandleException(fail);
					}
				}
			}
		}

		public static void IrcKick(Channel Channel, User Source, User Target)
		{
			lock(ExtensionHandler.Extensions)
			{
				foreach (Module module in ExtensionHandler.Extensions)
				{
					if (!module.IsWorking)
					{
						continue;
					}
					try
					{
						module.Hook_Kick(Channel, Source, Target);
					} catch (Exception fail)
					{
						Syslog.Log("MODULE: exception at Hook_Kick in " + module.Name, true);
						Core.HandleException(fail);
					}
				}
			}
		}

		public static void SystemLog(string Message, Syslog.Type MessageType)
		{
			return;
		}
	}
}

