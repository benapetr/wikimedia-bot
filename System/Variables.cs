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
using System.Text.RegularExpressions;
using System.IO;

namespace wmib
{
    /// <summary>
    /// variables
    /// </summary>
    public class variables
    {
        /// <summary>
        /// Configuration directory
        /// </summary>
        public static readonly string config = "configuration";
        /// <summary>
        /// Prefix for a log directory
        /// </summary>
        public static readonly string prefix_logdir = "log";
        /// <summary>
        /// This string represent a character that changes color
        /// </summary>
        public static readonly string color = ((char)003).ToString();
        /// <summary>
        /// This string represent a character that changes text to bold
        /// </summary>
        public static readonly string bold = ((char)002).ToString();
    }

    /// <summary>
    /// misc
    /// </summary>
    public class misc
    {
        /// <summary>
        /// Check if a regex is valid
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool IsValidRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;

            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }
}
