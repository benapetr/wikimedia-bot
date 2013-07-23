using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    /// <summary>
    /// This class is handling the system terminal, it writes to it using a single thread so that
    /// multiple threads do not break their messages as they write to console in same moment
    /// </summary>
    public class Logging
    {
        public class Message
        {
            public DateTime T;
            public string Text;
            public bool Warning;

            public Message(string text, DateTime time, bool warning)
            {
                Text = text;
                T = time;
                Warning = warning;
            }
        }

        public static List<Message> messages = new List<Message>();

        public static void Write(string Message, bool warning)
        {
            Message message = new Message(Message, DateTime.Now, warning);
            lock (messages)
            {
                messages.Add(message);
            }
        }

        public static void Display(DateTime time, string Message, bool Warning)
        {
            if (config.Colors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            if (Warning)
            {
                if (config.Colors)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                Console.Write("LOG (WARNING)");
            }
            else
            {
                Console.Write("LOG ");
            }
            if (config.Colors)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            Console.Write("[{0}]", time.ToString());
            if (config.Colors)
            {
                Console.ResetColor();
            }
            Console.WriteLine(": " + Message);
        }

        public static void Exec()
        {
            try
            {
                while (true)
                {
                    if (messages.Count > 0)
                    {
                        List<Message> priv = new List<Message>();
                        lock (messages)
                        {
                            priv.AddRange(messages);
                            messages.Clear();
                        }
                        foreach (Message message in priv)
                        {
                            Display(message.T, message.Text, message.Warning);
                        }
                    }
                    Thread.Sleep(100);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }
    }
}
