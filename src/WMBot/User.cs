//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System;

namespace wmib
{
    /// <summary>
    /// Network user, keep in mind that bot is also recognizing own users
    /// that are defined in System/User.cs only for core
    /// </summary>
    [Serializable]
    public class User : IComparable
    {
        /// <summary>
        /// Hostname
        /// </summary>
        public string Host;
        /// <summary>
        /// Ident
        /// </summary>
        public string Ident;
        /// <summary>
        /// Nickname
        /// </summary>
        public string Nick;

        /// <summary>
        /// Mode
        /// </summary>
        public NetworkMode ChannelMode = new NetworkMode();

        /// <summary>
        /// If user is opped
        /// </summary>
        public bool IsOperator
        {
            get
            {
                if (ChannelMode != null)
                {
                    if (ChannelMode._Mode.Contains("o"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Change a user level according to symbol
        /// </summary>
        /// <param name="symbol"></param>
        public void SymbolMode(char symbol)
        {
            if (symbol == '\0')
            {
                return;
            }

            if (Core.irc.UChars.Contains(symbol))
            {
                char mode = Core.irc.CUModes[Core.irc.UChars.IndexOf(symbol)];
                ChannelMode.ChangeMode("+" + mode.ToString());
            }
        }

        /// <summary>
        /// Creates a new instance of user
        /// </summary>
        /// <param name="nick"></param>
        /// <param name="host"></param>
        /// <param name="ident"></param>
        public User(string nick, string host, string ident)
        {
            if (nick != "")
            {
                char prefix = nick[0];
                if (Core.irc.UChars.Contains(prefix))
                {
                    SymbolMode(prefix);
                    nick = nick.Substring(1);
                }
            }
            Nick = nick;
            Ident = ident;
            Host = host;
        }

        /// <summary>
        /// Internal function
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj is User)
            {
                return Nick.CompareTo((obj as User).Nick);
            }
            return 0;
        }
    }
}