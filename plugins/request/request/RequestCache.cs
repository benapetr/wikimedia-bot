using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public class RequestCache
    {
        public static string file = variables.config + "/" + "requests.temp";
        public static List<string> Done = new List<string>();

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
            core.Log("DEBUG: loading cache for requests");
            if (!System.IO.File.Exists(file))
            {
                return;
            }
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
