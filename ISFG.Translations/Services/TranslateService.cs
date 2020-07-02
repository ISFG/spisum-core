using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ISFG.Common.Interfaces;
using ISFG.Translations.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace ISFG.Translations.Services
{
    public class TranslateService : ITranslateService
    {
        #region Fields

        private static readonly string cacheKey = "translation_{0}";
        private readonly ISimpleMemoryCache _simpleMemoryCache;

        #endregion

        #region Constructors

        public TranslateService(ISimpleMemoryCache simpleMemoryCache) => 
            _simpleMemoryCache = simpleMemoryCache;

        #endregion

        #region Implementation of ITranslateService

        public async Task<string> Translate(string key)
        {
            if (_simpleMemoryCache.IsExist(string.Format(cacheKey, key)))
                return _simpleMemoryCache.Get<string>(string.Format(cacheKey, key));
            
            var translations = await GetTranslations(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations.json"));
            try
            {
                var value = translations[key];
                _simpleMemoryCache.Create(string.Format(cacheKey, key), value, new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.High
                });

                return value;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task<string> Translate(string key, string nodeType, string translationType) => Translate($"{nodeType}.{key}.{translationType}");

        #endregion

        #region Private Methods

        private Task<Dictionary<string, string>> GetTranslations(string filePath)
        {
            if (!File.Exists(filePath)) return Task.FromResult(new Dictionary<string, string>());
            
            Dictionary<string, string> customDictionary;
            using (StreamReader sr = new StreamReader(filePath)) //, Encoding.GetEncoding(28591) addEncoding
            {
                customDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());
            }

            return Task.FromResult(customDictionary);
        }

        #endregion
    }
}