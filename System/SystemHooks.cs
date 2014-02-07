using System;

namespace wmib
{
	public class SystemHooks
	{
		public static void IrcReloadChannelConf(Channel Channel)
		{
			lock(Module.module)
			{
				foreach (Module xx in Module.module)
				{
					try
					{
						if (xx.working)
						{
							xx.Hook_ReloadConfig(Channel);
						}
					} catch (Exception fail)
					{
						Syslog.Log("Crash on Hook_Reload in " + xx.Name);
						Core.HandleException(fail);
					}
				}
			}
		}

		public static void IrcKick(Channel Channel, User Source, User Target)
		{
			lock(Module.module)
			{
				foreach (Module module in Module.module)
				{
					if (!module.working)
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

		public static void SystemLog(string Message, bool Warning)
		{
			return;
		}
	}
}

