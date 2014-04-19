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
    public class WmIrcProtocol : libirc.Protocol
    {
        /// <summary>
        /// List of instances
        /// </summary>
        public static Dictionary<string, Instance> Instances = new Dictionary<string, Instance>();
        /// <summary>
        /// Target's of each instance
        /// </summary>
        public static Dictionary<string, Instance> TargetBuffer = new Dictionary<string, Instance>();
        public static Network Network = null;
        public static WmIrcProtocol Protocol = new WmIrcProtocol();

        public WmIrcProtocol () : base()
        {
        }

		/// <summary>
		/// Creates a new instance
		/// </summary>
		/// <param name="name"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		public static int CreateInstance(string name, int port = 0)
		{
			Syslog.DebugLog("Creating instance " + name + " with port " + port);
			lock(Instances)
			{
				if (Instances.ContainsKey(name))
				{
					throw new Exception("Can't load instance " + name + " because this instance already is present");
				}
				Instances.Add(name, new Instance(name, port));
			}
			return 0;
		}

		/// <summary>
		/// Return instance with lowest number of channels
		/// </summary>
		/// <returns></returns>
		public static Instance GetInstance()
		{
			int lowest = 99999999;
			Instance instance = null;
			// first try to get instance which is online
			lock(Instances)
			{
				foreach (Instance xx in Instances.Values)
				{
					if (xx.IsConnected && xx.IsWorking && xx.ChannelCount < lowest)
					{
						lowest = xx.ChannelCount;
						instance = xx;
					}
				}
			}
			// if there is no such return any instance with low channels
			if (instance == null)
			{
				lock(Instances)
				{
					foreach (Instance xx in Instances.Values)
					{
						if (xx.ChannelCount < lowest)
						{
							lowest = xx.ChannelCount;
							instance = xx;
						}
					}
				}
			}
			return instance;
		}

        public static void ConnectAllIrcInstances()
        {
            Network = new Network(Configuration.IRC.NetworkHost);
            foreach (Instance instance in Instances.Values)
            {
                // connect it to irc
                instance.Init();
            }
            // now we need to wait for all instances to connect
            Syslog.Log("Waiting for all instances to connect to irc");
            bool IsOk = false;
            while (!IsOk)
            {
                foreach (Instance instance in Instances.Values)
                {
                    if (!instance.IsWorking)
                    {
                        Syslog.DebugLog("Waiting for " + instance.Nick, 2);
                        Thread.Sleep(1000);
                        IsOk = false;
                        break;
                    }
                    Syslog.DebugLog("Connected to " + instance.Nick, 6);
                    IsOk = true;
                }
            }

            // wait for all instances to join their channels
            Syslog.Log("Waiting for all instances to join channels");
            IsOk = false;
            while (!IsOk)
            {
                foreach (Instance instance in Instances.Values)
                {
                    if (!instance.irc.ChannelsJoined)
                    {
                        Thread.Sleep(100);
                        IsOk = false;
                        break;
                    }
                    IsOk = true;
                }
            }
            Syslog.Log("All instances joined their channels");
        }
    }
}

