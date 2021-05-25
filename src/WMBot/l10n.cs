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
using wmib.Properties;

namespace wmib
{
    /// <summary>
    /// Languages
    /// </summary>
    public class Localization
    {
        /// <summary>
        /// Default
        /// </summary>
        public static string Language = "en";

        /// <summary>
        /// Container for language data
        /// </summary>
        public class Container
        {
            /// <summary>
            /// Name of language
            /// </summary>
            public string Language;

            /// <summary>
            /// Data
            /// </summary>
            public Dictionary<string, string> Cache;

            /// <summary>
            /// Creates a new language data
            /// </summary>
            /// <param name="LanguageCode"></param>
            public Container(string LanguageCode)
            {
                Language = LanguageCode;
                Cache = new Dictionary<string, string>();
            }
        }

        private static readonly Dictionary<string, Container> data = new Dictionary<string, Container>();

        /// <summary>
        /// Load all language data
        /// </summary>
        public static void LoadLD()
        {
            data.Add("cs", new Container("cs"));
            data.Add("en", new Container("en"));
            data.Add("es", new Container("es"));
            data.Add("zh", new Container("zh"));
            data.Add("de", new Container("de"));
            data.Add("ko", new Container("ko"));
            data.Add("pt", new Container("pt"));
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
                Text = Text.Replace("$" + position, part);
            }
            return Text;
        }

        /// <summary>
        /// Return a language string for a given key
        /// </summary>
        /// <param name="item">Key</param>
        /// <param name="language">Language</param>
        /// <param name="va">List of params to use with localization strings that use them ($1, $2 ...)</param>
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
                    text = Resources.english;
                    break;
                case "es":
                    text = Resources.es_spanish;
                    break;
                case "cs":
                    text = Resources.cs_czech;
                    break;
                case "zh":
                    text = Resources.zh_chinese;
                    break;
                case "de":
                    text = Resources.de_german;
                    break;
                case "ko":
                    text = Resources.ko_korean;
                    break;
                case "pt":
                    text = Resources.pt_portugese;
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
