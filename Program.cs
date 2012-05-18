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
    internal class Program
    {
        public static bool Log(string msg)
        {
            Console.WriteLine("LOG [" + DateTime.Now.ToShortTimeString() + "]: " + msg);
            return false;
        }
        private static void Main(string[] args)
        {
            Log("Loading...");
			config.UpTime = System.DateTime.Now;
            if ( config.Load() != 0)
			{
				return;
			}
            Log("Connecting");
            core.Connect();
        }
    }
}
