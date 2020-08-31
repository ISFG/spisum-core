using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISFG.Alfresco.Api.Models.CodeList
{
    public class CodeListAllCRM
    {
        #region Properties

        [JsonProperty("data")]
        public List<CodeListCaseSensitiveWithValues<CodeListValue>> Values { get; set; } = new List<CodeListCaseSensitiveWithValues<CodeListValue>>();

        #endregion
    }
}
