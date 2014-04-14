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
using System.IO;

namespace wmib
{
    public partial class ExtensionHandler
    {
        /// <summary>
        /// List of all modules loaded in kernel
        /// </summary>
        public static List<Module> Extensions = new List<Module>();

        private static readonly List<Type> _moduleTypes = new List<Type>(); 

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
                    ExtensionHandler.Extensions.Add(module);
                }
                module.Init();
            }
            catch (Exception fail)
            {
                module.IsWorking = false;
                Syslog.Log("Unable to create instance of " + module.Name);
                Core.HandleException(fail);
            }
        }

        /// <summary>
        /// Load a binary module
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool LoadAllModulesInLibrary(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    System.Reflection.Assembly library = System.Reflection.Assembly.LoadFrom(path);
                    
                    if (library == null)
                    {
                        Syslog.WarningLog("Unable to load " + path + " because the file can't be read");
                        return false;
                    }
                    
                    Type[] types = library.GetTypes();

                    foreach (Type type in types)
                    {
                        if (type.IsSubclassOf(typeof(Module)))
                        {
                            // For recall later
                            _moduleTypes.Add(type);

                            if (ShouldCreateModuleOnStartup(type))
                            {
                                CreateModule(type);
                            } else
                            {
                                Syslog.DebugLog("Not registering module (type " + type.Name + ") because it's not in a module list");
                            }
                        }
                    }

                    return true;
                }

                Syslog.Log("Unable to load " + path + " because the file can't be read", true);
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            return false;
        }
        
        public static bool DumpAllModulesInLibrary(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    System.Reflection.Assembly library = System.Reflection.Assembly.LoadFrom(path);
                    if (library == null)
                    {
                        Syslog.WarningLog("Unable to load " + path + " because the file can't be read");
                        return false;
                    }
                    Type[] types = library.GetTypes();
                    string list = "";
                    foreach (Type type in types)
                    {
                        if (type.IsSubclassOf(typeof(Module)))
                        {
                            list += type.Name + ",";
                        }
                    }
                    list = list.TrimEnd(',');
                    if (list == "")
                    {
                        list = "No modules found in this file";
                    }
                    Console.WriteLine("In " + path + ": " + list);
                    return true;
                }

                Syslog.Log("Unable to load " + path + " because the file can't be read", true);
            }
            catch (Exception fail)
            {
                Core.HandleException(fail);
            }
            return false;
        }

        private static bool ShouldCreateModuleOnStartup(Type type)
        {
            var loadArray = Configuration.System.ModulesToLoadArray;

            // grrr.... .net 2.0 doesnt have LINQ
            return Array.Exists(loadArray, s => s.Equals(type.Name, StringComparison.InvariantCultureIgnoreCase));
        }

        private static void CreateModule(Type moduleType)
        {
            var module = (Module) Activator.CreateInstance(moduleType);

            if (!module.Construct())
            {
                Syslog.Log("Invalid module", true);
                module.Exit();
                return;
            }

            InitialiseMod(module);
        }

        /// <summary>
        /// Search and load the modules in modules folder
        /// </summary>
        public static void SearchMods()
        {
            if (!Directory.Exists(Configuration.Paths.ModulesPath))
            {
                Syslog.Log("There is no modules folder");
                return;
            }
            foreach (string dll in Directory.GetFiles(Configuration.Paths.ModulesPath, "*.dll"))
            {
                LoadAllModulesInLibrary(dll);
            }
            Syslog.Log("Modules loaded");
        }
        
        public static void DumpMods()
        {
            if (!Directory.Exists(Configuration.Paths.ModulesPath))
            {
                Syslog.Log("There is no modules folder");
                return;
            }
            foreach (string dll in Directory.GetFiles(Configuration.Paths.ModulesPath, "*.dll"))
            {
                DumpAllModulesInLibrary(dll);
            }
        }

        /// <summary>
        /// Retrieve a module from name
        /// </summary>
        /// <param name="name">Name of module</param>
        /// <returns></returns>
        public static Module RetrieveModule(string name)
        {
            lock (ExtensionHandler.Extensions)
            {
                foreach (Module module in ExtensionHandler.Extensions)
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
