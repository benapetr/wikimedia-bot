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
using System.Net;
using System.IO;

namespace wmib
{
    [Serializable()]
    public partial class core : MarshalByRefObject
    {
        public class RegexCheck
        {
            public string value;
            public string regex;
            public bool searching;
            public bool result = false;
            public RegexCheck(string Regex, string Data)
            {
                result = false;
                value = Data;
                regex = Regex;
            }
            private void Run()
            {
                try
                {
                    Regex c = new Regex(regex);
                    result = c.Match(value).Success;
                    searching = false;
                }
                catch (ThreadAbortException)
                {
                    searching = false;
                    return;
                }
            }
            public int IsMatch()
            {
                try
                {
                    Thread quick = new Thread(Run);
                    searching = true;
                    quick.Start();
                    int check = 0;
                    while (searching)
                    {
                        check++;
                        Thread.Sleep(10);
                        if (check > 50)
                        {
                            quick.Abort();
                            return 2;
                        }
                    }
                    if (result)
                    {
                        return 1;
                    }
                }
                catch (Exception fail)
                {
                    core.handleException(fail);
                }
                return 0;
            }
        }
    }
}
