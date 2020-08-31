using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ISFG.SpisUm.Dictionaries
{
    public static class Languages
    {
        #region Fields

        private static readonly Dictionary<string, Language> LanguagesList = new Dictionary<string, Language>
        {
            { "cs-CZ", GetCzechLanguage() }
        };

        #endregion

        #region Properties

        public static Language DefaultLanguage => GetCzechLanguage();

        #endregion

        #region Static Methods

        public static bool IsLanguageAvailable(string language)
        {
            return LanguagesList.ContainsKey(language);
        }

        /// <summary>
        /// Gets language from list of available languages.
        /// </summary>
        /// <param name="language">Key for search</param>
        /// <returns>Language if available otherwise default language.</returns>
        public static Language GetLanguageFromAvailableLanguages(string language)
        {
            if (language == null)
                return DefaultLanguage;

            return LanguagesList.GetValueOrDefault(language) ?? DefaultLanguage;
        }

        private static Language GetCzechLanguage()
        {
            return new Language
            {
                Code = "cs-CZ",
                Name = "Czech",
                Translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
                    Path.Combine(AppContext.BaseDirectory, "Dictionaries", "SpisUm.cs-CZ.json"), Encoding.Default)
                )
            };
        }

        #endregion

        #region Nested Types, Enums, Delegates

        public class Language
        {
            #region Properties

            public string Code { get; set; }
            public string Name { get; set; }
            public Dictionary<string, string> Translations { get; set; }

            #endregion
        }

        #endregion
    }
}