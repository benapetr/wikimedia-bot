//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Copyright 2013 - 2014 Petr Bena (benapetr@gmail.com)

using System.Collections.Generic;

namespace wmib
{
    public class CommandPool
    {
        private static Dictionary<string, GenericCommand> commands = new Dictionary<string, GenericCommand>();
        private static Dictionary<string, string> aliases = new Dictionary<string, string>();
        public static Dictionary<string, GenericCommand> CommandsList
        {
            get
            {
                return new Dictionary<string, GenericCommand>(commands);
            }
        }

        public static Dictionary<string, string> AliasesList
        {
            get
            {
                return new Dictionary<string, string>(aliases);
            }
        }

        public static bool Exists(string command)
        {
            return commands.ContainsKey(command);
        }

        public static bool AliasExists(string name)
        {
            return aliases.ContainsKey(name);
        }

        public static GenericCommand GetCommand(string command)
        {
            lock (commands)
            {
                if (commands.ContainsKey(command))
                    return commands[command];
                else if (aliases.ContainsKey(command) && commands.ContainsKey(aliases[command]))
                    return commands[aliases[command]];
                else
                    return null;
            }
        }

        public static void RegisterAlias(string name, string target, string module = null)
        {
            lock (aliases)
            {
                if (aliases.ContainsKey(name))
                {
                    throw new WmibException("This alias is already registered: " + name);
                }
                if (module != null)
                    Syslog.DebugLog("Module " + module + " registered alias " + name + " for command " + target);
                else
                    Syslog.DebugLog("Registered alias " + name + " for command " + target);
                if (!commands.ContainsKey(target))
                    Syslog.WarningLog("Alias " + name + " points to command " + target + " which doesn't exist");
                aliases.Add(name, target);
            }
        }

        public static void RegisterCommand(GenericCommand command)
        {
            lock (commands)
            {
                if (commands.ContainsKey(command.Name))
                {
                    throw new WmibException("This command is already registered: " + command.Name);
                }
                if (command.Module != null)
                {
                    Syslog.DebugLog("Module " + command.Module + " registered a new command: " + command.Name);
                }
                else
                {
                    Syslog.DebugLog("Registering a new command: " + command.Name);
                }
                commands.Add(command.Name, command);
            }
        }

        public static void UnregisterCommand(GenericCommand command)
        {
            lock (commands)
            {
                if (!commands.ContainsKey(command.Name))
                    throw new WmibException("There is no such a command in pool: " + command.Name);

                commands.Remove(command.Name);
                Syslog.DebugLog("Unregistered command: " + command.Name);
            }
        }

        public static void UnregisterAlias(string name)
        {
            lock (aliases)
            {
                if (!aliases.ContainsKey(name))
                    throw new WmibException("There is no such an alias in pool: " + name);

                aliases.Remove(name);
                Syslog.DebugLog("Unregistered alias: " + name);
            }
        }

        public static void UnregisterCommand(string command_name)
        {
            lock (commands)
            {
                if (!commands.ContainsKey(command_name))
                    throw new WmibException("There is no such a command in pool: " + command_name);

                commands.Remove(command_name);
            }
        }
    }

    public class CommandParams
    {
        /// <summary>
        /// Full message sent by user including command prefix
        /// </summary>
        public string Message;
        /// <summary>
        /// Only a command string
        /// </summary>
        public string Command;
        /// <summary>
        /// All text that was after command
        /// </summary>
        public string Parameters;
        /// <summary>
        /// User who sent this message
        /// </summary>
        public libirc.UserInfo User;
        /// <summary>
        /// Channel in which this message was sent
        /// </summary>
        public Channel SourceChannel = null;
        /// <summary>
        /// If this is not null it was a private message sent by this user
        /// </summary>
        public libirc.UserInfo SourceUser = null;
    }
}
