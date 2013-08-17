using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public class RequestCache
    {
        public static string file = variables.config + "/requests.temp";
        public static string file_labs = variables.config + "/requests2.temp";
        public static List<string> Done = new List<string>();
        public static List<string> DoneLabs = new List<string>();

        public static void Insert(string name)
        {
            lock (Done)
            {
                if (!Contains(name))
                {
                    Done.Add(name);
                    System.IO.File.AppendAllText(file, name + Environment.NewLine);
                }
            }
        }

        public static bool ContainsLabs(string name)
        {
            lock (DoneLabs)
            {
                if (DoneLabs.Contains(name))
                {
                    return true;
                }
            }
            return false;
        }

        public static void InsertLabs(string name)
        {
            lock (DoneLabs)
            {
                if (!ContainsLabs(name))
                {
                    DoneLabs.Add(name);
                    System.IO.File.AppendAllText(file_labs, name + Environment.NewLine);
                }
            }
        }

        public static bool Contains(string name)
        {
            lock (Done)
            {
                if (Done.Contains(name))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Load()
        {
            core.DebugLog("Loading cache for requests", 6);

            if (System.IO.File.Exists(file_labs))
            {
                lock (DoneLabs)
                {
                    DoneLabs.Clear();
                    string[] data = System.IO.File.ReadAllLines(file_labs);
                    foreach (string line in data)
                    {
                        if (line != "")
                        {
                            DoneLabs.Add(line);
                        }
                    }
                }
            }

            if (System.IO.File.Exists(file))
            {
                lock (Done)
                {
                    Done.Clear();
                    string[] data = System.IO.File.ReadAllLines(file);
                    foreach (string line in data)
                    {
                        if (line != "")
                        {
                            Done.Add(line);
                        }
                    }
                }
            }

        }
    }
}
