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
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public class Module
    {
        public static List<Module> module = new List<Module>();
        public string Name = "";
        public string Version = "unknown";
        public DateTime Date = DateTime.Now;
        public bool Reload = false;
        public bool Warning = false;
        public System.Threading.Thread thread;
        public bool working = false;

        public void Create(string name, bool start = false, bool restart = false)
        {
            Date = DateTime.Now;
            if (Exist(name))
            {
                throw new Exception("This module is already registered");
            }
            lock (module)
            {
                core.Log("Loading module: " + name);
                Name = name;
                Reload = restart;
                module.Add(this);
            }
            if (start)
            {
                Init();
            }
        }

        public Module()
        {
            if (Construct())
            {
                return;
            }
            throw new Exception("Invalid module");
        }

        ~Module()
        {
            Exit();
            lock (module)
            {
                if (module.Contains(this))
                {
                    module.Remove(this);
                }
            }
            core.Log("Module was unloaded: " + this.Name);
        }

        public virtual bool Construct()
        {
            return false;
        }

        public Module(string name, bool start = false)
        {
            Create(name, start);
        }

        public void Init()
        {
            try
            {
                thread = new System.Threading.Thread(Exec);
                thread.Name = "Module " + Name;
                working = true;
                thread.Start();
            }
            catch (Exception f)
            {
                core.handleException(f);
            }
        }

        public virtual void Hook_AfterChannelWeb(ref string html, config.channel channel)
        {
            return;
        }

        public virtual void Hook_Kick(config.channel channel, User source, User user)
        {
            return;
        }

        public virtual void Hook_ChannelWeb(ref string html, config.channel channel)
        {
            return;
        }

        public virtual void Hook_Join(config.channel channel, User user)
        {
            return;
        }

        /// <summary>
        /// This hook is called when someone send a private message to channel
        /// </summary>
        /// <param name="channel">channel</param>
        /// <param name="invoker">invoker</param>
        /// <param name="message">message</param>
        public virtual void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            return;
        }

        public virtual void Hook_ACTN(config.channel channel, User invoker, string message)
        {
            return;
        }

        public virtual void Hook_Part(config.channel channel, User user)
        {
            return;
        }

        public virtual void Hook_Quit(User user)
        {
            return;
        }

        /// <summary>
        /// This hook is called before the system information is being written to html
        /// </summary>
        /// <param name="html">Container of html code</param>
        public virtual void Hook_BeforeSysWeb(ref string html)
        {
            return;
        }

        /// <summary>
        /// This hook is called before the container is filled with footer
        /// </summary>
        /// <param name="html"></param>
        public virtual void Hook_AfterSysWeb(ref string html)
        {
            return;
        }

        public void Exec()
        {
            try
            {
                Load();
                core.Log("Module terminated: " + Name, true);
                working = false;
            }
            catch (ThreadAbortException)
            {
                core.Log("Module terminated: " + Name, true);
                return;
            }
            catch (Exception f)
            {
                core.handleException(f);
                working = false;
                core.Log("Module crashed: " + Name, true);
            }
            while (Reload)
            {
                try
                {
                    Warning = true;
                    working = true;
                    core.Log("Restarting the module: " + Name, true);
                    Load();
                    core.Log("Module terminated: " + Name, true);
                    working = false;
                }
                catch (ThreadAbortException)
                {
                    core.Log("Module terminated: " + Name, true);
                    return;
                }
                catch (Exception f)
                {
                    core.handleException(f);
                    working = false;
                    core.Log("Module crashed: " + Name, true);
                }
            }
        }

        public virtual void Load()
        { 
            core.Log("Module " + Name + " is missing core thread, terminated", true);
            Reload = false;
            working = false;
            return;
        }

        public void Exit()
        {
            core.Log("Unloading module: " + Name);
            try
            {
                working = false;
                Reload = false;
                if (thread != null)
                {
                    if (thread.ThreadState == System.Threading.ThreadState.Running)
                    {
                        core.Log("Terminating module: " + Name, true);
                        if (Reload)
                        {
                            Reload = false;
                        }
                        thread.Abort();
                    }
                }
                lock (module)
                {
                    if (module.Contains(this))
                    {
                        module.Remove(this);
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }

        public static bool Exist(string Name)
        {
            try
            {
                lock (module)
                {
                    foreach (Module x in module)
                    {
                        if (x.Name == Name)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception f)
            {
                core.handleException(f);
            }
            return false;
        }
    }
}
