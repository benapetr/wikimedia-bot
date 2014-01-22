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
using System.IO;

namespace wmib
{
    public partial class core
    {
        /// <summary>
        /// Intialise module
        /// </summary>
        /// <param name="module"></param>
        public static void InitialiseMod(Module module)
        {
            if (string.IsNullOrEmpty(module.Name))
            {
                Syslog.Log("This module has invalid name and was terminated to prevent troubles", true);
                throw new Exception("Invalid name");
            }
            module.Date = DateTime.Now;
            if (Module.Exist(module.Name))
            {
                Syslog.Log("This module is already registered " + module.Name + " this new instance was terminated to prevent troubles", true);
                throw new Exception("This module is already registered");
            }
            try
            {
                lock (module)
                {
                    Syslog.Log("Loading module: " + module.Name + " v" + module.Version);
                    Module.module.Add(module);
                }
                if (module.start)
                {
                    module.Init();
                }
            }
            catch (Exception fail)
            {
                module.working = false;
                Syslog.Log("Unable to create instance of " + module.Name);
                core.handleException(fail);
            }
        }

        /// <summary>
        /// Load a binary module
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool LoadMod(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    System.Reflection.Assembly library = System.Reflection.Assembly.LoadFrom(path);

                    if (library == null)
                    {
                        Syslog.Log("Unable to load " + path + " because the file can't be read", true);
                        return false;
                    }
                    Type[] types = library.GetTypes();
                    Type type = library.GetType("wmib.RegularModule");
                    Type pluginInfo = null;
                    foreach (Type curr in types)
                    {
                        if (curr.IsAssignableFrom(type))
                        {
                            pluginInfo = curr;
                            break;
                        }
                    }

                    if (pluginInfo == null)
                    {
                        foreach (Type curr in types)
                        {
                            if (curr.BaseType == typeof(Module))
                            {
                                pluginInfo = curr;
                                break;
                            }
                        }
                    }

                    if (pluginInfo == null)
                    {
                        Syslog.Log("Unable to load " + path + " because the library contains no module", true);
                        return false;
                    }

                    Module _plugin = (Module)Activator.CreateInstance(pluginInfo);

                    _plugin.ParentDomain = core.domain;
                    if (!_plugin.Construct())
                    {
                        Syslog.Log("Invalid module", true);
                        _plugin.Exit();
                        return false;
                    }

                    InitialiseMod(_plugin);
                    return true;
                }
                Syslog.Log("Unable to load " + path + " because the file can't be read", true);
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return false;
        }

        /// <summary>
        /// Search and load the modules in modules folder
        /// </summary>
        public static void SearchMods()
        {
            if (Directory.Exists(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                + Path.DirectorySeparatorChar + "modules"))
            {
                foreach (string dll in Directory.GetFiles(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    + Path.DirectorySeparatorChar + "modules", "*.bin"))
                {
                    LoadMod(dll);
                }
            }
            Syslog.Log("Modules loaded");
        }

        /// <summary>
        /// Retrieve a module from name
        /// </summary>
        /// <param name="name">Name of module</param>
        /// <returns></returns>
        public static Module getModule(string name)
        {
            lock (Module.module)
            {
                foreach (Module module in Module.module)
                {
                    if (module.Name == name)
                    {
                        return module;
                    }
                }
            }
            return null;
        }
    }
}
