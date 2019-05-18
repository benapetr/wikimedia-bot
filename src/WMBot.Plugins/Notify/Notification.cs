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

namespace wmib.Extensions
{
    public class Notification
    {
        public static List<Notification> NotificationList = new List<Notification>();
        public string Target = null;
        public string Message = null;
        public DateTime Expiry;
        public string Source_Name = null;
        public string Source_Host = null;

        public Notification(string target, string source, string host, string text = null)
        {
            this.Target = target;
            this.Source_Host = host;
            this.Source_Name = source;
            this.Message = text;
            this.Expiry = DateTime.Now.AddDays(20);
        }

        public static Notification RetrieveTarget(string Target)
        {
            string target = Target.ToLower();
            lock (NotificationList)
            { 
                foreach (Notification x in NotificationList)
                {
                    if (x.Target.ToLower() == target)
                    {
                        return x;
                    }
                }
            }
            return null;
        }

        public static Notification RetrieveSource(string Source)
        {
            string target = Source.ToLower();
            lock (NotificationList)
            {
                foreach (Notification x in NotificationList)
                {
                    if (x.Source_Name.ToLower() == target)
                    {
                        return x;
                    }
                }
            }
            return null;
        }

        public static bool Contains(string Target, string Source)
        {
            Target = Target.ToUpper();
            Source = Source.ToUpper();
            lock (NotificationList)
            {
                foreach (Notification x in NotificationList)
                {
                    if (x.Target.ToUpper() == Target)
                    {
                        if (Source == x.Source_Name.ToUpper())
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void RemoveOld()
        {
            lock (NotificationList)
            {
                List<Notification> deleted = new List<Notification>();
                foreach (Notification x in NotificationList)
                {
                    if (x.Expiry < DateTime.Now)
                    {
                        deleted.Add(x);
                    }
                }
                foreach (Notification x in deleted)
                {
                    if (NotificationList.Contains(x))
                    {
                        NotificationList.Remove(x);
                    }
                }
            }
        }
    }
}
