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
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    /// <summary>
    /// This class allow you to format the mode to better look
    /// </summary>
    public class Formatter
    {
        private int ParametersPerOneLine = 2;
        private int ModesPerOneLine = 20;
        /// <summary>
        /// Prefix of mode
        /// </summary>
        public string Prefix = "";
        /// <summary>
        /// This buffer contains modes that belong to channel and is only filled up when you rewrite this formatter with custom mode
        /// </summary>
        public string channelModes = "";
        private string buffer = null;
        /// <summary>
        /// If this is true the produced string will remove the modes
        /// </summary>
        public bool Removing = false;
        private List<SimpleMode> Mode = new List<SimpleMode>();
        private List<SimpleMode> rMode = new List<SimpleMode>();

        /// <summary>
        /// Return a list of modes
        /// </summary>
        public List<SimpleMode> getMode
        {
            get
            {
                List<SimpleMode> mode = new List<SimpleMode>();
                lock (Mode)
                {
                    mode.AddRange(Mode);
                    return mode;
                }
            }
        }

        /// <summary>
        /// Return a list of modes that are being removed
        /// </summary>
        public List<SimpleMode> getRemovingMode
        {
            get
            {
                List<SimpleMode> mode = new List<SimpleMode>();
                lock (rMode)
                {
                    mode.AddRange(rMode);
                    return mode;
                }
            }
        }

        /// <summary>
        /// Creates new
        /// </summary>
        public Formatter() { }

        /// <summary>
        /// Require number of parameters and modes
        /// </summary>
        /// <param name="_ParametersPerOneLine"></param>
        /// <param name="_ModesPerOneLine"></param>
        public Formatter(int _ParametersPerOneLine, int _ModesPerOneLine)
        {
            ParametersPerOneLine = _ParametersPerOneLine;
            ModesPerOneLine = _ModesPerOneLine;
        }

        /// <summary>
        /// Insert a list of modes to parser
        /// </summary>
        /// <param name="mode"></param>
        public void InsertModes(List<SimpleMode> mode)
        {
            lock (Mode)
            {
                Mode.AddRange(mode);
            }
        }

        /// <summary>
        /// Changes the content based on buffer
        /// </summary>
        /// <param name="data"></param>
        public void RewriteBuffer(string data)
        {
            lock (Mode)
            {
                buffer = data;
                Mode.Clear();

                List<string> line = new List<string>();
                line.AddRange(data.Split('\n'));
                rMode.Clear();
                channelModes = "";
                string positive = "+";
                string negative = "-";
                foreach (string xx in line)
                {
                    bool rm = false;
                    int CurrentParam = 1;
                    List<string> parts = new List<string>();
                    parts.AddRange(xx.Split(' '));
                    if (parts.Count > 0)
                    {
                        if (parts[0].Contains("-") || parts[0].Contains("+"))
                        {
                            foreach (char CurrentMode in parts[0])
                            {
                                switch (CurrentMode)
                                {
                                    case '+':
                                        rm = false;
                                        continue;
                                    case '-':
                                        rm = true;
                                        continue;
                                }

                                // user mode, has a parameter
                                if (core.irc.CUModes.Contains(CurrentMode) || core.irc.PModes.Contains(CurrentMode))
                                {
                                    if (parts.Count < CurrentParam + 1)
                                    {
                                        core.DebugLog("Mode: " + xx + " is invalid and can't be parsed");
                                        return;
                                    }

                                    SimpleMode mode = new SimpleMode(CurrentMode, parts[CurrentParam]);
                                    if (rm)
                                    {
                                        rMode.Add(mode);
                                    }
                                    else
                                    {
                                        Mode.Add(mode);
                                    }
                                    CurrentParam++;
                                    continue;
                                }

                                // channel special mode with parameter
                                if (core.irc.SModes.Contains(CurrentMode) || core.irc.XModes.Contains(CurrentMode))
                                {
                                    if (parts.Count < CurrentParam + 1)
                                    {
                                        core.DebugLog("Mode: " + xx + " is invalid and can't be parsed");
                                        return;
                                    }

                                    SimpleMode mode = new SimpleMode(CurrentMode, parts[CurrentParam]);
                                    if (rm)
                                    {
                                        rMode.Add(mode);
                                        positive = positive.Replace(CurrentMode.ToString(), "");
                                        negative += CurrentMode.ToString();
                                    }
                                    else
                                    {
                                        Mode.Add(mode);
                                        negative = negative.Replace(CurrentMode.ToString(), "");
                                        positive += CurrentMode.ToString();
                                    }
                                    CurrentParam++;
                                    continue;
                                }

                                // channel mode
                                if (core.irc.CModes.Contains(CurrentMode))
                                {
                                    SimpleMode mode = new SimpleMode(CurrentMode, null);
                                    if (rm)
                                    {
                                        rMode.Add(mode);
                                        positive = positive.Replace(CurrentMode.ToString(), "");
                                        negative += CurrentMode.ToString();
                                    }
                                    else
                                    {
                                        Mode.Add(mode);
                                        negative = negative.Replace(CurrentMode.ToString(), "");
                                        positive += CurrentMode.ToString();
                                    }
                                    continue;
                                }
                            }
                        }
                    }
                }

                if (positive.Length > 1)
                {
                    channelModes += positive;
                }

                if (negative.Length > 1)
                {
                    channelModes += negative;
                }
            }
        }

        private void Format()
        {
            string modes = "+";
            if (Removing)
            {
                modes = "-";
            }
            string parameters = " ";
            int CurrentMode = 1;
            buffer = "";
            int CurrentLine = 1;
            int CurrentPm = 1;
            lock (Mode)
            {
                if (Mode.Count == 0)
                {
                    return;
                }
                foreach (SimpleMode xx in Mode)
                {
                    if (CurrentMode > ModesPerOneLine || CurrentPm > ParametersPerOneLine)
                    {
                        buffer += Prefix + modes + parameters + "\n";
                        CurrentLine++;
                        if (Removing)
                        {
                            modes = "-";
                        }
                        else
                        {
                            modes = "+";
                        }
                        parameters = " ";
                        CurrentMode = 1;
                        CurrentPm = 1;
                    }
                    modes += xx.Mode.ToString();
                    CurrentMode++;
                    if (xx.ContainsParameter)
                    {
                        parameters += xx.Parameter + " ";
                        CurrentPm++;
                    }
                }
                buffer += Prefix + modes + parameters + "\n";
            }
        }

        /// <summary>
        /// String
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            lock (Mode)
            {
                Format();
                return buffer;
            }
        }
    }
}
