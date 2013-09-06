//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena

using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace wmib
{
    public partial class core : MarshalByRefObject
    {
        /// <summary>
        /// System user
        /// </summary>
        [Serializable]
        public class SystemUser
        {
            /// <summary>
            /// Regex
            /// </summary>
            public string name = null;
            /// <summary>
            /// Level
            /// </summary>
            public string level = null;
            /// <summary>
            /// Username of user, this is used for interactive logins
            /// </summary>
            public string UserName = null;
            /// <summary>
            /// Password of user
            /// </summary>
            public string Password = null;
            /// <summary>
            /// If user is global or not
            /// </summary>
            public bool IsGlobal;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="level"></param>
            /// <param name="name"></param>
            public SystemUser(string level, string name, bool Global = false)
            {
                IsGlobal = Global;
                this.level = level;
                this.name = name;
            }
        }
    }
}
