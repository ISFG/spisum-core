using System.Collections.Generic;
using System.Globalization;

namespace ISFG.SpisUm.Dictionaries
{
    public static class Localization
    {
        #region Static Methods

        /// <summary>
        /// Try get value from resource file that is associated with the provided language.
        /// </summary>
        /// <param name="key">Column "Name" or "Key" (depends on view) in the language resource file.</param>        
        /// <returns></returns>
        public static string Translate(string key)
        {            
            return Languages.GetLanguageFromAvailableLanguages(CultureInfo.CurrentCulture.Name).Translations.GetValueOrDefault(key);
        }

        #endregion
    }
}
