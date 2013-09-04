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
        public struct Job
        {
            public DateTime time;
            public string message;
            public config.channel ch;
        }

        public class Item
        {
            public string username = null;
            public DateTime time;
            public string message = null;
            public bool act = false;
            public config.channel ch;
        }

        private List<char> Separator = new List<char> { ' ', ',', (char)3, '(', ')', '{', '}', (char)2, '<', '>' };

        private bool Unloading = false;

        private List<Job> jobs = new List<Job>();

        private List<Item> DJ = new List<Item>();

        public override void Hook_ACTN(config.channel channel, User invoker, string message)
        {
            ChanLog(message, channel, invoker.Nick, invoker.Host, false);
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            ChanLog(message, channel, invoker.Nick, invoker.Host);
            if (message == config.CommandPrefix + "logon")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (Module.GetConfig(channel, "Logging.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("ChannelLogged", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("LoggingOn", channel.Language), channel.Name);
                        Module.SetConfig(channel, "Logging.Enabled", true);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "logoff")
            {
                if (channel.Users.IsApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!Module.GetConfig(channel, "Logging.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("LogsE1", channel.Language), channel.Name);
                        return;
                    }
                    else
                    {
                        Module.SetConfig(channel, "Logging.Enabled", false);
                        channel.SaveConfig();
                        core.irc._SlowQueue.DeliverMessage(messages.get("NotLogged", channel.Language), channel.Name);
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
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
                    while (!WriteLog(curr.message, curr.ch, curr.time))
                    {
                        Thread.Sleep(2000);
                    }
                }
            }
            return 2;
        }

        private void Finish()
        {
            if (jobs.Count != 0)
            {
                core.Log("Logging was requested to stop, but there is still " + jobs.Count.ToString() + " lines, writing now");
            }
            WriteData();
            core.Log("There are no unsaved data, we can disable this module now");
            Unloading = false;
        }

        public void Writer()
        {
            try
            {
                if (!core.DatabaseServerIsAvailable)
                {
                    Log("No sql server is available, closing DB log writer");
                    return;
                }
                if (core.DB == null)
                {
                    Log("core.DB is null", true);
                }
                DebugLog("The SQL writer started");
                while (core._Status != core.Status.ShuttingDown)
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
                        lock (core.DB.DatabaseLock)
                        {
                            core.DB.Connect();
                            while (!core.DB.IsConnected)
                            {
                                if (core.DB.ErrorBuffer != null)
                                {
                                    Log("Unable to connect to SQL server: " + core.DB.ErrorBuffer + " retrying in 20 seconds");
                                }
                                Thread.Sleep(20000);
                                core.DB.Connect();
                            }
                            foreach (Item item in db)
                            {
                                Database.Row row = new Database.Row();
                                row.Values.Add(new Database.Row.Value(0));
                                row.Values.Add(new Database.Row.Value(item.ch.Name, Database.DataType.Varchar));
                                row.Values.Add(new Database.Row.Value(item.username, Database.DataType.Varchar));
                                row.Values.Add(new Database.Row.Value(item.time));
                                row.Values.Add(new Database.Row.Value(item.act));
                                row.Values.Add(new Database.Row.Value(item.message, Database.DataType.Varchar));
                                row.Values.Add(new Database.Row.Value(item.message, Database.DataType.Varchar));
                                core.DB.InsertRow("logs", row);
                            }
                            core.DB.Commit();
                            core.DB.Disconnect();
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                if (core.DatabaseServerIsAvailable)
                {
                    core.DB.Commit();
                    core.DB.Disconnect();
                }
                Log("SQL Writer is shut down with " + DJ.Count.ToString() + " unfinished lines");
            }
            catch (Exception fail)
            {
                handleException(fail);
                Log("SQL Writer is down!!", true);
            }
        }

        public override void Load()
        {
            Thread sql = new Thread(Writer);
            sql.Name = "sql writer";
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
                    return;
                }
                catch (Exception fail)
                {
                    handleException(fail);
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
            start = true;
            Reload = true;
            Version = "1.8.0";
            return true;
        }

        private bool WriteLog(string message, config.channel channel, System.DateTime _datetime)
        {
            try
            {
                string path = GetConfig(channel, "Logs.Path", "null");
                if (path == "null")
                {
                    SetConfig(channel, "Logs.Path", channel.LogDir);
                    path = channel.LogDir;
                }

                System.IO.File.AppendAllText(config.path_txt + path + _datetime.Year + TDToString(_datetime.Month) + TDToString(_datetime.Day) + ".txt", message);
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
        public void ChanLog(string message, config.channel channel, string user, string host, bool noac = true)
        {
            try
            {
                DateTime time = DateTime.Now;
                if (Module.GetConfig(channel, "Logging.Enabled", false))
                {

                    string log;
                    string URL = core.Host.Host2Name(host);
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
                    line.ch = channel;
                    line.time = time;
                    line.message = log;
                    lock (jobs)
                    {
                        jobs.Add(line);
                    }
                    if (core.DatabaseServerIsAvailable)
                    {
                        Item item = new Item();
                        item.ch = channel;
                        item.time = time;
                        item.username = user;
                        item.act = !noac;
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
                handleException(er);
            }
        }

        public override bool Hook_OnUnload()
        {
            try
            {
                int wait = 0;
                Unloading = true;
                Reload = false;
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
                handleException(fail);
                return false;
            }
        }

        public override void Hook_OnSelf(config.channel channel, User self, string message)
        {
            ChanLog(message, channel, config.username, "");
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
