//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace wmib
{
    public partial class IRC
    {
        /// <summary>
        /// Queue of all messages that should be delivered to some network
        /// </summary>
        public class MessageQueue
        {
            /// <summary>
            /// Message
            /// </summary>
            public class Message
            {
                /// <summary>
                /// Priority
                /// </summary>
                public priority MessagePriority;
                /// <summary>
                /// Message itself
                /// </summary>
                public string Text;
                /// <summary>
                /// Channel which the message should be delivered to
                /// </summary>
                public string Channel = null;
                public Channel pChannel = null;
                /// <summary>
                /// If this is true the message will be sent as raw command
                /// </summary>
                public bool Command = false;
            }

            private bool running = true;
            /// <summary>
            /// List of messages in queue which needs to be processed
            /// </summary>
            public List<Message> Messages = new List<Message>();
            /// <summary>
            /// List of new messages
            /// </summary>
            public List<Message> newmessages = new List<Message>();
            [NonSerialized]
            private IRC Parent;

            /// <summary>
            /// Creates new queue
            /// </summary>
            /// <param name="_parent">Parent object</param>
            public MessageQueue(IRC _parent)
            {
                Parent = _parent;
            }

            /// <summary>
            /// Deliver a message
            /// </summary>
            /// <param name="Message">Text</param>
            /// <param name="Channel">Channel</param>
            /// <param name="Pr">Priority</param>
            public void DeliverMessage(string Message, string Channel, priority Pr = priority.normal)
            {
                // first of all we check if we are in correct instance
                if (Channel.StartsWith("#"))
                {
                    Channel ch = Core.GetChannel(Channel);
                    if (ch == null)
                    {
                        Syslog.Log("Not sending message to unknown channel: " + Channel);
                        return;
                    }
                    // this is wrong instance so let's put this message to correct one
                    if (ch.PrimaryInstance != Parent.ParentInstance)
                    {
                        ch.PrimaryInstance.irc.Queue.DeliverMessage(Message, Channel, Pr);
                        return;
                    }
                }
                else
                {
                    lock (Core.TargetBuffer)
                    {
                        if (Core.TargetBuffer.ContainsKey(Channel))
                        {
                            if (Core.TargetBuffer[Channel] != Parent.ParentInstance)
                            {
                                Core.TargetBuffer[Channel].irc.Queue.DeliverMessage(Message, Channel, Pr);
                                return;
                            }
                        }
                    }
                }
                Message text = new Message { MessagePriority = Pr, Text = Message, Channel = Channel };
                lock (Messages)
                {
                    Messages.Add(text);
                }
            }

            /// <summary>
            /// Deliver me
            /// </summary>
            /// <param name="Message">Text</param>
            /// <param name="Channel">Channel</param>
            /// <param name="Pr">Priority</param>
            public void DeliverAct(string Message, string Channel, priority Pr = priority.normal)
            {
                // first of all we check if we are in correct instance
                if (Channel.StartsWith("#"))
                {
                    Channel ch = Core.GetChannel(Channel);
                    if (ch == null)
                    {
                        Syslog.Log("Not sending message to unknown channel: " + Channel);
                        return;
                    }
                    // this is wrong instance so let's put this message to correct one
                    if (ch.PrimaryInstance != Parent.ParentInstance)
                    {
                        ch.PrimaryInstance.irc.Queue.DeliverAct(Message, Channel, Pr);
                        return;
                    }
                }
                Message text = new Message { MessagePriority = Pr, Text = Message, Channel = Channel };
                lock (Messages)
                {
                    Messages.Add(text);
                }
            }

            /// <summary>
            /// Send a command to server
            /// </summary>
            /// <param name="Data"></param>
            /// <param name="Priority"></param>
            public void Send(string Data, priority Priority = priority.high)
            {
                Message text = new Message { MessagePriority = Priority, Channel = null, Text = Data, Command = true };
                lock (Messages)
                {
                    Messages.Add(text);
                }
            }

            /// <summary>
            /// Deliver a message
            /// </summary>
            /// <param name="Message">Text</param>
            /// <param name="User">User</param>
            /// <param name="Pr">Priority</param>
            public void DeliverMessage(string Message, User User, priority Pr = priority.low)
            {
                Message text = new Message { MessagePriority = Pr, Text = Message, Channel = User.Nick };
                lock (Messages)
                {
                    Messages.Add(text);
                }
            }

            /// <summary>
            /// Deliver a message
            /// </summary>
            /// <param name="Message">Text</param>
            /// <param name="Channel">Channel</param>
            /// <param name="Pr">Priority</param>
            public void DeliverMessage(string Message, Channel Channel, priority Pr = priority.normal)
            {
                if (Channel == null)
                {
                    Syslog.Log("Not sending message to null channel");
                    return;
                }
                // this is wrong instance so let's put this message to correct one
                if (Channel.PrimaryInstance != Parent.ParentInstance)
                {
                    Channel.PrimaryInstance.irc.Queue.DeliverMessage(Message, Channel, Pr);
                    return;
                }
                Message text = new Message { MessagePriority = Pr, Text = Message, pChannel = Channel };
                lock (Messages)
                {
                    Messages.Add(text);
                }
            }

            /// <summary>
            /// Disable queue
            /// </summary>
            public void Exit()
            {
                running = false;
                Syslog.Log("Turning off the message queue of instance " + Parent.ParentInstance.Nick + " with " + (newmessages.Count + Messages.Count).ToString() + " untransfered data");
                lock (Messages)
                {
                    Messages.Clear();
                }
                lock (newmessages)
                {
                    newmessages.Clear();
                }
            }

            private void Transfer(Message text)
            {
                if (text.Command)
                {
                    Parent.SendData(text.Text);
                    return;
                }
                if (text.pChannel != null)
                {
                    Parent.Message(text.Text, text.pChannel);
                }
                if (text.Channel != null)
                {
                    Parent.Message(text.Text, text.Channel);
                }
            }

            /// <summary>
            /// Internal function
            /// </summary>
            public void Run()
            {
                while (Core.IsRunning)
                {
                    try
                    {
                        if (!running)
                        {
                            return;
                        }
                        if (Messages.Count > 0)
                        {
                            lock (Messages)
                            {
                                newmessages.AddRange(Messages);
                                Messages.Clear();
                            }
                        }
                        if (newmessages.Count > 0)
                        {
                            List<Message> Processed = new List<Message>();
                            priority highest = priority.low;
                            lock (newmessages)
                            {
                                while (newmessages.Count > 0)
                                {
                                    // we need to get all messages that have been scheduled to be send
                                    lock (Messages)
                                    {
                                        if (Messages.Count > 0)
                                        {
                                            newmessages.AddRange(Messages);
                                            Messages.Clear();
                                        }
                                    }
                                    highest = priority.low;
                                    // we need to check the priority we need to handle first
                                    foreach (Message message in newmessages)
                                    {
                                        if (message.MessagePriority > highest)
                                        {
                                            highest = message.MessagePriority;
                                            if (message.MessagePriority == priority.high)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    // send highest priority first
                                    foreach (Message message in newmessages)
                                    {
                                        if (message.MessagePriority >= highest)
                                        {
                                            Processed.Add(message);
                                            Transfer(message);
                                            System.Threading.Thread.Sleep(Configuration.IRC.Interval);
                                            if (highest != priority.high)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    foreach (Message message in Processed)
                                    {
                                        if (newmessages.Contains(message))
                                        {
                                            newmessages.Remove(message);
                                        }
                                    }
                                }
                            }
                            lock (newmessages)
                            {
                                newmessages.Clear();
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
        }
    }
}

