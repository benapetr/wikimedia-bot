//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// This module allows to run a 3rd scripts by a bot so that you can extend it with
// scripts written in different languages

using System;
using System.Collections.Generic;
using System.IO;
using System.Deployment;
using System.Threading;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Text;

namespace wmib.Extensions
{
    public class ScriptingEngine : Module
    {
        class Task
        {
            public Channel channel = null;
            public libirc.UserInfo user = null;
            public string parameters = "";
            public Script task;
        }

        /// <summary>
        /// Script that is run by a bot
        /// </summary>
        [Serializable]
        public class Script
        {
            public string Path;
            public string Command;
            public string Parameters;
            public bool OneLine = true;
            public bool RequireParameters = false;
            public bool Escape = true;
            public bool AcceptInput = false;
            public string Help = null;
            public bool SingleParameter = false;
            public string Permission = "trust";

            public void Exec(CommandParams pm)
            {
                lock (Tasks)
                {
                    Task tx = new Task();
                    if (pm.SourceUser != null)
                        tx.user = pm.SourceUser;
                    else
                        tx.channel = pm.SourceChannel;
                    tx.task = this;
                    tx.parameters = this.Parameters;
                    if (this.RequireParameters && string.IsNullOrEmpty(pm.Parameters))
                    {
                        if (pm.SourceUser != null)
                            IRC.DeliverMessage("You need to provide some parameters", pm.SourceUser);
                        else
                            IRC.DeliverMessage("You need to provide some parameters", pm.SourceChannel);
                        return;
                    }
                    string parameters = pm.Parameters;
                    if (this.SingleParameter && parameters != null)
                        parameters = "\"" + parameters.Replace("'", "\\'").Replace("\"", "\\\"") + "\"";
                    else if (this.Escape && parameters != null)
                        parameters = parameters.Replace("'", "\\'").Replace("\"", "\\\"");
                    if (this.AcceptInput && parameters != null)
                        tx.parameters += " " + parameters;
                    Tasks.Add(tx);
                }
            }
        }

        private static List<Task> Tasks = new List<Task>();
        private static List<Script> files = new List<Script>();

        public override bool Construct()
        {
            this.Version = new Version(1, 0, 0, 0);
            if (!File.Exists(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "scripts.xml"))
            {
                Log("No scripts definition file found", true);
                return false;
            }
            return true;
        }

        public override bool Hook_OnRegister()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Script>));
            StreamReader reader = new StreamReader(Variables.ConfigurationDirectory + Path.DirectorySeparatorChar + "scripts.xml");
            List<Script> files = (List<Script>)serializer.Deserialize(reader);
            reader.Close();
            foreach (Script script in files)
            {
                string help = script.Help;
                if (help == null)
                    help = "Run " + script.Path;
                Core.Help.Register(script.Command, help);
                RegisterCommand(new GenericCommand(script.Command, script.Exec, true, script.Permission));
            }
            return base.Hook_OnRegister();
        }

        public override bool Hook_OnUnload()
        {
            foreach (Script sc in files)
            {
                Core.Help.Unregister(sc.Command);
                UnregisterCommand(sc.Command);
            }
            return base.Hook_OnUnload();
        }

        public override void Load()
        {
            try
            {
                while (Core.IsRunning && this.IsWorking)
                {
                    List<Task> tasks = new List<Task>();
                    lock (Tasks)
                    {
                        tasks.AddRange(Tasks);
                        Tasks.Clear();
                    }
                    foreach (Task ts in tasks)
                    {
                        try
                        {
                            Process proc = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = ts.task.Path,
                                    Arguments = ts.parameters,
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                }
                            };

                            proc.Start();
                            string output = "";
                            bool write_file = false;
                            while (!proc.StandardOutput.EndOfStream || !proc.StandardError.EndOfStream)
                            {
                                string line;
                                if (!proc.StandardOutput.EndOfStream)
                                    line = proc.StandardOutput.ReadLine();
                                else
                                    line = proc.StandardError.ReadLine();
                                if (write_file || (ts.task.OneLine && !string.IsNullOrEmpty(output)))
                                {
                                    output += line + "\n";
                                    write_file = true;
                                    continue;
                                }
                                output += line + "\n";
                                if (ts.channel == null)
                                {
                                    // send back to channel
                                    IRC.DeliverMessage(line, ts.user);
                                }
                                else
                                {
                                    // to user
                                    IRC.DeliverMessage(line, ts.channel);
                                }
                            }
                            proc.WaitForExit();
                            proc.Close();
                            proc.Dispose();
                            if (write_file)
                            {
                                string filename = Path.GetTempFileName();
                                if (filename.EndsWith(".tmp"))
                                {
                                    filename = filename.Substring(0, filename.Length - 3);
                                    filename += "txt";
                                }
                                File.WriteAllText(filename, output);
#if __MonoCS__
                                Mono.Unix.Native.Syscall.chmod(filename, Mono.Unix.Native.FilePermissions.S_IROTH);
#endif
                                if (ts.channel == null)
                                {
                                    // send back to channel
                                    IRC.DeliverMessage("The command produced multiline output, see " + Configuration.WebPages.WebpageURL + filename, ts.user);
                                }
                                else
                                {
                                    // to user
                                    IRC.DeliverMessage("The command produced multiline output, see " + Configuration.WebPages.WebpageURL + filename, ts.channel);
                                }
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            return;
                        }
                        catch (Exception ef)
                        {
                            HandleException(ef);
                        }
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                return;
            }
        }
    }
}
