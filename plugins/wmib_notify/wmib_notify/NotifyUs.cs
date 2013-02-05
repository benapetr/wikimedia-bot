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
using System.Text;

namespace wmib
{
    public class RegularModule : Module
    {
        public override bool Construct()
        {
            Name = "Notifications";
            start = true;
            Version = "1.0.0.0";
            return true;
        }

        public override void Hook_Join(config.channel channel, User user)
        {
            Notification result = Notification.RetrieveTarget(user.Nick);
            while (result != null)
            {
                core.irc._SlowQueue.DeliverMessage(result.Source_Name + "! " + user.Nick + " just joined " + channel.Name + " this message was delivered to you, because you requested to be notified about their activity, for more information see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name, IRC.priority.low);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(user.Nick);
            }
        }

        public override void Hook_Nick(config.channel channel, User Target, string OldNick)
        {
            Notification result = Notification.RetrieveTarget(Target.Nick);
            while (result != null)
            {
                core.irc._SlowQueue.DeliverMessage(result.Source_Name + "! " + OldNick + " just changed a nickname to " + Target.Nick + " which you wanted to talk with, in " + channel.Name + ", this message was delivered to you, because you requested to be notified about their activity, for more information see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name, IRC.priority.low);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(Target.Nick);
            }
            result = Notification.RetrieveTarget(OldNick);
            while (result != null)
            {
                core.irc._SlowQueue.DeliverMessage(result.Source_Name + "! " + OldNick + " just changed a nickname to " + Target.Nick + " which you wanted to talk with, in " + channel.Name + ", this message was delivered to you, because you requested to be notified about their activity, for more information see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name, IRC.priority.low);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(OldNick);
            }
        }

        public override void Hook_Kick(config.channel channel, User source, User user)
        {
            Notification result = Notification.RetrieveTarget(user.Nick);
            while (result != null)
            {
                core.irc._SlowQueue.DeliverMessage(result.Source_Name + "! " + user.Nick + " was just kicked from " + channel.Name + ", this message was delivered to you, because you requested to be notified about their activity, for more information see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name, IRC.priority.low);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(user.Nick);
            }
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            Notification result = Notification.RetrieveTarget(invoker.Nick);
            while (result != null)
            {
                core.irc._SlowQueue.DeliverMessage(result.Source_Name + "! " + invoker.Nick + " just said something in " + channel.Name + ", this message was delivered to you, because you requested to be notified about their activity, for more information see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name, IRC.priority.low);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(invoker.Nick);
            }
        }

        public override bool Hook_OnPrivateFromUser(string message, User user)
        {
            Notification result = Notification.RetrieveTarget(user.Nick);
            while (result != null)
            {
                core.irc._SlowQueue.DeliverMessage(result.Source_Name + "! " + user.Nick + " just sent me a private message, this message was delivered to you, because you requested to be notified about their activity, for more information see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name, IRC.priority.low);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(user.Nick);
            }

            if (message.StartsWith("@notify "))
            {
                string parameter = "";
                parameter = message.Substring(message.IndexOf(" ") + 1);
                if (parameter != "")
                {
                    if (!isValid(parameter))
                    {
                        core.irc._SlowQueue.DeliverMessage("I doubt that anyone could have such a nick '" + parameter + "'", user.Nick, IRC.priority.low);
                        return true;
                    }
                    if (Notification.Contains(parameter, user.Nick))
                    {
                        core.irc._SlowQueue.DeliverMessage("You already requested this user to be watched", user.Nick, IRC.priority.low);
                        return true;
                    }
                    lock (config.channels)
                    {
                        foreach (config.channel item in config.channels)
                        {
                            if (item.containsUser(parameter))
                            {
                                core.irc._SlowQueue.DeliverMessage("This user is now online in " + item.Name + " so I will let you know when they show some activity (talk etc)", user.Nick, IRC.priority.low);
                                lock (Notification.NotificationList)
                                {
                                    Notification.NotificationList.Add(new Notification(parameter, user.Nick, user.Host));
                                }
                                return true;
                            }
                        }
                    }
                    lock (Notification.NotificationList)
                    {
                        Notification.NotificationList.Add(new Notification(parameter, user.Nick, user.Host));
                    }
                    core.irc._SlowQueue.DeliverMessage("I will notify you, when I see " + parameter + " around here", user.Nick, IRC.priority.low);
                    return true;
                }
            }
            return false;
        }

        public override void Hook_BeforeSysWeb(ref string html)
        {
            html += "<br>\nNotifications: " + Notification.NotificationList.Count.ToString() + "<br>\n";
        }

        public static bool isValid(string name)
        {
            if (name.Contains(" "))
            {
                return false;
            }
            if (name.Contains("@"))
            {
                return false;
            }
            return true;
        }

        public override void Load()
        {
            try
            {
                while (true)
                {
                    Notification.RemoveOld();
                    Thread.Sleep(360000);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }
    }
}
