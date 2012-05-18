using System;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
    class messages
    {
        public static string Language = "en";
        class container
        {
            public string language;
            public Dictionary<string, string> Cache;
            container(string LanguageCode)
            {
                language = LanguageCode;
                Cache = new Dictionary<string, string>();
            }
        }

        public static Dictionary<string, container> data = new Dictionary<string, container>();

        private static string parse(string text, string name)
        {
            if (text.Contains(name))
            {
                string x = text;
                x = text.Substring(text.IndexOf(name + "=")).Replace(name + "=", "");
                x = x.Substring(0, x.IndexOf(";"));
                return x;
            }
            return "";
        }

        public static bool exist(string lang)
        {
            switch (lang)
            { 
                case "en":
                case "cs":
                    return true;
            }
            return false;
        }

        public static string get(string item, string language = null)
        {
            if (language == null)
            {
                language = Language;
            }
            if (!data.ContainsKey(language))
            {
                return "invalid language: " + language;
            }
            if (data[language].Cache.ContainsKey(item))
            {
                return data[language].Cache[item];
            }
            string text;
            switch (language)
            { 
                case "en":
                        text = wmib.Properties.Resources.english;
                        break;
                case "cs":
                        text = wmib.Properties.Resources.czech;
                        break;
                default:
                        return "invalid language: " + language;
            }
            string value = parse(text, item);
            if (value == "")
            {
                if (Language != language)
                {
                    return (get(item));
                } else
                {
                    return "error: requested item was not found in dictionary";
                }
            }
            
            data[language].Cache.Add(item, value);
            return value;
        }
    }
}
