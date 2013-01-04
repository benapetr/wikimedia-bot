using System;
using System.Data;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace wmib
{
    public class RegularModule : Module
    {
        public static List<Nova> ProjectList = new List<Nova>();
        public static List<Instance> Instances = new List<Instance>();
        public static DateTime LastUpdate = DateTime.Now;
        public static bool OK = false;

        public class Instance
        {
            public string OriginalName;
            public string Name;
            public string Host;
            public string IP;
            public string Type;
            public string ImageID = "";
            public string Ram = "";
            public string NumberOfCpu = "";
            public string ModifyTime = "";
            public string Storage = "";
            public string FullUrl = "";
            public string fqdn = "";
            public string LaunchTime = "";
            public List<string> Puppets = new List<string>();
            public bool Online;
            private string project_name = "";
            public Nova project;
            public string Project
            {
                set
                {
                    project_name = value;
                    project = getProject(value);
                }
                get
                {
                    return project_name;
                }
            }
            public Instance(string real, string name, string host, string ip, string type, bool online)
            {
                OriginalName = real;
                IP = ip;
                Online = online;
                Name = name;
                Type = type;
                Host = host;
            }
        }

        public class Nova
        {
            public List<Instance> instances = new List<Instance>();
            public List<string> users = new List<string>();
            public string Name;
            public string Description;
            public Nova(string name)
            {
                Name = name;
            }
        }

        public static string getProjects(string user)
        {
            string result = "";
            lock (ProjectList)
            {
                foreach (Nova x in ProjectList)
                {
                    if (x.users.Contains(user))
                    {
                        result += x.Name + ", ";
                    }
                }
            }
            return result;
        }

        public static Instance getInstance(string project)
        {
            lock (Instances)
            {
                foreach (Instance x in Instances)
                {
                    if (x.OriginalName == project)
                    {
                        return x;
                    }
                }
            }
            return null;
        }

        public static string WildcardToRegex(string pattern)
        {
            return "^" + System.Text.RegularExpressions.Regex.Escape(pattern).
                               Replace(@"\*", ".*").
                               Replace(@"\?", ".") + "$";
        }

        public static void deleteInstance(Instance instance)
        {
            try
            {
                lock (Instances)
                {
                    if (Instances.Contains(instance))
                    {
                        Instances.Remove(instance);
                    }
                }
                lock (ProjectList)
                {
                    foreach (Nova project in ProjectList)
                    {
                        if (project.instances.Contains(instance))
                        {
                            project.instances.Remove(instance);
                        }
                    }
                }
            }
            catch (Exception r)
            {
                core.handleException(r);
            }
        }

        public static Nova getProject(string project)
        {
            project = project.ToLower();
            lock (ProjectList)
            {
                foreach (Nova x in ProjectList)
                {
                    if (x.Name.ToLower() == project)
                    {
                        return x;
                    }
                }
            }
            return null;
        }

        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain,
                                      System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }



        private static bool Download(string file, string where)
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = Validator;
                System.Net.WebClient _b = new System.Net.WebClient();
                _b.DownloadFile(file, where);
                return true;
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return false;
        }

        public string getValue(ref JsonTextReader b, bool silent = false)
        {
            while (true)
            {
                if (!b.Read())
                {
                    return "{unknown}";
                }
                if (b.Value != null)
                {
                    if (b.TokenType == JsonToken.PropertyName)
                    {
                        if (!silent)
                        {
                            core.Log("JSON returned property when I requested value", true);
                        }
                        return "{unknown}";
                    }
                    if (b.TokenType != JsonToken.Comment && b.Value != null)
                    {
                        if (b.ValueType.ToString() == "")
                        {
                            return "{unknown}";
                        }
                        return b.Value.ToString();
                    }
                }
            }
        }

        public void JSON()
        {
            try
            {
                string URL = "https://labsconsole.wikimedia.org/wiki/Special:Ask/-5B-5BResource-20Type::instance-5D-5D/-3FInstance-20Name/-3FInstance-20Type/-3FProject/-3FImage-20Id/-3FFQDN/-3FLaunch-20Time/-3FPuppet-20Class/-3FModification-20date/-3FInstance-20Host/-3FNumber-20of-20CPUs/-3FRAM-20Size/-3FAmount-20of-20Storage/limit%3D500/format%3Djson";
                string temp = System.IO.Path.GetTempFileName();
                if (Download(URL, temp))
                {
                    List<Instance> deleted = new List<Instance>();
                    lock (Instances)
                    {
                        deleted.AddRange(Instances);
                    }
                    System.IO.StreamReader a = System.IO.File.OpenText(temp);
                    JsonTextReader b = new JsonTextReader(a);
                    string name = "{unknown}";
                    string host = "{unknown}";
                    string ip = "{unknown}";
                    string project = "{unknown}";
                    string resource = "{unknown}";
                    string image = "{unknown}";
                    string Ram = "{unknown}";
                    string NumberOfCpu = "{unknown}";
                    string ModifyTime = "{unknown}";
                    string Storage = "{unknown}";
                    string FullUrl = "{unknown}";
                    string fqdn = "{unknown}";
                    string LaunchTime = "{unknown}";
                    string type = "{unknown}";
                    while (b.Read())
                    {
                        if (b.TokenType == JsonToken.PropertyName)
                        {
                            if (b.Value == null)
                            {
                                core.Log("DEBUG: null at value");
                                continue;
                            }
                            string value = b.Value.ToString();
                            if (value.StartsWith("Nova Resource:"))
                            {
                                value = value.Substring("Nova Resource:".Length);
                                if (resource != "{unknown}")
                                {
                                    Instance instance = getInstance(resource);
                                    if (instance != null)
                                    {
                                        if (deleted.Contains(instance))
                                        {
                                            deleted.Remove(instance);
                                        }
                                        deleteInstance(instance);
                                    }
                                    try
                                    {
                                        IPAddress[] addresslist = Dns.GetHostAddresses(resource);

                                        if (addresslist.Length > 0)
                                        {
                                            ip = addresslist[0].ToString();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        core.Log("Debug: can't resolve" + resource);
                                    }
                                    instance = new Instance(resource, name, host, ip, type, true);
                                    instance.fqdn = fqdn;
                                    instance.FullUrl = FullUrl;
                                    instance.ImageID = image;
                                    instance.LaunchTime = LaunchTime;
                                    instance.ModifyTime = ModifyTime;
                                    instance.NumberOfCpu = NumberOfCpu;
                                    instance.Project = project;
                                    instance.Ram = Ram;
                                    instance.Storage = Storage;
                                    lock (Instances)
                                    {
                                        Instances.Add(instance);
                                    }
                                    name = "{unknown}";
                                    host = "{unknown}";
                                    ip = "{unknown}";
                                    project = "{unknown}";
                                    resource = "{unknown}";
                                    image = "{unknown}";
                                    Ram = "{unknown}";
                                    NumberOfCpu = "{unknown}";
                                    ModifyTime = "{unknown}";
                                    Storage = "{unknown}";
                                    FullUrl = "{unknown}";
                                    fqdn = "{unknown}";
                                    LaunchTime = "{unknown}";
                                    type = "{unknown}";
                                }
                                resource = value;
                            }
                            switch (b.Value.ToString())
                            {
                                case "Instance Name":
                                    name = getValue(ref b);
                                    break;
                                case "Instance Type":
                                    type = getValue(ref b);
                                    break;
                                case "Project":
                                    project = getValue(ref b);
                                    break;
                                case "Image Id":
                                    image = getValue(ref b);
                                    break;
                                case "FQDN":
                                    fqdn = getValue(ref b);
                                    break;
                                case "Launch Time":
                                    continue;
                                case "Puppet Class":
                                    break;
                                case "Modification date":
                                    ModifyTime = getValue(ref b);
                                    break;
                                case "Instance Host":
                                    host = getValue(ref b);
                                    break;
                                case "Number of CPUs":
                                    NumberOfCpu = getValue(ref b);
                                    break;
                                case "RAM Size":
                                    Ram = getValue(ref b);
                                    break;
                                case "Amount of Storage":
                                    Storage = getValue(ref b);
                                    break;
                                case "fullurl":
                                    FullUrl = getValue(ref b);
                                    break;
                            }
                        }
                    }
                    System.IO.File.Delete(temp);
                    foreach (Instance x in deleted)
                    {
                        deleteInstance(x);
                    }
                }
                else
                {
                    core.Log("Failed to download db file", true);
                }
                URL = "https://labsconsole.wikimedia.org/wiki/Special:Ask/-5B-5BResource-20Type::project-5D-5D/-3F/-3FMember/-3FDescription/mainlabel%3D-2D/searchlabel%3Dprojects/offset%3D0/format%3Djson";
                temp = System.IO.Path.GetTempFileName();
                if (Download(URL, temp))
                {
                    System.IO.StreamReader a = System.IO.File.OpenText(temp);
                    JsonTextReader b = new JsonTextReader(a);
                    string name = "{unknown}";
                    List<Nova> projectlist2 = new List<Nova>();
                    List<string> members = new List<string>();
                    string description = "{unknown}";
                    while (b.Read())
                    {
                        if (b.Value == null)
                        {
                            continue;
                        }
                        //Console.WriteLine(b.Value.ToString());
                        string value = b.Value.ToString();
                        if (value.StartsWith("Nova Resource:"))
                        {
                            value = value.Substring("Nova Resource:".Length);
                            if (name != "{unknown}")
                            {
                                Nova project = null;
                                foreach (Nova x in projectlist2)
                                {
                                    if (x.Name == value)
                                    {
                                        project = x;
                                        break;
                                    }
                                }
                                if (project == null)
                                {
                                    project = new Nova(name);
                                }
                                project.users.Clear();
                                project.users.AddRange(members);
                                if (!(description == "" && project.Description == ""))
                                {
                                    project.Description = description;
                                } else if ( project.Description == "" )
                                {
                                    project.Description = "there is no description for this item";
                                }
                                description = "";
                                if (!projectlist2.Contains(project))
                                {
                                    projectlist2.Add(project);
                                }
                                members.Clear();
                            }
                            name = value;
                        }
                        if (value.StartsWith("User:"))
                        {
                            members.Add(value.Substring(5));
                        }
                        if (value == "Description")
                        {
                            description = getValue(ref b, true);
                        }
                    }
                    System.IO.File.Delete(temp);
                    lock (ProjectList)
                    {
                        ProjectList.Clear();
                        ProjectList.AddRange(projectlist2);
                    }
                    lock (Instances)
                    {
                        foreach (Instance i in Instances)
                        {
                            Nova nova = getProject(i.Project);
                            if (nova != null)
                            {
                                i.project = nova;
                                lock (nova.instances)
                                {
                                    if (!nova.instances.Contains(i))
                                    {
                                        nova.instances.Add(i);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    core.Log("Failed to download db file", true);
                }
                OK = true;
                LastUpdate = DateTime.Now;
            }
            catch (Exception t)
            {
                core.Log(t.Data + t.StackTrace);
                core.handleException(t);
            }
        }

        public TimeSpan time()
        {
            return (DateTime.Now - LastUpdate);
        }

        public override bool Construct()
        {
            Name = "Labs";
            start = true;
            Version = "1.2.90.0";
            return true;
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (message.StartsWith("@labs-off"))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!GetConfig(channel, "LABS.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Labs utilities are already disabled", channel.Name);
                        return;
                    }
                    SetConfig(channel, "LABS.Enabled", false);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage("Labs utilities were disabled", channel.Name);
                    return;
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
                return;
            }


            if (message.StartsWith("@labs-on"))
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (GetConfig(channel, "LABS.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Labs utilities are already enabled", channel.Name);
                        return;
                    }
                    SetConfig(channel, "LABS.Enabled", true);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage("Labs utilities were enabled", channel.Name);
                    return;
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
                return;
            }

            if (message.StartsWith("@labs-user "))
            {
                if (GetConfig(channel, "LABS.Enabled", false))
                {
                    string user = message.Substring(11);
                    string result = getProjects(user);
                    if (result != "")
                    {
                        core.irc._SlowQueue.DeliverMessage(user + " is member of these projects: " + result, channel.Name);
                        return;
                    }
                    core.irc._SlowQueue.DeliverMessage("That user is not a member of any project", channel.Name);
                    return;
                }
            }

            if (message.StartsWith("@labs-info "))
            {
                if (GetConfig(channel, "LABS.Enabled", false))
                {
                    string host = message.Substring("@labs-info ".Length);
                    string results = "";
                    if (!OK)
                    {
                        core.irc._SlowQueue.DeliverMessage("Please wait, I still didn't retrieve the labs datafile containing the list of instances", channel.Name);
                        return;
                    }
                    Instance instance = getInstance(host);
                    if (instance == null)
                    {
                        instance = resolve(host);
                        if (instance != null)
                        {
                            results = "[Name " + host + " doesn't exist but resolves to " + instance.OriginalName + "] ";
                        }
                    }
                    if (instance != null)
                    {
                        results += instance.OriginalName + " is Nova Instance with name: " + instance.Name + ", host: " + instance.Host + ", IP: " + instance.IP
                            + " of type: " + instance.Type + ", with number of CPUs: " + instance.NumberOfCpu + ", RAM of this size: " + instance.Ram
                            + "M, member of project: " + instance.Project + ", size of storage: " + instance.Storage + " and with image ID: " + instance.ImageID;

                        core.irc._SlowQueue.DeliverMessage(results, channel.Name);
                        return;
                    }
                    core.irc._SlowQueue.DeliverMessage("I don't know this instance, sorry, try browsing the list by hand, but I can guarantee there is no such instance matching this name, host or Nova ID unless it was created less than " + time().Seconds.ToString() + " seconds ago", channel.Name);
                }
            }

            if (message.StartsWith("@labs-resolve "))
            {
                if (GetConfig(channel, "LABS.Enabled", false))
                {
                    string host = message.Substring("@labs-resolve ".Length);
                    if (!OK)
                    {
                        core.irc._SlowQueue.DeliverMessage("Please wait, I still didn't retrieve the labs datafile containing the list of instances", channel.Name);
                        return;
                    }
                    Instance instance = resolve(host);
                    if (instance != null)
                    {
                        string d = "The " + host + " resolves to instance " + instance.OriginalName + " with a fancy name " + instance.Name + " and IP " + instance.IP;
                        core.irc._SlowQueue.DeliverMessage(d, channel.Name);
                        return;
                    }
                    string names = "";
                    lock (Instances)
                    {
                        foreach (Instance instance2 in Instances)
                        {
                            if (instance2.Name.Contains(host) || instance2.OriginalName.Contains(host))
                            {
                                names += instance2.OriginalName + " (" + instance2.Name + "), ";
                                if (names.Length > 210)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (names != "")
                    {
                        core.irc._SlowQueue.DeliverMessage("I don't know this instance - aren't you are looking for: " + names, channel.Name);
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage("I don't know this instance, sorry, try browsing the list by hand, but I can guarantee there is no such instance matching this name, host or Nova ID unless it was created less than " + time().Seconds.ToString() + " seconds ago", channel.Name);
                    }
                }
            }

            if (message.StartsWith("@labs-project-users "))
            {
                if (GetConfig(channel, "LABS.Enabled", false))
                {
                    string host = message.Substring("@labs-project-users ".Length);
                    if (!OK)
                    {
                        core.irc._SlowQueue.DeliverMessage("Please wait, I still didn't retrieve the labs datafile containing the list of projects", channel.Name);
                        return;
                    }
                    Nova project = getProject(host);
                    if (project != null)
                    {
                        string instances = "";
                        lock (project.instances)
                        {
                            foreach (string x in project.users)
                            {
                                instances = instances + x + ", ";
                            }
                            core.irc._SlowQueue.DeliverMessage("Following users are in this project: " + instances, channel.Name);
                            return;
                        }
                    }
                    string names = getProjectsList(host);
                    if (names != "")
                    {
                        core.irc._SlowQueue.DeliverMessage("I don't know this project, did you mean: " + names + "? I can guarantee there is no such project matching this name unless it has been created less than " + time().Seconds.ToString() + " seconds ago", channel.Name);
                        return;
                    }
                    core.irc._SlowQueue.DeliverMessage("I don't know this project, sorry, try browsing the list by hand, but I can guarantee there is no such project matching this name unless it has been created less than " + time().Seconds.ToString() + " seconds ago", channel.Name);
                    return;
                }
            }

            if (message.StartsWith("@labs-project-instances "))
            {
                if (GetConfig(channel, "LABS.Enabled", false))
                {
                    string host = message.Substring("@labs-project-instances ".Length);
                    if (!OK)
                    {
                        core.irc._SlowQueue.DeliverMessage("Please wait, I still didn't retrieve the labs datafile containing the list of projects", channel.Name);
                        return;
                    }
                    Nova project = getProject(host);
                    if (project != null)
                    {
                        string instances = "";
                        lock (project.instances)
                        {
                            foreach (Instance x in project.instances)
                            {
                                instances = instances + x.Name + ", ";
                            }
                            core.irc._SlowQueue.DeliverMessage("Following instances are in this project: " + instances, channel.Name);
                            return;
                        }
                    }
                    string names = getProjectsList(host);
                    if (names != "")
                    {
                        core.irc._SlowQueue.DeliverMessage("I don't know this project, did you mean: " + names + "? I can guarantee there is no such project matching this name unless it has been created less than " + time().Seconds.ToString() + " seconds ago", channel.Name);
                        return;
                    }
                    core.irc._SlowQueue.DeliverMessage("I don't know this project, sorry, try browsing the list by hand, but I can guarantee there is no such project matching this name unless it has been created less than " + time().Seconds.ToString() + " seconds ago", channel.Name);
                }
            }

            if (message.StartsWith("@labs-project-info "))
            {
                if (GetConfig(channel, "LABS.Enabled", false))
                {
                    string host = message.Substring("@labs-project-info ".Length);
                    if (!OK)
                    {
                        core.irc._SlowQueue.DeliverMessage("Please wait, I still didn't retrieve the labs datafile containing the list of projects", channel.Name);
                        return;
                    }
                    Nova project = getProject(host);
                    if (project != null)
                    {
                        string d = "The project " + project.Name + " has " + project.instances.Count.ToString() + " instances and " + project.users.Count.ToString() + " members, description: " + project.Description;
                        core.irc._SlowQueue.DeliverMessage(d, channel.Name);
                        return;
                    }
                    string names = getProjectsList(host);
                    if (names != "")
                    {
                        core.irc._SlowQueue.DeliverMessage("I don't know this project, did you mean: " + names + "? I can guarantee there is no such project matching this name unless it has been created less than " + time().Seconds.ToString() + " seconds ago", channel.Name);
                        return;
                    }
                    core.irc._SlowQueue.DeliverMessage("I don't know this project, sorry, try browsing the list by hand, but I can guarantee there is no such project matching this name unless it has been created less than " + time().Seconds.ToString() + " seconds ago", channel.Name);
                    return;
                }
            }
        }

        public string getProjectsList(string host)
        {
            string names = "";
            List<string> projects = new List<string>();
            lock (ProjectList)
            {
                foreach (Nova instance2 in ProjectList)
                {
                    if (projects.Contains(instance2.Name) != true)
                    {
                        projects.Add(instance2.Name);
                    }
                }
                host = host.ToLower();
                foreach (string instance2 in projects)
                {
                    if (instance2.ToLower().Contains(host))
                    {
                        names += instance2 + ", ";
                        if (names.Length > 210)
                        {
                            break;
                        }
                    }
                }
            }
            if (names.EndsWith(", "))
                        {
                            names = names.Substring(0, names.Length - 2);
                        }
            return names;
        }

        public Instance resolve(string name)
        {
            name = name.ToLower();
            lock (Instances)
            {
                foreach (Instance g in Instances)
                {
                    if (g.OriginalName.ToLower() == name)
                    {
                        return g;
                    }
                }
                foreach (Instance g in Instances)
                {
                    if (g.Name.ToLower() == name)
                    {
                        return g;
                    }
                }
                foreach (Instance g in Instances)
                {
                    if (g.Host.ToLower() == name)
                    {
                        return g;
                    }
                }
                foreach (Instance g in Instances)
                {
                    if (g.fqdn.ToLower() == name)
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        public override bool Hook_OnUnload()
        {
            core.Help.Unregister("labs-on");
            core.Help.Unregister("labs-off");
            core.Help.Unregister("labs-resolve");
            core.Help.Unregister("labs-info");
            core.Help.Unregister("labs-project-info");
            core.Help.Unregister("labs-project-instances");
            core.Help.Unregister("labs-project-users");
            core.Help.Unregister("labs-user");
            bool ok = true;
            return ok;
        }

        public override void Load()
        {
            try
            {
                core.Help.Register("labs-on", "Turn on the labs tool");
                core.Help.Register("labs-off", "Turn off the labs tool");
                core.Help.Register("labs-resolve", "Retrieve information about object");
                core.Help.Register("labs-info", "Display all known info about the object");
                core.Help.Register("labs-user", "Display information about user");
                core.Help.Register("labs-project-info", "Display information about the project");
                core.Help.Register("labs-project-instances", "List all instances in a project");
                core.Help.Register("labs-project-users", "Display all users in a project");

                while (true)
                {
                    JSON();
                    System.Threading.Thread.Sleep(200000);
                }
            }
            catch (Exception f)
            {
                core.handleException(f);
            }
        }

    }
}
