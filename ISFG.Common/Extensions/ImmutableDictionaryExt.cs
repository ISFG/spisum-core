using System.Collections.Immutable;

namespace ISFG.Common.Extensions
{
    public static class ImmutableDictionaryExt
    {
        #region Static Methods

        public static ImmutableDictionary<TKey, TValue> AddIfNotNull<TKey, TValue>(this ImmutableDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            return key == null || value == null ? dict : dict.Add(key, value);
        }

        #endregion
    }
}