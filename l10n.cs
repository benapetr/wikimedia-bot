//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System.Collections.Generic;

namespace wmib
{
    /// <summary>
    /// Languages
    /// </summary>
    public class messages
    {
        /// <summary>
        /// Default
        /// </summary>
        public static string Language = "en";

        /// <summary>
        /// Container for language data
        /// </summary>
        public class container
        {
            /// <summary>
            /// Name of language
            /// </summary>
            public string language;

            /// <summary>
            /// Data
            /// </summary>
            public Dictionary<string, string> Cache;

            /// <summary>
            /// Creates a new language data
            /// </summary>
            /// <param name="LanguageCode"></param>
            public container(string LanguageCode)
            {
                language = LanguageCode;
                Cache = new Dictionary<string, string>();
            }
        }

        private static readonly Dictionary<string, container> data = new Dictionary<string, container>();

        /// <summary>
        /// Load all language data
        /// </summary>
        public static void LoadLD()
        {
            data.Add("cs", new container("cs"));
            data.Add("en", new container("en"));
            data.Add("zh", new container("zh"));
            data.Add("de", new container("de"));
            data.Add("ko", new container("ko"));
        }

        private static string Parse(string text, string name)
        {
            if (text.Contains(name + "="))
            {
                string x = text.Substring(text.IndexOf(name + "=")).Replace(name + "=", "");
                x = x.Substring(0, x.IndexOf(";"));
                return x;
            }
            return "";
        }

        /// <summary>
        /// Return true if a language exist
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static bool Exists(string lang)
        {
            if (!data.ContainsKey(lang))
            {
                return false;
            }
            return true;
        }

        private static string finalize(string text, List<string> va)
        {
            string Text = text;
            int position = 0;
            if (va == null)
            {
                return text;
            }
            foreach (string part in va)
            {
                position++;
                Text = Text.Replace("$" + position.ToString(), part);
            }
            return Text;
        }

        /// <summary>
        /// Return a language string for a given key
        /// </summary>
        /// <param name="item">Key</param>
        /// <param name="language">Language</param>
        /// <param name="va"></param>
        /// <returns></returns>
        public static string Localize(string item, string language = null, List<string> va = null)
        {
            if (language == null)
            {
                language = Language;
            }
            if (!data.ContainsKey(language))
            {
                return "error - invalid language: " + language;
            }
            if (data[language].Cache.ContainsKey(item))
            {
                return finalize(data[language].Cache[item], va);
            }
            string text;
            switch (language)
            {
                case "en":
                    text = Properties.Resources.english;
                    break;
                case "cs":
                    text = Properties.Resources.cs_czech;
                    break;
                case "zh":
                    text = Properties.Resources.zh_chinese;
                    break;
                case "de":
                    text = Properties.Resources.de_german;
                    break;
                case "ko":
                    text = Properties.Resources.ko_korean;
                    break;
                default:
                    return "invalid language: " + language;
            }
            string value = Parse(text, item);
            if (string.IsNullOrEmpty(value))
            {
                if (Language != language)
                {
                    return Localize(item, null, va);
                }
                return "[" + item + "]";
            }

            data[language].Cache.Add(item, value);
            return finalize(value, va);
        }
    }
}
