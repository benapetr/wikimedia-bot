using System;
namespace wmib.Extensions
{
	public class Time : Module
	{
		public override bool Hook_OnRegister()
		{
			RegisterCommand(new GenericCommand("time", this.time));
			return base.Hook_OnRegister();
		}

		private void time(CommandParams p)
		{
			DateTime time = DateTime.UtcNow;
			IRC.DeliverMessage(p.User.Nick + ": It is is " + time + " UTC", p.SourceChannel);
		}

		public override bool Hook_OnUnload()
		{
			UnregisterCommand("time");
			return base.Hook_OnUnload();
		}

		public override bool Construct()
		{
			this.HasSeparateThreadInstance = false;
			return true;
		}
	}
}

