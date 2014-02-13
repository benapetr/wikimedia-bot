using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Text;

namespace wmib
{
    public partial class Infobot
    {
        public class InfobotKey
        {
            /// <summary>
            /// Text
            /// </summary>
            public string Text;

            /// <summary>
            /// Key
            /// </summary>
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
            /// <param name="Key">Key</param>
            /// <param name="Text">Text of the key</param>
            /// <param name="_User">User who created the key</param>
            /// <param name="Lock">If key is locked or not</param>
            public InfobotKey(string key, string text, string _User, string Lock = "false", string date = "", string time = "", int Number = 0, bool RAW = false)
            {
                Text = text;
                Key = key;
                IsLocked = Lock;
                User = _User;
                Raw = RAW;
                Displayed = Number;
                if (time == "")
                {
                    LastTime = NA;
                }
                else
                {
                    LastTime = DateTime.FromBinary(long.Parse(time));
                }
                if (date == "")
                {
                    CreationTime = DateTime.Now;
                }
                else
                {
                    CreationTime = DateTime.FromBinary(long.Parse(date));
                }
            }
        }

        public class InfobotAlias
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name;

            /// <summary>
            /// Key
            /// </summary>
            public string Key;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="name">Alias</param>
            /// <param name="key">Key</param>
            public InfobotAlias(string name, string key)
            {
                Name = name;
                Key = key;
            }
        }

        public class InfoItem
        {
            /// <summary>
            /// Channel
            /// </summary>
            public Channel _Channel = null;
            /// <summary>
            /// User
            /// </summary>
            public string User = null;
            /// <summary>
            /// Name
            /// </summary>
            public string Name = null;
            /// <summary>
            /// Host
            /// </summary>
            public string Host = null;
        }
    }
}

