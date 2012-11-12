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
    [Serializable()]
    public class Module
    {
        public static List<Module> module = new List<Module>();
        public string Name = "";
        public string Version = "unknown";
        public DateTime Date = DateTime.Now;
        public bool Reload = false;
        public bool Warning = false;
        [NonSerialized()]
        public System.Threading.Thread thread;
        public bool working = false;

        public void Create(string name, bool start = false, bool restart = false)
        {
            if (name == null || name == "")
            {
                core.Log("This module has invalid name and was terminated to prevent troubles", true);
                throw new Exception("Invalid name");
            }
            Date = DateTime.Now;
            if (Exist(name))
            {
                core.Log("This module is already registered " + name + " this new instance was terminated to prevent troubles", true);
                throw new Exception("This module is already registered");
            }
            try
            {
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
            catch (Exception fail)
            {
                working = false;
                core.handleException(fail);
            }
        }

        public Module()
        {
            if (Construct())
            {
                return;
            }
            core.Log("Invalid module", true);
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
                Hook_OnRegister();
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

        public virtual bool Hook_SetConfig(config.channel chan, User invoker, string config, string value)
        {
            return false;
        }

        public virtual void Hook_ReloadConfig(config.channel chan)
        {
            return;
        }

        public virtual void Hook_ChannelDrop(config.channel chan)
        {
            return;
        }

        public virtual void Hook_Part(config.channel channel, User user)
        {
            return;
        }

        public virtual void Hook_OnSelf(config.channel channel, User self, string message)
        {
            return;
        }

        public virtual string Extension_DumpHtml(config.channel channel)
        {
            return null;
        }

        /// <summary>
        /// This hook is called when channel is constructed
        /// </summary>
        /// <param name="channel"></param>
        public virtual void Hook_Channel(config.channel channel)
        {
            return;
        }

        public virtual bool Hook_OnUnload()
        {
            return true;
        }

        public virtual bool Hook_OnRegister()
        {
            return true;
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

        public static int GetConfig(config.channel chan, string name, int invalid)
        {
            try
            {
                if (chan != null)
                {
                    string value = chan.Extension_GetConfig(name);
                    int result = 0;
                    if (int.TryParse(value, out result))
                    {
                        return result;
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return invalid;
        }

        public static string GetConfig(config.channel chan, string name, string invalid)
        {
            try
            {
                string result = null;
                if (chan != null)
                {
                    string value = chan.Extension_GetConfig(name);
                    if (result == null)
                    {
                        result = invalid;
                    }
                        return result;
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return invalid;
        }

        public static void SetConfig(config.channel chan, string name, bool data)
        {
            try
            {
                if (chan != null)
                {
                    chan.Extension_SetConfig(name, data.ToString());
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }

        public static void SetConfig(config.channel chan, string name, string data)
        {
            try
            {
                if (chan != null)
                {
                    chan.Extension_SetConfig(name, data);
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
        }

        /// <summary>
        /// Get a bool from config of channel
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="name"></param>
        /// <param name="invalid"></param>
        /// <returns></returns>
        public static bool GetConfig(config.channel chan, string name, bool invalid)
        {
            try
            {
                if (chan != null)
                {
                    string value = chan.Extension_GetConfig(name);
                    bool result = false;
                    if (bool.TryParse(value, out result))
                    {
                        return result;
                    }
                }
              return invalid;
            }
            catch (Exception fail)
            {
                core.handleException(fail);
                return invalid;
            }
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

        public virtual bool Hook_OnPrivateFromUser(User user)
        {
            return false;
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
                if (!Hook_OnUnload())
                {
                    core.Log("Unable to unload module, forcefully removed from memory: " + Name, true);
                }
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
                lock (core.Domains)
                {
                    if (core.Domains.ContainsKey(this))
                    {
                        AppDomain.Unload(core.Domains[this]);
                        core.Domains.Remove(this);
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
