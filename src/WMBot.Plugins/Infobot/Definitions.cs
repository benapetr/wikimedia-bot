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

namespace wmib.Extensions
{
    public partial class Infobot
    {
        public class InfobotKey
        {
            public string Text;
            public string Key;

            /// <summary>
            /// User who created this key
            /// </summary>
            public string User;

            /// <summary>
            /// If this key is locked or not
            /// </summary>
            public string IsLocked;

            /// <summary>
            /// Creation time of key
            /// </summary>
            public DateTime CreationTime;
            public bool IsAct = false;

            /// <summary>
            /// If key is raw or not
            /// </summary>
            public bool Raw;

            /// <summary>
            /// How many times it was displayed
            /// </summary>
            public int Displayed = 0;

            /// <summary>
            /// Last time when a key was displayed
            /// </summary>
            public DateTime LastTime;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="key">Key</param>
            /// <param name="text">Text of the key</param>
            /// <param name="_User">User who created the key</param>
            /// <param name="Lock">If key is locked or not</param>
            /// <param name="date"></param>
            /// <param name="time"></param>
            /// <param name="Number"></param>
            /// <param name="RAW"></param>
            /// <param name="act"></param>
            public InfobotKey(string key, string text, string _User, string Lock = "false", string date = "", string time = "", int Number = 0, bool RAW = false, bool act = false)
            {
                this.Text = text;
                this.Key = key;
                this.IsLocked = Lock;
                this.User = _User;
                this.Raw = RAW;
                this.IsAct = act;
                Displayed = Number;
                if (string.IsNullOrEmpty(time))
                    LastTime = NA;
                else
                    LastTime = DateTime.FromBinary(long.Parse(time));

                if (string.IsNullOrEmpty(date))
                    CreationTime = DateTime.Now;
                else
                    CreationTime = DateTime.FromBinary(long.Parse(date));

            }
        }

        public class InfobotAlias
        {
            public string Name;
            public string Key;

            public InfobotAlias(string name, string key)
            {
                Name = name;
                Key = key;
            }
        }

        public class InfoItem
        {
            public Channel Channel = null;
            public libirc.UserInfo User = null;
            public string Name = null;
        }
    }
}

