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

namespace wmib
{
    public class SystemHooks
    {
        public static void IrcReloadChannelConf(Channel Channel)
        {
            foreach (Module module in ExtensionHandler.ExtensionList)
            {
                try
                {
                    if (module.IsWorking)
                    {
                        module.Hook_ReloadConfig(Channel);
                    }
                }
                catch (Exception fail)
                {
                    Syslog.Log("MODULE: exception at Hook_Reload in " + module.Name);
                    Core.HandleException(fail, module.Name);
                }
            }
        }

        public static void IrcKick(Channel Channel, libirc.UserInfo Source, string Target)
        {
            foreach (Module module in ExtensionHandler.ExtensionList)
            {
                if (!module.IsWorking)
                {
                    continue;
                }
                try
                {
                    module.Hook_Kick(Channel, Source, Target);
                }
                catch (Exception fail)
                {
                    Syslog.Log("MODULE: exception at Hook_Kick in " + module.Name, true);
                    Core.HandleException(fail, module.Name);
                }
            }
        }

        public static void SystemLog(string Message, Syslog.Type MessageType)
        {
        }

        public static void OnSQL(string name, string sql)
        {
            
        }
    }
}

