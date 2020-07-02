using System.Collections.Generic;
using System.Linq;

namespace ISFG.Common.Extensions
{
    public static class DictionaryExt
    {
        #region Static Methods

        public static TValue GetNestedValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, params TKey[] strArray)
        {
            if (dict == null || strArray == null || !strArray.Any())
                return default;
            
            object dictValue = dict;

            foreach (var str in strArray)
            {
                var localDict = dictValue.As<Dictionary<TKey, TValue>>();
                if (localDict?.Keys == null)
                    return default;

                dictValue = localDict.FirstOrDefault(x => x.Key.Equals(str)).Value;
            }

            return (TValue)dictValue;
        }

        public static void AddIfNotNull<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (key != null && value != null)
                dict.Add(key, value);
        }

        public static TV GetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default) => 
            dict.TryGetValue(key, out TV value) ? value : defaultValue;

        #endregion
    }
}