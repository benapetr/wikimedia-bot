//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace wmib.Extensions
{
    // This whole module is a simple stupid attempt to give a bot some basic ability to communicate with others
    public class AI : Module
    {
        private Dictionary<string, string> AutoResponses = new Dictionary<string, string>()
        {
            { "how are you",   "I am a robot, so as long as the power is on, I am fine :)" },
            { "how are you doing",   "I am a robot, so as long as the power is on, I am fine :)" },
            { "how do you do", "I am a robot, so as long as the power is on, I am doing just fine..." },
            { "howdy", "howdy!" },
            { "hello", "hi! I am a robot :)" },
            { "hi", "hi! I am a robot :)" },
            { "hey", "hi! I am a robot :)" },
            { "who are you",  "I am a robot, I am here to help you when all humans are too busy" },
            { "what are you", "I am a robot, I am here to help you when all humans are too busy" },
            { "what are you doing", "I am waiting for a command :)" },
            { "can you help me", "I am just a stupid robot, you can try to search my infobot db (use @search <keyword>) for an answer" },
            { "can you help",    "I am just a stupid robot, you can try to search my infobot db (use @search <keyword>) for an answer" }
        };

        private Dictionary<string, string> autoQuestionResponses = new Dictionary<string, string>()
        {
            { "my nick", "Your nick is $invoker.nick" },
            { "my ident", "Your ident is $invoker.ident" },
            { "my host", "Your hostname is $invoker.host" },
            { "my hostname", "Your hostname is $invoker.host" }
        };

        private List<string> questionStarts = new List<string>() { "can you tell me ", "could you tell me ", "can I ask ", "what is ", "what's " };

        public AI()
        {
        }

        public override bool Construct()
        {
            this.Version = new Version(1, 0, 0, 0);
            this.HasSeparateThreadInstance = false;
            this.RestartOnModuleCrash = true;
            return true;
        }

        public override bool Hook_OnUnload()
        {
            UnregisterCommand("ai-on");
            UnregisterCommand("ai-off");
            //UnregisterCommand("kick");
            return base.Hook_OnUnload();
        }

        public override bool Hook_OnRegister()
        {
            RegisterCommand(new GenericCommand("ai-on", this.aion, false, "admin"));
            RegisterCommand(new GenericCommand("ai-off", this.aioff, false, "admin"));
            return base.Hook_OnRegister();
        }

        private void aion(CommandParams p)
        {
            if (GetConfig(p.SourceChannel, "AI.Enabled", false))
            {
                IRC.DeliverMessage("AI is already enabled", p.SourceChannel);
                return;
            }
            IRC.DeliverMessage("AI enabled", p.SourceChannel.Name);
            SetConfig(p.SourceChannel, "AI.Enabled", true);
            p.SourceChannel.SaveConfig();
        }

        private void aioff(CommandParams p)
        {
            if (!GetConfig(p.SourceChannel, "AI.Enabled", false))
            {
                IRC.DeliverMessage("AI was already disabled", p.SourceChannel);
                return;
            }
            IRC.DeliverMessage("AI disabled", p.SourceChannel.Name);
            SetConfig(p.SourceChannel, "AI.Enabled", false);
            p.SourceChannel.SaveConfig();
        }

        private string NoSpecials(string text)
        {
            text = text.Replace(",", " ").Replace("!", " ").Replace(".", " ").Replace("?", " ");
            while (text.Contains("  "))
                text = text.Replace("  ", " ");
            if (text.StartsWith(" ", StringComparison.InvariantCulture))
                text = text.Substring(1);

            if (text.EndsWith(" ", StringComparison.InvariantCulture))
                text = text.Substring(0, text.Length - 1);

            return text;
        }

        private void Respond(Channel channel, libirc.UserInfo invoker, string message)
        {
            message = message.Replace("$invoker.nick", invoker.Nick)
                             .Replace("$invoker.host", invoker.Host)
                             .Replace("$invoker.ident", invoker.Ident);

            IRC.DeliverMessage(invoker.Nick + ": " + message, channel);
        }

        private bool processQuestion(Channel channel, libirc.UserInfo invoker, string question)
        {
            if (question.StartsWith(" "))
                question = question.Substring(1);

            if (question.StartsWith("what is "))
                question = question.Substring("what is ".Length);

            if (autoQuestionResponses.ContainsKey(question))
            {
                this.Respond(channel, invoker, autoQuestionResponses[question]);
                return true;
            }

            return false;
        }

        private void ProcessInput(Channel channel, libirc.UserInfo invoker, string message)
        {
            string l_message = message.ToLower();
            string ns_message = NoSpecials(message);

            if (this.AutoResponses.ContainsKey(ns_message))
            {
                this.Respond(channel, invoker, this.AutoResponses[ns_message]);
                return;
            }

            bool start_with_question = false;
            foreach (string question in this.questionStarts)
            {
                if (ns_message.StartsWith(question, StringComparison.InvariantCulture))
                {
                    start_with_question = true;
                    break;
                }
            }

            if (start_with_question || l_message.EndsWith("?", StringComparison.InvariantCulture))
            {
                string question = ns_message;
                if (start_with_question)
                {
                    foreach (string qs in this.questionStarts)
                    {
                        if (question.StartsWith(qs, StringComparison.InvariantCulture))
                        {
                            question.Substring(qs.Length);
                            break;
                        }
                    }
                }
                if (question.EndsWith("?", StringComparison.InvariantCulture))
                {
                    question = question.Substring(0, question.Length - 1);
                }

                if (this.processQuestion(channel, invoker, question))
                    return;
            }

            this.Respond(channel, invoker, "Sorry, but I don't know to respond to this. Please keep in mind I am just a stupid robot, I can't hold an intelligent conversation.");
        }

        public override void Hook_PRIV(Channel channel, libirc.UserInfo invoker, string message)
        {
            if (!GetConfig(channel, "AI.Enabled", false))
                return;

            // These are commands sent directly to bot
            if (!message.StartsWith(channel.PrimaryInstance.Nick + ": ", StringComparison.InvariantCulture))
                return;

            message = message.Substring(channel.PrimaryInstance.Nick.Length + 2);

            this.ProcessInput(channel, invoker, message);
        }
    }
}
