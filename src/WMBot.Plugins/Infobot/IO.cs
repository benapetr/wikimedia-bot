using System.Collections.Generic;
using System;
using System.Threading;
using System.Text;

namespace wmib
{
    [Serializable()]
    public class InfobotWriter : Module
    {
        public override bool Construct()
        {
            Version = "1.2.0";
            Name = "Infobot DB";
            RestartOnModuleCrash = true;
            return true;
        }

        public override void Load()
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
