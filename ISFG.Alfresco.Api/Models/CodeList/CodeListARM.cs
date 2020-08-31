using System.Collections.Generic;
using Newtonsoft.Json;

namespace ISFG.Alfresco.Api.Models.CodeList
{
    #region Create
    public class CodeListCreateARM : CodeListCaseSensitiveWithValues<CodeListValue>
    {}

    #endregion

    #region Update
    public class CodeListUpdateValues : CodeListCaseSensitiveWithValues<CodeListValue>
    {}
    public class CodeListUpdateValuesAthoritiesARM : CodeListCaseSensitiveWithValues<CodeListValueAuthority>
    {}
    public class CodeListUpdateARM : CodeListWithValues<CodeListValue>
    {}
    public class CodeListUpdateValuesARM
    {
        #region Properties

        [JsonProperty("data")]
        private CodeListUpdateValues ResponseData { get; set; } = new CodeListUpdateValues();

        #endregion
    }
    #endregion

    #region Get
    public class CodeListAllARM
    {
        #region Properties

        [JsonProperty("data")]
        public List<CodeList> CodeLists { get; set; }

        #endregion
    }
    public class CodeListValuesARM
    {
        #region Properties

        [JsonProperty("data")]
        public CodeListCaseSensitiveWithValues<CodeListValue> CodeList { get; set; }

        #endregion
    } 
    #endregion
}
