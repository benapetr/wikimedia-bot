using System.Collections.Generic;
using System;
using System.Threading;
using System.Text;

namespace wmib
{
    public class InfobotWriter
    {
		public Thread thread;
		
		public void Init()
		{
			thread = new Thread(Worker);
			thread.Name = "Module:Infobot/Worker";
			Core.ThreadManager.RegisterThread(thread);
			thread.Start();
		}
		
        private void Worker()
        {
            try
            {
                while (true)
                {
                    SaveData();
                    Thread.Sleep(20000);
                }
            }
            catch (ThreadAbortException)
            {
                SaveData();
            }
            catch (Exception fail)
            {
                Core.HandleException(fail, "infobot");
            }
			Core.ThreadManager.UnregisterThread(thread);
        }

        public void SaveData()
        {
            lock (Configuration.Channels)
            {
                foreach (Channel x in Configuration.Channels)
                {
                    Infobot infobot = (Infobot)x.RetrieveObject("Infobot");
                    if (infobot != null)
                    {
                        if (infobot.stored == false)
                        {
                            infobot.stored = true;
                            infobot.Save();
                        }
                    }
                }
            }
        }
    }
}
