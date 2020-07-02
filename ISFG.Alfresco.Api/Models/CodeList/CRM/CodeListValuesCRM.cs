using Newtonsoft.Json;

namespace ISFG.Alfresco.Api.Models.CodeList
{
    public class CodeListValuesCRM
    {
        #region Properties

        [JsonProperty("data")]
        public CodeListCaseSensitiveWithValues<CodeListValue> CodeList { get; set; }

        #endregion
    }
}
