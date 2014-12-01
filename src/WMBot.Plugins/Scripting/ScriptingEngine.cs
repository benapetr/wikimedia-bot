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

        [Serializable]
        public class Script
        {
            public string Path;
            public string Command;
            public string Parameters;
            public bool OneLine = true;
            public bool RequireParameters = false;
            public bool AcceptInput = false;
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
                    if (this.AcceptInput)
                        tx.parameters += " " + pm.Parameters;
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
                RegisterCommand(new GenericCommand(script.Command, script.Exec, true, script.Permission));
            }
            return base.Hook_OnRegister();
        }

        public override bool Hook_OnUnload()
        {
            foreach (Script sc in files)
            {
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
                                output += line + "\n";
                                if (write_file || (ts.task.OneLine && !string.IsNullOrEmpty(output)))
                                {
                                    write_file = true;
                                    continue;
                                }
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
                                File.WriteAllText(filename, output);
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
