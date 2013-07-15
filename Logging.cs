using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
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
            Console.ForegroundColor = ConsoleColor.Red;
            if (Warning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("LOG (WARNING)");
            }
            else
            {
                Console.Write("LOG ");
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[{0}]", time.ToString());
            Console.ResetColor();
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
