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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web;

namespace wmib
{
    /// <summary>
    /// This is a custom protocol for handling irc requests that is capable of parsing input from
    /// multiple sessions (connections) so that we can use only 1 network instance for all wm-bot
    /// sessions that are connected to target server
    /// </summary>
    public class WmIrcProtocol : libirc.Protocols.ProtocolIrc
    {
        public string BouncerHost = "127.0.0.1";
        public int BouncerPort = 6667;

        public WmIrcProtocol(string ServerHost, string bouncerHost, int bouncerPort) : base()
        {
            this.Server = ServerHost;
            this.BouncerPort = bouncerPort;
            this.BouncerHost = bouncerHost;
        }
    }
}

