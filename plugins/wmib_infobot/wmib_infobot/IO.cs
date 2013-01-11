using System.Collections.Generic;
using System;
using System.Threading;
using System.Text;

namespace wmib
{
    [Serializable()]
    public class infobot_writer : Module
    {
        public override bool Construct()
        {
            Version = "1.0.0";
            Name = "Infobot DB";
            start = true;
            Reload = true;
            return true;
        }

        public override void Load()
        {
            try
            {
                while (true)
                {
                    SaveData();
                    Thread.Sleep(2000);
                }
            }
            catch (ThreadAbortException)
            {
                SaveData();
            }
        }

        public void SaveData()
        {
            lock (config.channels)
            {
                foreach (config.channel x in config.channels)
                {
                    infobot_core infobot = (infobot_core)x.RetrieveObject("Infobot");
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
