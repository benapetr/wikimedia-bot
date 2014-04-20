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
using System.Collections.Generic;

namespace WMBot.Bouncer
{
    public static class Buffer
    {
        public static List<BufferItem> OutgoingData = new List<BufferItem>();
        public static List<BufferItem> IncomingData = new List<BufferItem>();

        public static bool Out(string message)
        {
            try
            {
                BufferItem item = new BufferItem {_datetime = DateTime.Now, Text = message};
                lock (OutgoingData)
                {
                    OutgoingData.Add(item);
                }
                return true;
            }
            catch (Exception fail)
            {
                Console.WriteLine(fail.ToString());
                return false;
            }
        }

        public static bool In(string message, bool control = false)
        {
            try
            {
                BufferItem item = new BufferItem {_datetime = DateTime.Now, important = control, Text = message};
                lock (IncomingData)
                {
                    IncomingData.Add(item);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
