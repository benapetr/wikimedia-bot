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
using System.Text.RegularExpressions;

namespace wmib
{
    public class User
    {
        public string nick;
        public string name;
        public string host;
        public List<string> channels;
        public User(string _nick, string _host)
        {
            nick = _nick;
            host = _host;
        }
    }
}