using System;
using System.Collections.Generic;
using System.IO;
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
                            DebugLog("Running " + ts.task.Path, 1);
                            Process proc = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = ts.task.Path,
                                    Arguments = ts.parameters,
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                }
                            };

                            proc.Start();
                            DebugLog("Reading output for " + ts.task.Path, 1);
                            while (!proc.StandardOutput.EndOfStream)
                            {
                                string line = proc.StandardOutput.ReadLine();
                                DebugLog(line);
                                if (ts.channel == null)
                                {
                                    // send back to channel
                                    IRC.DeliverMessage(line, ts.channel);
                                }
                                else
                                {
                                    // to user
                                    IRC.DeliverMessage(line, ts.user);
                                }
                            }
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
