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

namespace wmib
{
    /// <summary>
    /// Module
    /// </summary>
    [Serializable]
    public abstract class Module
    {
        /// <summary>
        /// Name of module
        /// </summary>
        public string Name
        {
            get
            {
                if (name == null)
                {
                    name = this.GetType().Name;
                }
                return name;
            }
        }
        public virtual string Description
        {
            get
            {
                return "No description";
            }
        }
        /// <summary>
        /// This is just a cache for current module name that is used to prevent calls to expensive functions
        /// </summary>
        private string name = null;
        /// <summary>
        /// Version
        /// </summary>
        public Version Version = new Version(1, 0, 0);
        /// <summary>
        /// Time when it was loaded to system
        /// </summary>
        public DateTime Date = DateTime.Now;
        /// <summary>
        /// Whether it should be reloaded on crash
        /// </summary>
        public bool RestartOnModuleCrash = false;
        /// <summary>
        /// If the module is in warning mode
        /// </summary>
        public bool Warning = false;
        /// <summary>
        /// Thread associated to this module
        /// </summary>
        [NonSerialized]
        public Thread thread;
        /// <summary>
        /// Whether it is working
        /// </summary>
        public bool IsWorking = false;
        /// <summary>
        /// If this module contains own thread
        /// </summary>
        public bool HasSeparateThreadInstance = true;

        /// <summary>
        /// Creates a new instance of module
        /// </summary>
        protected Module()
        {
            thread = null;
        }

        /// <summary>
        /// Called when module is unloaded from memory
        /// </summary>
        ~Module()
        {
            Exit();
            lock (ExtensionHandler.Extensions)
            {
                if (ExtensionHandler.Extensions.Contains(this))
                {
                    ExtensionHandler.Extensions.Remove(this);
                }
            }
            Syslog.Log("Module was unloaded: " + this.Name);
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
                IsWorking = true;
                Hook_OnRegister();
                if (HasSeparateThreadInstance)
                {
                    thread = new Thread(Exec) { Name = "Module:" + Name };
                    Core.ThreadManager.RegisterThread(thread);
                    thread.Start();
                }
            }
            catch (Exception f)
            {
                Core.HandleException(f);
            }
        }

        public virtual void RegisterPermissions() { }
        public virtual void UnregisterPermissions() { }

        /// <summary>
        /// This is a private hook of html extension, each module can return a string that is a part of status page for each channel
        /// the content of the page is unsorted so this string will be randomly on end of html source
        /// </summary>
        /// <param name="html"></param>
        /// <param name="channel"></param>
        public virtual void Hook_AfterChannelWeb(ref string html, Channel channel)
        {
        }

        /// <summary>
        /// Someone is kicked
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="source"></param>
        /// <param name="user"></param>
        public virtual void Hook_Kick(Channel channel, User source, User user)
        {
        }

        /// <summary>
        /// This is a private hook of html extension, each module can return a string that is a part of status page for each channel
        /// the content of the page is unsorted so this string will be randomly inside of html source
        /// </summary>
        /// <param name="html"></param>
        /// <param name="channel"></param>
        public virtual void Hook_ChannelWeb(ref string html, Channel channel)
        {
        }

        /// <summary>
        /// Someone join
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        public virtual void Hook_Join(Channel channel, User user)
        {
        }

        /// <summary>
        /// This hook is called when someone send a private message to channel
        /// </summary>
        /// <param name="channel">channel</param>
        /// <param name="invoker">invoker</param>
        /// <param name="message">message</param>
        public virtual void Hook_PRIV(Channel channel, User invoker, string message)
        {
        }

        /// <summary>
        /// When someone is using action
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="invoker"></param>
        /// <param name="message"></param>
        public virtual void Hook_ACTN(Channel channel, User invoker, string message)
        {
        }

        /// <summary>
        /// Return a value
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="invoker"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public virtual bool Hook_GetConfig(Channel chan, User invoker, string config)
        {
            return false;
        }

        /// <summary>
        /// When a configuration of channel is being changed
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="invoker"></param>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool Hook_SetConfig(Channel chan, User invoker, string config, string value)
        {
            return false;
        }

        /// <summary>
        /// When a config of channel is being reload
        /// </summary>
        /// <param name="chan"></param>
        public virtual void Hook_ReloadConfig(Channel chan)
        {
        }

        /// <summary>
        /// When a channel is removed from operating memory
        /// </summary>
        /// <param name="chan"></param>
        public virtual void Hook_ChannelDrop(Channel chan)
        {
        }

        /// <summary>
        /// This hook is executed on user quit for each channel the user was in
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        /// <param name="mesg"></param>
        public virtual void Hook_ChannelQuit(Channel channel, User user, string mesg)
        {
        }

        /// <summary>
        /// Event that happen when user part
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        public virtual void Hook_Part(Channel channel, User user)
        {
        }

        /// <summary>
        /// Event that happens when the bot talk
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="self"></param>
        /// <param name="message"></param>
        public virtual void Hook_OnSelf(Channel channel, User self, string message)
        {
        }

        /// <summary>
        /// This is a private hook of html extension, each module can have a block of text in system page
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public virtual string Extension_DumpHtml(Channel channel)
        {
            return null;
        }

        /// <summary>
        /// This hook is called when channel is constructed
        /// </summary>
        /// <param name="channel"></param>
        public virtual void Hook_Channel(Channel channel)
        {
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

        public virtual uint Hook_GetWriterSize()
        {
            return 0;
        }

        /// <summary>
        /// User quit
        /// </summary>
        /// <param name="user"></param>
        /// <param name="Message"></param>
        public virtual void Hook_Quit(User user, string Message)
        {
        }

        /// <summary>
        /// This hook is called before the system information is being written to html
        /// </summary>
        /// <param name="html">Container of html code</param>
        public virtual void Hook_BeforeSysWeb(ref string html)
        {
        }

        /// <summary>
        /// This hook is called before the container is filled with footer
        /// </summary>
        /// <param name="html"></param>
        public virtual void Hook_AfterSysWeb(ref string html)
        {
        }

        /// <summary>
        /// Someone changes the nick
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="Target"></param>
        /// <param name="OldNick"></param>
        public virtual void Hook_Nick(Channel channel, User Target, string OldNick)
        {
        }

        /// <summary>
        /// Get a config
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="name"></param>
        /// <param name="invalid"></param>
        /// <returns></returns>
        public static int GetConfig(Channel chan, string name, int invalid)
        {
            try
            {
                if (chan != null)
                {
                    string value = chan.Extension_GetConfig(name);
                    int result;
                    if (int.TryParse(value, out result))
                    {
                        return result;
                    }
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            return invalid;
        }

        /// <summary>
        /// Debug log
        /// </summary>
        /// <param name="text"></param>
        /// <param name="verbosity"></param>
        public void DebugLog(string text, int verbosity = 1)
        {
            Syslog.DebugLog(text, verbosity);
        }

        /// <summary>
        /// System log
        /// </summary>
        /// <param name="text"></param>
        /// <param name="warning"></param>
        public void Log(string text, bool warning = false)
        {
            Syslog.Log(Name + ": " + text, warning);
        }

        /// <summary>
        /// Get config
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="name"></param>
        /// <param name="invalid"></param>
        /// <returns></returns>
        public static string GetConfig(Channel chan, string name, string invalid)
        {
            try
            {
                if (chan != null)
                {
                    string result = chan.Extension_GetConfig(name);
                    if (result == null)
                    {
                        return invalid;
                    }
                    return result;
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            return invalid;
        }

        /// <summary>
        /// Set config
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public static void SetConfig(Channel chan, string name, bool data)
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
                Core.HandleException(fail);
            }
        }

        /// <summary>
        /// Exception handler
        /// </summary>
        /// <param name="ex">Exception pointer</param>
        /// <param name="chan">Channel name</param>
        public void HandleException(Exception ex, string chan = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(Configuration.System.DebugChan))
                {
                    Core.irc.Queue.DeliverMessage("DEBUG Exception in plugin " + Name + ": " + ex.Message +
                                                       " last input was " + Core.LastText,
                                                       Configuration.System.DebugChan);
                }
                Syslog.Log("DEBUG Exception in module " + Name + ": " + ex.Message + ex.Source + ex.StackTrace, true);
            }
            catch (Exception) // exception happened while we tried to handle another one, ignore that (probably issue with logging)
            { }
        }

        /// <summary>
        /// Set config
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public static void SetConfig(Channel chan, string name, string data)
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
                Core.HandleException(fail);
            }
        }

        /// <summary>
        /// Get a bool from config of channel
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="name"></param>
        /// <param name="invalid"></param>
        /// <returns></returns>
        public static bool GetConfig(Channel chan, string name, bool invalid)
        {
            try
            {
                if (chan != null)
                {
                    string value = chan.Extension_GetConfig(name);
                    bool result;
                    if (bool.TryParse(value, out result))
                    {
                        return result;
                    }
                }
              return invalid;
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
                return invalid;
            }
        }

        private void Exec()
        {
            try
            {
                Load();
                Syslog.Log("Module terminated: " + Name);
                IsWorking = false;
                Core.ThreadManager.UnregisterThread(thread);
            }
            catch (ThreadAbortException)
            {
                Syslog.Log("Module terminated: " + Name);
                Core.ThreadManager.UnregisterThread(thread);
                return;
            }
            catch (Exception f)
            {
                Core.HandleException(f);
                IsWorking = false;
                Syslog.Log("Module crashed: " + Name, true);
            }
            while (Core.IsRunning && RestartOnModuleCrash)
            {
                try
                {
                    Warning = true;
                    IsWorking = true;
                    Syslog.Log("Restarting the module: " + Name, true);
                    Load();
                    Syslog.Log("Module terminated: " + Name);
                    IsWorking = false;
                }
                catch (ThreadAbortException)
                {
                    Syslog.Log("Module terminated: " + Name);
                    Core.ThreadManager.UnregisterThread(thread);
                    return;
                }
                catch (Exception f)
                {
                    Core.HandleException(f);
                    IsWorking = false;
                    Syslog.Log("Module crashed: " + Name, true);
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

        /// <summary>
        /// Start
        /// </summary>
        public virtual void Load()
        { 
            Syslog.Log("Module " + Name + " is missing core thread, terminated", true);
            RestartOnModuleCrash = false;
            IsWorking = false;
        }

        /// <summary>
        /// Disable module
        /// </summary>
        public void Exit()
        {
            Syslog.Log("Unloading module: " + Name);
            try
            {
                if (!Hook_OnUnload())
                {
                    Syslog.Log("Unable to unload module, forcefully removed from memory: " + Name, true);
                }
                IsWorking = false;
                RestartOnModuleCrash = false;
                if (thread != null)
                {
                    Syslog.Log("Terminating module: " + Name, true);
                    if (RestartOnModuleCrash)
                    {
                        RestartOnModuleCrash = false;
                    }
                    Core.ThreadManager.KillThread(thread);
                }
                lock (ExtensionHandler.Extensions)
                {
                    if (ExtensionHandler.Extensions.Contains(this))
                    {
                        ExtensionHandler.Extensions.Remove(this);
                    }
                }
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
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
                lock (ExtensionHandler.Extensions)
                {
                    foreach (Module x in ExtensionHandler.Extensions)
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
                Core.HandleException(f);
            }
            return false;
        }
    }
}
