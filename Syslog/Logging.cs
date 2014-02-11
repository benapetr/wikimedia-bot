//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    public class Syslog
    {
        public enum Type
        {
            Error,
            Debug,
            Warning,
            Normal
        }

        /// <summary>
        /// Log the specified Message and MessageType.
        /// </summary>
        /// <param name='Message'>
        /// If set to <c>true</c> message.
        /// </param>
        /// <param name='MessageType'>
        /// If set to <c>true</c> message type.
        /// </param>
        public static bool Log(string Message, Type MessageType)
        {
            Logging.Write(Message, MessageType);
            SystemHooks.SystemLog(Message, MessageType);
            return true;
        }

        /// <summary>
        /// Log the specified message
        /// </summary>
        /// <param name='Message'>
        /// Message that you want to log.
        /// </param>
        /// <param name='Warning'>
        /// If this is true the message will be classified as a warning.
        /// </param>
        public static bool Log(string Message, bool Warning = false)
        {
            Type MessageType = Type.Normal;
            if (Warning)
            {
                MessageType = Type.Warning;
            }
            Logging.Write(Message, MessageType);
            SystemHooks.SystemLog(Message, MessageType);
            return true;
        }

        /// <summary>
        /// Log the specified message
        /// </summary>
        /// <param name='Message'>
        /// Message that you want to log.
        /// </param>
        public static bool WarningLog(string Message)
        {
            Syslog.Log(Message, true);
            return true;
        }

        /// <summary>
        /// Log the specified message
        /// </summary>
        /// <param name='Message'>
        /// Message that you want to log.
        /// </param>
        public static bool ErrorLog(string Message)
        {
            Syslog.Log(Message, Type.Error);
            return true;
        }

        /// <summary>
        /// Writes the message immediately to console with no thread sync
        /// </summary>
        /// <returns>
        /// The now.
        /// </returns>
        /// <param name='Message'>
        /// Message that you want to log.
        /// </param>
        /// <param name='Warning'>
        /// If this is true the message will be classified as a warning.
        /// </param>
        public static bool WriteNow(string Message, bool Warning = false)
        {
            Syslog.Type _Type = Type.Normal;
            if (Warning)
            {
                _Type = Type.Warning;
            }
            Logging.Display(DateTime.Now, Message, _Type);
            SystemHooks.SystemLog(Message, _Type);
            return true;
        }

        /// <summary>
        /// Writes the message immediately to console with no thread sync
        /// </summary>
        /// <returns>
        /// The now.
        /// </returns>
        /// <param name='Message'>
        /// If set to <c>true</c> message.
        /// </param>
        /// <param name='MessageType'>
        /// If set to <c>true</c> message type.
        /// </param>
        public static bool WriteNow(string Message, Syslog.Type MessageType)
        {
            Logging.Display(DateTime.Now, Message, MessageType);
            SystemHooks.SystemLog(Message, MessageType);
            return true;
        }

        /// <summary>
        /// Debug log
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="Verbosity"></param>
        public static void DebugLog(string Message, int Verbosity = 1)
        {
            if (Configuration.System.SelectedVerbosity >= Verbosity)
            {
                Syslog.Log("DEBUG <" + Verbosity.ToString() + ">: " + Message);
            }
        }

        /// <summary>
        /// Debug log
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="Verbosity"></param>
        public static void DebugWrite(string Message, int Verbosity = 1)
        {
            if (Configuration.System.SelectedVerbosity >= Verbosity)
            {
                Syslog.WriteNow("DEBUG <" + Verbosity.ToString() + ">: " + Message);
            }
        }
    }

    /// <summary>
    /// This class is handling the system terminal, it writes to it using a single thread so that
    /// multiple threads do not break their messages as they write to console in same moment
    /// </summary>
    public class Logging
    {
        /// <summary>
        /// Message that needs to be printed
        /// </summary>
        public class Message
        {
            /// <summary>
            /// Time
            /// </summary>
            public DateTime Time;
            /// <summary>
            /// Message
            /// </summary>
            public string Text;
            /// <summary>
            /// Is a warning
            /// </summary>
            public Syslog.Type _Type;

            /// <summary>
            /// Initializes a new instance of the class.
            /// </summary>
            /// <param name='text'>
            /// Text.
            /// </param>
            /// <param name='time'>
            /// Time.
            /// </param>
            /// <param name='MT'>
            /// M.
            /// </param>
            public Message(string text, DateTime time, Syslog.Type MT)
            {
                this.Text = text;
                this.Time = time;
                this._Type = MT;
            }
        }

        /// <summary>
        /// Database of messages that needs to be written
        /// </summary>
        public static List<Message> messages = new List<Message>();

        /// <summary>
        /// Write a message to terminal
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="MessageType"></param>
        public static void Write(string Message, Syslog.Type MessageType)
        {
            Message message = new Message(Message, DateTime.Now, MessageType);
            lock (messages)
            {
                messages.Add(message);
            }
        }

        /// <summary>
        /// Write a message
        /// </summary>
        /// <param name="time"></param>
        /// <param name="Message"></param>
        /// <param name="MessageType"></param>
        public static void Display(DateTime time, string Message, Syslog.Type MessageType)
        {
            if (Configuration.System.Colors)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
            }
            Console.Write("LOG ");
            if (Configuration.System.Colors)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            Console.Write("[{0}]", time.ToString());
            if (MessageType == Syslog.Type.Warning)
            {
                if (Configuration.System.Colors)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                Console.Write(" [WARNING]");
            } else if (MessageType == Syslog.Type.Error)
            {
                if (Configuration.System.Colors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.Write("   [ERROR]");
            } else
            {
                if (Configuration.System.Colors)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                }
                Console.Write("    [INFO]");
            }
            if (Configuration.System.Colors)
            {
                Console.ResetColor();
            }
            Console.WriteLine(": " + Message);
        }

        private static void Flush()
        {
            if (messages.Count > 0)
            {
                List<Message> priv = new List<Message>();
                lock(messages)
                {
                    priv.AddRange(messages);
                    messages.Clear();
                }
                foreach (Message message in priv)
                {
                    Display(message.Time, message.Text, message._Type);
                }
            }
        }

        /// <summary>
        /// Execute thread
        /// </summary>
        public static void Exec()
        {
            try
            {
                Core.ThreadManager.Logger = true;
                while (Core.IsRunning)
                {
                    Flush();
                    Thread.Sleep(100);
                }
            }
            catch (ThreadAbortException)
            {
                Flush();
                Core.ThreadManager.Logger = false;
                return;
            }
            Flush();
            Core.ThreadManager.Logger = false;
        }
    }
}
