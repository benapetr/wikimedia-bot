//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace wmib.Extensions
{
    public class Change
    {
        public string Site;
        public string Page;
        public string Summary;
        public string User;
        public bool Bot = false;
        public bool Minor = false;
        public string Size = null;
        public bool New = false;
        public string oldid = null;
        public string diff = null;
        public bool Special = true;

        public Change(string _Page, string _Description, string _User)
        {
            Summary = _Description;
            User = _User;
            Page = _Page;
        }
    }
}
