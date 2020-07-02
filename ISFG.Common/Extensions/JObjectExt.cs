using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ISFG.Common.Extensions
{
    public static class JObjectExt
    {
        #region Static Methods

        public static Dictionary<string, object> ToDictionary(this JObject jObject)
        {
            var result = jObject.ToObject<Dictionary<string, object>>();

            var jObjectKeys = (from r in result
                let key = r.Key
                let value = r.Value
                where value.GetType() == typeof(JObject)
                select key).ToList();

            var jArrayKeys = (from r in result
                let key = r.Key
                let value = r.Value
                where value.GetType() == typeof(JArray)
                select key).ToList();

            jArrayKeys.ForEach(key =>
            {
                if (result[key] is JArray)
                    result[key] = (JArray) result[key];
                else
                    result[key] = ((JArray)result[key]).Values().Select(x => ((JValue)x).Value).ToArray();
            });
            jObjectKeys.ForEach(key => result[key] = ToDictionary(result[key] as JObject));

            return result;
        }

        #endregion
    }
}