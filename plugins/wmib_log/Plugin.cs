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
using System.IO;
using System.Text;

namespace wmib
{
    public class RegularModule : Module
    {
        public struct Job
        {
            public DateTime time;
            public string message;
            public Channel channel;
        }

        public class Item
        {
            public string username = null;
            public DateTime time;
            public string message = null;
            public bool act = false;
            // 0 chat
            // 1 quit
            // 2 join
            // 3 part
            // 4 kick
            // 5 topic change
            // 6 nick change
            public int type;
            public string host;
            public Channel channel;
        }

        public string TextPath = "logs" + Path.DirectorySeparatorChar;
        //private List<char> Separator = new List<char> { ' ', ',', (char)3, '(', ')', '{', '}', (char)2, '<', '>' };
        private bool Unloading = false;
        private List<Job> jobs = new List<Job>();
        private List<Item> DJ = new List<Item>();

        public override void Hook_ACTN(Channel channel, User invoker, string message)
        {
            ChanLog(message, channel, invoker.Nick, invoker.Host, false);
        }

        public override void Hook_PRIV(Channel channel, User invoker, string message)
        {
            ChanLog(message, channel, invoker.Nick, invoker.Host);
            if (message == Configuration.System.CommandPrefix + "logon")
            {
                if (channel.SystemUsers.IsApproved(invoker, "admin"))
                {
                    if (Module.GetConfig(channel, "Logging.Enabled", false))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("ChannelLogged", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("LoggingOn", channel.Language), channel.Name);
                        Module.SetConfig(channel, "Logging.Enabled", true);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == Configuration.System.CommandPrefix + "logoff")
            {
                if (channel.SystemUsers.IsApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!Module.GetConfig(channel, "Logging.Enabled", false))
                    {
                        Core.irc.Queue.DeliverMessage(messages.Localize("LogsE1", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        Module.SetConfig(channel, "Logging.Enabled", false);
                        channel.SaveConfig();
                        Core.irc.Queue.DeliverMessage(messages.Localize("NotLogged", channel.Language), channel.Name);
                        return;
                    }
                }
                if (!channel.SuppressWarnings)
                {
                    Core.irc.Queue.DeliverMessage(messages.Localize("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }
        }

        private int WriteData()
        {
            if (jobs.Count > 0)
            {
                List<Job> line = new List<Job>();
                List<Job> tr = new List<Job>();
                lock (jobs)
                {
                    line.AddRange(jobs);
                    jobs.Clear();
                }
                // remove them from queue
                foreach (Job curr in tr)
                {
                    line.Remove(curr);
                }
                // write to disk
                foreach (Job curr in line)
                {
                    while (!WriteLog(curr.message, curr.channel, curr.time))
                    {
                        Thread.Sleep(2000);
                    }
                }
            }
            return 2;
        }

        public override void Hook_Nick(Channel channel, User Target, string OldNick)
        {
            if (Module.GetConfig(channel, "Logging.Enabled", false))
            {
                Item item = new Item();
                item.channel = channel;
                item.act = false;
                item.host = Target.Host;
                item.message = OldNick;
                item.time = DateTime.Now;
                item.type = 6;
                item.username = Target.Nick;
                lock (DJ)
                {
                    DJ.Add(item);
                }
            }
        }

        public override void Hook_Part(Channel channel, User user)
        {
            if (Module.GetConfig(channel, "Logging.Enabled", false))
            {
                Item item = new Item();
                item.channel = channel;
                item.act = false;
                item.host = user.Host;
                item.message = "";
                item.time = DateTime.Now;
                item.type = 3;
                item.username = user.Nick;
                lock (DJ)
                {
                    DJ.Add(item);
                }
            }
        }

        public override void Hook_ChannelQuit(Channel channel, User user, string mesg)
        {
            if (Module.GetConfig(channel, "Logging.Enabled", false))
            {
                Item item = new Item();
                item.channel = channel;
                item.act = false;
                item.host = user.Host;
                item.message = mesg;
                item.time = DateTime.Now;
                item.type = 1;
                item.username = user.Nick;
                lock (DJ)
                {
                    DJ.Add(item);
                }
            }
        }

        public override void Hook_Kick(Channel channel, User source, User user)
        {
            if (Module.GetConfig(channel, "Logging.Enabled", false))
            {
                Item item = new Item();
                item.channel = channel;
                item.act = false;
                item.host = user.Host;
                item.message = source.Nick;
                item.time = DateTime.Now;
                item.type = 4;
                item.username = user.Nick;
                lock (DJ)
                {
                    DJ.Add(item);
                }
            }
        }

        public override void Hook_Join(Channel channel, User user)
        {
            if (Module.GetConfig(channel, "Logging.Enabled", false))
            {
                Item item = new Item();
                item.channel = channel;
                item.act = false;
                item.host = user.Host;
                item.message = "";
                item.time = DateTime.Now;
                item.type = 2;
                item.username = user.Nick;
                lock (DJ)
                {
                    DJ.Add(item);
                }
            }
        }

        private void Finish()
        {
            if (jobs.Count != 0)
            {
                Syslog.Log("Logging was requested to stop, but there is still " + jobs.Count.ToString() + " lines, writing now");
            }
            WriteData();
            Syslog.Log("There are no unsaved data, we can disable this module now");
            Unloading = false;
        }

        public void Writer()
        {
            try
            {
                if (!Core.DatabaseServerIsAvailable)
                {
                    Log("No sql server is available, closing DB log writer");
                    return;
                }
                if (Core.DB == null)
                {
                    Log("core.DB is null", true);
                }
                DebugLog("The SQL writer started");
                while (Core.IsRunning)
                {
                    string message = "";
                    try
                    {
                        Thread.Sleep(2000);
                        if (DJ.Count > 0)
                        {
                            List<Item> db = new List<Item>();
                            lock (DJ)
                            {
                                db.AddRange(DJ);
                                DJ.Clear();
                            }
                            lock (Core.DB.DatabaseLock)
                            {
                                Core.DB.Connect();
                                while (!Core.DB.IsConnected)
                                {
                                    if (Core.DB.ErrorBuffer != null)
                                    {
                                        Log("Unable to connect to SQL server: " + Core.DB.ErrorBuffer + " retrying in 20 seconds");
                                    }
                                    Thread.Sleep(20000);
                                    Core.DB.Connect();
                                }
                                foreach (Item item in db)
                                {
                                    Database.Row row = new Database.Row();
                                    message = item.message;
                                    row.Values.Add(new Database.Row.Value(0));
                                    row.Values.Add(new Database.Row.Value(item.channel.Name, Database.DataType.Varchar));
                                    row.Values.Add(new Database.Row.Value(item.username, Database.DataType.Varchar));
                                    row.Values.Add(new Database.Row.Value(item.time));
                                    row.Values.Add(new Database.Row.Value(item.act));
                                    row.Values.Add(new Database.Row.Value(item.message, Database.DataType.Varchar));
                                    row.Values.Add(new Database.Row.Value(item.type));
                                    row.Values.Add(new Database.Row.Value(item.host, Database.DataType.Varchar));
                                    if (!Core.DB.InsertRow("logs", row))
                                    {
                                        Log("Failed to insert row: " + message);
                                    }
                                }
                                Core.DB.Commit();
                                Core.DB.Disconnect();
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        if (Core.DatabaseServerIsAvailable)
                        {
                            Core.DB.Commit();
                            Core.DB.Disconnect();
                        }
                        Log("SQL Writer is shut down with " + DJ.Count.ToString() + " unfinished lines");
                        return;
                    }
                    catch (Exception fail)
                    {
                        HandleException(fail);
                        Log("SQL Writer error: " + message, true);
                    }
                }
            }
            catch (Exception fail)
            {
                HandleException(fail);
                Core.ThreadManager.UnregisterThread(Thread.CurrentThread);
                Log("SQL Writer is down!!", true);
            }
        }

        public override void Load()
        {
            if (!Directory.Exists(TextPath))
            {
                Log("Creating a directory for text logs");
                Directory.CreateDirectory(TextPath);
            }
            Thread sql = new Thread(Writer);
            sql.Name = "Module:Logs/SqlWriter";
            Core.ThreadManager.RegisterThread(sql);
            sql.Start();
            Log("Writer thread started");
            int timer = 0;
            while (!Unloading)
            {
                try
                {
                    timer = 0;
                    WriteData();
                    while (!Unloading)
                    {
                        timer++;
                        if (timer > 20)
                        {
                            break;
                        }
                        Thread.Sleep(900);
                    }
                }
                catch (ThreadAbortException)
                {
                    Finish();
                    Log("Writer thread stopped");
                    return;
                }
                catch (Exception fail)
                {
                    HandleException(fail);
                }
            }
            if (Unloading)
            {
                Finish();
            }
            Log("Writer thread stopped");
        }

        /// <summary>
        /// Start work
        /// </summary>
        /// <returns></returns>
        public override bool Construct()
        {
            Name = "LOGS";
            RestartOnModuleCrash = true;
            Version = "2.6.0";
            return true;
        }

        private bool WriteLog(string message, Channel channel, System.DateTime _datetime)
        {
            try
            {
                string path = GetConfig(channel, "Logs.Path", channel.Name + Path.DirectorySeparatorChar);
                if (!Directory.Exists(TextPath + path))
                {
                    Log("Creating a folder for channel " + channel.Name);
                    Directory.CreateDirectory(TextPath + path);
                }
                System.IO.File.AppendAllText(TextPath + path + _datetime.Year + TDToString(_datetime.Month) + TDToString(_datetime.Day) + ".txt", message);
                return true;
            }
            catch (Exception er)
            {
                // nothing
                Log("Unable to write to log files, delaying write!", true);
                Console.WriteLine(er.Message);
            }
            return false;
        }

        /// <summary>
        /// Log file
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="channel">Channel</param>
        /// <param name="user">User</param>
        /// <param name="host">Host</param>
        /// <param name="noac">Action (if true it's logged as message, if false it's action)</param>
        public void ChanLog(string message, Channel channel, string user, string host, bool noac = true)
        {
            try
            {
                DateTime time = DateTime.Now;
                if (Module.GetConfig(channel, "Logging.Enabled", false))
                {

                    string log;
                    //string URL = Core.Host.Host2Name(host);
                    //updateBold(ref messagehtml);
                    if (!noac)
                    {
                        log = "[" + TDToString(time.Hour) + ":" +
                            TDToString(time.Minute) + ":" +
                            TDToString(time.Second) + "] * " +
                            user + " " + message + "\n";
                    }
                    else
                    {
                        log = "[" + TDToString(time.Hour) + ":"
                            + TDToString(time.Minute) + ":" +
                            TDToString(time.Second) + "] " + "<" +
                            user + ">\t " + message + "\n";
                    }
                    Job line = new Job();
                    line.channel = channel;
                    line.time = time;
                    line.message = log;
                    lock (jobs)
                    {
                        jobs.Add(line);
                    }
                    if (Core.DatabaseServerIsAvailable)
                    {
                        Item item = new Item();
                        item.channel = channel;
                        item.time = time;
                        item.username = user;
                        item.act = !noac;
                        item.host = host;
                        item.type = 0;
                        item.message = message;
                        lock (DJ)
                        {
                            DJ.Add(item);
                        }
                    }
                }
            }
            catch (Exception er)
            {
                // nothing
                HandleException(er);
            }
        }

        public override bool Hook_OnUnload()
        {
            try
            {
                int wait = 0;
                Unloading = true;
                Log("Unloading log system, terminating the writer thread...");
                if (this.thread.ThreadState == ThreadState.Running || this.thread.ThreadState == ThreadState.WaitSleepJoin)
                {
                    while (Unloading)
                    {
                        if (wait > 1000)
                        {
                            Log("Writer thread didn't finish within grace time");
                            return false;
                        }
                        wait++;
                        Thread.Sleep(10);
                    }
                    Log("Writer thread was unloaded OK");
                    return true;
                }
                Log("Writer thread is " + this.thread.ThreadState.ToString() + " - doing nothing");
                return true;
            }
            catch (Exception fail)
            {
                HandleException(fail);
                return false;
            }
        }

        public override void Hook_OnSelf(Channel channel, User self, string message)
        {
            ChanLog(message, channel, channel.PrimaryInstance.Nick, "");
        }

        /// <summary>
        /// Convert the number to format we want to have in log
        /// </summary>
        private string TDToString(int number)
        {
            if (number <= 9 && number >= 0)
            {
                return "0" + number.ToString();
            }
            return number.ToString();
        }
    }
}
