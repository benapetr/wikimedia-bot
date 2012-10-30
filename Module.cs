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
using System.Text;

namespace wmib
{
    public class Module
    {
        public static List<Module> module = new List<Module>();
        public string Name = "";
        public bool Reload = false;
        public bool Warning = false;
        public System.Threading.Thread thread;
        public bool working = false;

        public void Create(string name, bool start = false, bool restart = false)
        {
            if (Exist(name))
            {
                throw new Exception("This module is already registered");
            }
            lock (module)
            {
                Program.Log("Loading module: " + name);
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
            Program.Log("Unloading module: " + Name);
            try
            {
                if (thread.ThreadState == System.Threading.ThreadState.Running)
                {
                    Program.Log("Terminating module: " + Name, true);
                    if (Reload)
                    {
                        Reload = false;
                    }
                    thread.Abort();
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

        }

        public virtual void Hook_ChannelWeb(ref string html, config.channel channel)
        {

        }

        public virtual void Hook_BeforeSysWeb(ref string html)
        {

        }

        public virtual void Hook_AfterSysWeb(ref string html)
        { 
            
        }

        public void Exec()
        {
            try
            {
                Load();
                Program.Log("Module terminated: " + Name, true);
                working = false;
                while (Reload)
                {
                    Warning = true;
                    working = true;
                    Program.Log("Restarting the module: " + Name, true);
                    Load();
                    Program.Log("Module terminated: " + Name, true);
                    working = false;
                }
            }
            catch (Exception f)
            {
                core.handleException(f);
                working = false;
            }
        }

        public virtual void Load()
        { 
            Program.Log("Module " + Name + " is missing core thread, terminated", true);
            working = false;
            return;
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
