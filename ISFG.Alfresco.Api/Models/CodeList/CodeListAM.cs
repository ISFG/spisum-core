using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISFG.Alfresco.Api.Models.CodeList
{
    public class CodeListCreateAM
    {
        #region Properties

        [JsonProperty("constraintName")]
        public string Name { get; set; }

        [JsonProperty("constraintTitle")]
        public string Title { get; set; }

        [JsonProperty("allowedValues")]
        public string[] AllowedValues { get; set; }

        #endregion
    }
    public class CodeListUpdateAM
    {
        #region Properties

        [JsonProperty("constraintTitle")]
        public string Title { get; set; }

        #endregion
    }
    public class CodeListValueWithAuthority
    {
        #region Properties

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("authorities")]
        public string[] Authorities { get; set; }

        #endregion
    }
    public class CodeListUpdateValuesAuthorityAM
    {
        #region Properties

        [JsonProperty("values")]
        public List<CodeListValueWithAuthority> Values { get; set; } = new List<CodeListValueWithAuthority>();

        #endregion
    }
    public class CodeListUpdateValuesAM
    {
        #region Properties

        [JsonProperty("allowedValues")]
        public List<string> AllowedValues { get; set; } = new List<string>();

        #endregion
    }
}
