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
    public partial class core : MarshalByRefObject
    {
        /// <summary>
        /// This is a class that check if regex matches value while it doesn't affect the system thread
        /// </summary>
        public class RegexCheck
        {
            /// <summary>
            /// Value which is this regex compared with
            /// </summary>
            public string value;
            /// <summary>
            /// Regex
            /// </summary>
            public string regex;
            /// <summary>
            /// Whether it is currently searching
            /// </summary>
            private bool searching = false;
            private bool result = false;

            /// <summary>
            /// Creates a new instance of regex check
            /// </summary>
            /// <param name="Regex"></param>
            /// <param name="Data"></param>
            public RegexCheck(string Regex, string Data)
            {
                result = false;
                value = Data;
                regex = Regex;
            }

            /// <summary>
            /// Start check
            /// </summary>
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

            /// <summary>
            /// Return 1 in case that regex match, 0 if it doesn't, 2 if it timed out, and 800 error
            /// </summary>
            /// <returns></returns>
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
                    return 800;
                }
                return 0;
            }
        }
    }
}
