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
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Text.RegularExpressions;

namespace wmib
{
    /// <summary>
    /// Network user
    /// </summary>
    [Serializable()]
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
        /// Creates a new instance of user
        /// </summary>
        /// <param name="nick"></param>
        /// <param name="host"></param>
        /// <param name="ident"></param>
        public User(string nick, string host, string ident)
        {
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
                return this.Nick.CompareTo((obj as User).Nick);
            }
            return 0;
        }
    }
}