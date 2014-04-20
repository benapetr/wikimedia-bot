//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Threading;

namespace wmib.Extensions
{
    public class InfobotWriter
    {
        public Thread thread;
        
        public void Init()
        {
            thread = new Thread(Worker) {Name = "Module:Infobot/Worker"};
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
