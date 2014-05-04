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
using System.Threading;

namespace wmib.Extensions
{
    public class Notify : Module
    {
        public override bool Construct()
        {
            Version = new Version(1, 0, 16, 0);
            return true;
        }

        public override bool Hook_OnUnload()
        {
            Core.Help.Unregister("notify");
            return true;
        }

        public override void Hook_Join(Channel channel, libirc.UserInfo user)
        {
            Notification result = Notification.RetrieveTarget(user.Nick);
            while (result != null)
            {
                IRC.DeliverMessage(result.Source_Name + "! " + user.Nick + " just joined " + channel.Name + ". This message was delivered to you because you asked me to notify you about this user's activity. For more information, see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(user.Nick);
            }
        }

        public override void Hook_Nick(Channel channel, libirc.UserInfo Target, string OldNick)
        {
            Notification result = Notification.RetrieveTarget(Target.Nick);
            while (result != null)
            {
                IRC.DeliverMessage(result.Source_Name + "! " + OldNick + " just changed nicknames to " + 
                                   Target.Nick + " which you wanted to talk with, in " + channel.Name + 
                                   ". This message was delivered to you because you asked me to notify"+
                                   "you about this user's activity. For more information, see "+
                                   "http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(Target.Nick);
            }
            result = Notification.RetrieveTarget(OldNick);
            while (result != null)
            {
                IRC.DeliverMessage(result.Source_Name + "! " + OldNick + " just changed a nickname to " + 
                                   Target.Nick + " which you wanted to talk with, in " + channel.Name + 
                                   ". This message was delivered to you because you asked me to notify"+
                                   " you about this user's activity. For more information, see "+
                                   "http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(OldNick);
            }
            if (Target.Nick.ToLower() != OldNick.ToLower())
            {
                result = Notification.RetrieveSource(OldNick);
                while (result != null)
                {
                    result.Source_Name = Target.Nick;
                    result = Notification.RetrieveSource(OldNick);
                }
            }
        }

        public override void Hook_Kick(Channel channel, libirc.UserInfo source, string user)
        {
            Notification result = Notification.RetrieveTarget(user);
            while (result != null)
            {
                IRC.DeliverMessage(result.Source_Name + "! " + user + " just got kicked from " + channel.Name + ". This message was delivered to you because you asked me to notify you about this user's activity. For more information, see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(user);
            }
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            Notification result = Notification.RetrieveTarget(invoker.Nick);
            while (result != null)
            {
                IRC.DeliverMessage(result.Source_Name + "! " + invoker.Nick + " just said something in " + channel.Name + ". This message was delivered to you because you asked me to notify you about this user's activity. For more information, see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(invoker.Nick);
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "notify "))
            {
                string parameter = message.Substring(message.IndexOf(" ") + 1).Trim();
                if (parameter != "")
                {
                    if (!isValid(parameter))
                    {
                        IRC.DeliverMessage("I doubt that anyone could have such a nick '" + parameter + "'", channel);
                        return;
                    }
                    if (Notification.Contains(parameter, invoker.Nick))
                    {
                        IRC.DeliverMessage("You've already asked me to watch this user", channel);
                        return;
                    }
                    foreach (Channel item in Configuration.ChannelList)
                    {
                        if (item.ContainsUser(parameter))
                        {
                            IRC.DeliverMessage("This user is now online in " + item.Name + ". I'll let you know when they show some activity (talk, etc.)", channel);
                            lock(Notification.NotificationList)
                            {
                                Notification.NotificationList.Add(new Notification(parameter, invoker.Nick, invoker.Host));
                            }
                            return;
                        }
                    }
                    lock (Notification.NotificationList)
                    {
                        Notification.NotificationList.Add(new Notification(parameter, invoker.Nick, invoker.Host));
                    }
                    IRC.DeliverMessage("I'll let you know when I see " + parameter + " around here", channel);
                }
            }
        }

        public override void Hook_Quit(libirc.UserInfo user, string Message)
        {
            Notification result = Notification.RetrieveSource(user.Nick);
            while (result != null)
            {
                lock (Notification.NotificationList)
                {
                    if (Notification.NotificationList.Contains(result))
                    { 
                        Notification.NotificationList.Remove(result);
                    }
                }
                result = Notification.RetrieveSource(user.Nick);
            }
        }

        public override bool Hook_OnPrivateFromUser(string message, libirc.UserInfo user)
        {
            Notification result = Notification.RetrieveTarget(user.Nick);
            while (result != null)
            {
                IRC.DeliverMessage(result.Source_Name + "! " + user.Nick + " just sent me a private message. This message was delivered to you because you asked me to notify you about this user's activity. For more information, see http://meta.wikimedia.org/wiki/WM-Bot", result.Source_Name);
                lock (Notification.NotificationList)
                {
                    Notification.NotificationList.Remove(result);
                }
                result = Notification.RetrieveTarget(user.Nick);
            }

            if (message.StartsWith(Configuration.System.CommandPrefix + "notify "))
            {
                string parameter = message.Substring(message.IndexOf(" ") + 1);
                if (parameter != "")
                {
                    if (!isValid(parameter))
                    {
                        IRC.DeliverMessage("I doubt that anyone could have such a nick '" + parameter + "'", user.Nick);
                        return true;
                    }
                    if (Notification.Contains(parameter, user.Nick))
                    {
                        IRC.DeliverMessage("You've already asked me to watch this user", user.Nick);
                        return true;
                    }
                    foreach (Channel item in Configuration.ChannelList)
                    {
                        if (item.ContainsUser(parameter))
                        {
                            IRC.DeliverMessage("This user is now online in " + item.Name + " so I'll let you know when they show some activity (talk, etc.)", user);
                            lock(Notification.NotificationList)
                            {
                                Notification.NotificationList.Add(new Notification(parameter, user.Nick, user.Host));
                            }
                            return true;
                        }
                    }
                    lock (Notification.NotificationList)
                    {
                        Notification.NotificationList.Add(new Notification(parameter, user.Nick, user.Host));
                    }
                    IRC.DeliverMessage("I'll let you know when I see " + parameter + " around here", user);
                    return true;
                }
            }
            return false;
        }

        public override void Hook_BeforeSysWeb(ref string html)
        {
            html += "<br />\nNotifications: " + Notification.NotificationList.Count;
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

        public override bool Hook_OnRegister()
        {
            Core.Help.Register("notify", "inform you when specified user become active or join some channel in private message");
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
            }
            catch (Exception fail)
            {
                HandleException(fail);
            }
        }
    }
}
