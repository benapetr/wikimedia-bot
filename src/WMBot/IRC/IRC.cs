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
using System.Text;
using System.Threading;

namespace wmib
{
    public partial class IRC
    {
        /// <summary>
        /// If this is not true it means bot did not yet finish connecting or joining to all networks
        /// </summary>
        private static bool finishedJoining = false;
        public static bool FinishedJoining
        {
            get
            {
                return finishedJoining;
            }
        }

        public static void DeliverMessage(string text, Channel target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal)
        {
            if (!target.Suppress)
            {
                Self(text, target);
                target.PrimaryInstance.Network.Message(text, target.Name, priority);
            }
        }

        public static void DeliverMessage(string text, libirc.UserInfo target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal)
        {
            // this is a private message
            lock (Instance.TargetBuffer)
            {
                if (Instance.TargetBuffer.ContainsKey(target.Nick))
                {
                    Instance.TargetBuffer[target.Nick].Network.Message(text, target.Nick, priority);
                    return;
                }
            }
            Instance.PrimaryInstance.Network.Message(text, target.Nick, priority);
        }

        public static void DeliverAction(string text, Channel target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal)
        {
            if (!target.Suppress)
            {
                SelfAct(text, target);
                target.PrimaryInstance.Network.Act(text, target.Name, priority);
            }
        }

        private static void dm(string text, string target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal, bool is_act = false)
        {
            // get a target instance
            if (target.StartsWith("#"))
            {
                // it's a channel
                Channel ch = Core.GetChannel(target);
                if (ch == null)
                {
                    Syslog.Log("Not sending message to unknown channel: " + target);
                    return;
                }
                if (!ch.PrimaryInstance.IsConnected)
                {
                    Syslog.Log("Not sending message using disconnected instance: " + ch.PrimaryInstance.Nick + " target: " + target + " message: " + text);
                    return;
                }
                if (!ch.Suppress)
                {
                    Self(text, ch);
                    if (!is_act)
                    {
                        ch.PrimaryInstance.Network.Message(text, target, priority);
                    }
                    else
                    {
                        ch.PrimaryInstance.Network.Act(text, target, priority);
                    }
                }
            }
            else
            {
                lock (Instance.TargetBuffer)
                {
                    if (Instance.TargetBuffer.ContainsKey(target))
                    {
                        if (is_act)
                        {
                            Instance.TargetBuffer[target].Network.Act(text, target, priority);
                        }
                        else
                        {
                            Instance.TargetBuffer[target].Network.Message(text, target, priority);
                        }
                        return;
                    }
                }
                if (!is_act)
                    Instance.PrimaryInstance.Network.Message(text, target, priority);
                else
                    Instance.PrimaryInstance.Network.Act(text, target, priority);
            }
        }

        public static void DeliverAction(string text, string target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal)
        {
            dm(text, target, priority, true);
        }

        public static void DeliverMessage(string text, string target, libirc.Defs.Priority priority = libirc.Defs.Priority.Normal)
        {
            dm(text, target, priority);
        }

        /// <summary>
        /// Write a self message to modules
        /// </summary>
        /// <param name="message">Message.</param>
        private static void Self(string message, Channel channel)
        {
            foreach (Module module in ExtensionHandler.ExtensionList)
            {
                try
                {
                    if (module.IsWorking)
                    {
                        module.Hook_OnSelf(channel, new libirc.UserInfo(Configuration.IRC.NickName, "wmib", "wikimedia/bot/wm-bot"), message);
                    }
                }
                catch (Exception fail)
                {
                    Core.HandleException(fail, module.Name);
                }
            }
        }

        private static void SelfAct(string message, Channel channel)
        {
            foreach (Module module in ExtensionHandler.ExtensionList)
            {
                try
                {
                    if (module.IsWorking)
                    {
                        module.Hook_OnSelf(channel, new libirc.UserInfo(Configuration.IRC.NickName, "wmib", "wikimedia/bot/wm-bot"), message, true);
                    }
                }
                catch (Exception fail)
                {
                    Core.HandleException(fail, module.Name);
                }
            }
        }

        /// <summary>
        /// Connect to network
        /// </summary>
        public static void Connect()
        {
            Instance.ConnectAllIrcInstances();
            finishedJoining = true;
            InitialiseList();
        }

        private static void InitialiseList()
        {
            Thread thread = new Thread(ChannelList);
            thread.Name = "IRC/ChannelList";
            Core.ThreadManager.RegisterThread(thread);
            thread.Start();
        }

        /// <summary>
        /// This function will retrieve a list of users in a channel for every channel that doesn't have it so far
        /// </summary>
        private static void ChannelList()
        {
            try
            {
                while (Core.IsRunning)
                {
                    foreach (Channel channel in Configuration.ChannelList)
                    {
                        if (!channel.HasFreshUserList && channel.PrimaryInstance != null && channel.PrimaryInstance.Network != null)
                        {
                            channel.PrimaryInstance.Network.Transfer("WHO " + channel.Name, libirc.Defs.Priority.Low);
                            Thread.Sleep(1000);
                        }
                    }
                    // take stolen nick
                    foreach (Instance instance in Instance.Instances.Values)
                    {
                        if (instance.Nick != instance.Network.Nickname)
                        {
                            instance.Network.Transfer("NICK " + instance.Nick);
                        }
                    }
                    Thread.Sleep((Configuration.Channels.Count * 2000) + 80000);
                }
            }
            catch (ThreadAbortException)
            {
                Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
                return;
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
        }
    }
}
