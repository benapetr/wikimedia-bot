using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace wmib
{
    class config
    {
        public class channel
        {
            /// <summary>
            /// Channel name
            /// </summary>
            public string Name;
            public string Language;

            public bool FreshList = false;

            public bool Logged;


            /// <summary>
            /// Log
            /// </summary>
            public string Log;

            public bool Seen = false;
            public bool Feed;
            public bool Info;


            public bool EnableRss = false;

            public bool suppress;

            public List<string> Infobot_IgnoredNames = new List<string>();

            public int respond_wait = 120;

            public bool respond_message = false;

            public System.DateTime last_msg = System.DateTime.Now;

            public bool infobot_trim_white_space_in_name = true;

            /// <summary>
            /// Infobot help
            /// </summary>
            public bool infobot_help = false;

            /// <summary>
            /// Infobot sorted
            /// </summary>
            public bool infobot_sorted = false;

            /// <summary>
            /// Doesn't send any warnings on error
            /// </summary>
            public bool suppress_warnings = false;

            public bool logs_no_write_data = false;

            public bool statistics_enabled = false;

            /// <summary>
            /// Completion
            /// </summary>
            public bool infobot_auto_complete = false;

            /// <summary>
            /// Configuration text
            /// </summary>
            private string conf;

            public bool ignore_unknown = false;

            public string shared;

            public List<config.channel> sharedlink;

            /// <summary>
            /// Users
            /// </summary>
            public IRCTrust Users;

            /// <summary>
            /// Path of db
            /// </summary>
            public string keydb = "";
        }
    }
}
