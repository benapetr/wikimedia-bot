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
    /// <summary>
    /// Module
    /// </summary>
    [Serializable()]
    public abstract class Module : MarshalByRefObject
    {
        /// <summary>
        /// List of all modules loaded in kernel
        /// </summary>
        public static List<Module> module = new List<Module>();
        /// <summary>
        /// Name of module
        /// </summary>
        public string Name = "";
        /// <summary>
        /// Version
        /// </summary>
        public string Version = "unknown";
        /// <summary>
        /// Time when it was loaded to system
        /// </summary>
        public DateTime Date = DateTime.Now;
        /// <summary>
        /// Whether it should be reloaded on crash
        /// </summary>
        public bool Reload = false;
        /// <summary>
        /// If the module is in warning mode
        /// </summary>
        public bool Warning = false;
        /// <summary>
        /// Thread associated to this module
        /// </summary>
        [NonSerialized()]
        public System.Threading.Thread thread;
        /// <summary>
        /// Whether it is working
        /// </summary>
        public bool working = false;
        /// <summary>
        /// Parent domain of this module
        /// </summary>
        public AppDomain ParentDomain = null;
        /// <summary>
        /// Whether it has started or not
        /// </summary>
        public bool start = false;

        /// <summary>
        /// Creates a new instance of module
        /// </summary>
        public Module()
        {
            thread = null;
        }

        /// <summary>
        /// Called when module is unloaded from memory
        /// </summary>
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
            if (core.Domains.ContainsKey(this) && core.Domains[this] != ParentDomain)
            {
                lock (core.Domains)
                {
                    //AppDomain.Unload(core.Domains[this]);
                    core.Domains.Remove(this);
                }
            }
            core.Log("Module was unloaded: " + this.Name);
        }

        /// <summary>
        /// This function is called during load of module
        /// </summary>
        /// <returns></returns>
        public virtual bool Construct()
        {
            return false;
        }

        /// <summary>
        /// Create a thread and load the module
        /// </summary>
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

        /// <summary>
        /// This is a private hook of html extension, each module can return a string that is a part of status page for each channel
        /// the content of the page is unsorted so this string will be randomly on end of html source
        /// </summary>
        /// <param name="html"></param>
        /// <param name="channel"></param>
        public virtual void Hook_AfterChannelWeb(ref string html, config.channel channel)
        {
            return;
        }

        /// <summary>
        /// Someone is kicked
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="source"></param>
        /// <param name="user"></param>
        public virtual void Hook_Kick(config.channel channel, User source, User user)
        {
            return;
        }

        /// <summary>
        /// This is a private hook of html extension, each module can return a string that is a part of status page for each channel
        /// the content of the page is unsorted so this string will be randomly inside of html source
        /// </summary>
        /// <param name="html"></param>
        /// <param name="channel"></param>
        public virtual void Hook_ChannelWeb(ref string html, config.channel channel)
        {
            return;
        }

        /// <summary>
        /// Someone join
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
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

        /// <summary>
        /// When someone is using action
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="invoker"></param>
        /// <param name="message"></param>
        public virtual void Hook_ACTN(config.channel channel, User invoker, string message)
        {
            return;
        }

        /// <summary>
        /// When a configuration of channel is being changed
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="invoker"></param>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool Hook_SetConfig(config.channel chan, User invoker, string config, string value)
        {
            return false;
        }

        /// <summary>
        /// When a config of channel is being reload
        /// </summary>
        /// <param name="chan"></param>
        public virtual void Hook_ReloadConfig(config.channel chan)
        {
            return;
        }

        /// <summary>
        /// When a channel is removed from operating memory
        /// </summary>
        /// <param name="chan"></param>
        public virtual void Hook_ChannelDrop(config.channel chan)
        {
            return;
        }

        /// <summary>
        /// Event that happen when user part
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        public virtual void Hook_Part(config.channel channel, User user)
        {
            return;
        }

        /// <summary>
        /// Event that happens when the bot talk
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="self"></param>
        /// <param name="message"></param>
        public virtual void Hook_OnSelf(config.channel channel, User self, string message)
        {
            return;
        }

        /// <summary>
        /// This is a private hook of html extension, each module can have a block of text in system page
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
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

        /// <summary>
        /// When the module is loaded
        /// </summary>
        /// <returns></returns>
        public virtual bool Hook_OnUnload()
        {
            return true;
        }

        /// <summary>
        /// When the module is being registered in memory
        /// </summary>
        /// <returns></returns>
        public virtual bool Hook_OnRegister()
        {
            return true;
        }

        /// <summary>
        /// User quit
        /// </summary>
        /// <param name="user"></param>
        /// <param name="Message"></param>
        public virtual void Hook_Quit(User user, string Message)
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

        /// <summary>
        /// Someone changes the nick
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="Target"></param>
        /// <param name="OldNick"></param>
        public virtual void Hook_Nick(config.channel channel, User Target, string OldNick)
        { 
            
        }

        /// <summary>
        /// Get a config
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="name"></param>
        /// <param name="invalid"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get config
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="name"></param>
        /// <param name="invalid"></param>
        /// <returns></returns>
        public static string GetConfig(config.channel chan, string name, string invalid)
        {
            try
            {
                string result = null;
                if (chan != null)
                {
                    result = chan.Extension_GetConfig(name);
                    if (result == null)
                    {
                        return invalid;
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

        private void Exec()
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

        /// <summary>
        /// This is called only when a bot receive a private message, if it return true the message is considered handled
        /// </summary>
        /// <param name="message"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual bool Hook_OnPrivateFromUser(string message, User user)
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

        /// <summary>
        /// Disable module
        /// </summary>
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

        /// <summary>
        /// Return true if this module is already loaded
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
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
