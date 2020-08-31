using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ISFG.Common.Extensions
{
    public static class ObjectExt
    {
        #region Static Methods

        public static Dictionary<string, object> TryConvertJObjectToDictionary(this object jobject)
        {
            if (jobject == null || !(jobject is JObject))
                return null;

             return ((JObject)jobject).ToObject<Dictionary<string, object>>();
        }

        public static T TryGetValueFromProperties<T>(this object jobject, string key)
        {
            return (T)jobject.TryConvertJObjectToDictionary().GetValueOrDefault(key);
        }

        public static T As<T>(this object o) where T : class
        {
            return o as T;
        }

        #endregion
    }
}
