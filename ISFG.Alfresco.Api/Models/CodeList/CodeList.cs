using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISFG.Alfresco.Api.Models.CodeList
{
    public class CodeListCaseSensitive : CodeList
    {
        #region Properties

        [JsonProperty("caseSensitive")]
        public bool IsCaseSensitive { get; set; }

        #endregion
    }
    public class CodeList
    {
        #region Properties

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("constraintName")]
        public string Name { get; set; }

        [JsonProperty("constraintTitle")]
        public string Title { get; set; }

        #endregion
    }
    public class CodeListAll
    {
        #region Properties

        [JsonProperty("data")]
        public List<CodeListCaseSensitiveWithValues<CodeListValue>> Values { get; set; } = new List<CodeListCaseSensitiveWithValues<CodeListValue>>();

        #endregion
    }
    public class CodeListAuthority
    {
        #region Properties

        [JsonProperty("authorityName")]
        public string AuthorityName { get; set; }

        [JsonProperty("authorityTitle")]
        public string AuthorityTitle { get; set; }

        #endregion
    }
    public class CodeListCaseSensitiveWithValues<T> : CodeListCaseSensitive where T : class
    {
        #region Properties

        [JsonProperty("values")]
        public List<T> Values { get; set; } = new List<T>();

        #endregion
    }
    public class CodeListWithValues<T> : CodeList where T : class
    {
        #region Properties

        [JsonProperty("values")]
        public List<T> Values { get; set; } = new List<T>();

        #endregion
    }
    public class CodeListValue
    {
        #region Properties

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("valueName")]
        public string ValueName { get; set; }

        [JsonProperty("valueTitle")]
        public string ValueTitle { get; set; }

        #endregion
    }
    public class CodeListValueAuthority : CodeListValue
    {
        #region Properties

        [JsonProperty("authorities")]
        public List<CodeListAuthority> Authorities { get; set; } = new List<CodeListAuthority>();

        #endregion
    }
  
}