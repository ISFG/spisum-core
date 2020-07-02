using ISFG.Alfresco.Api.Models.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.Alfresco.Api.Models.CoreApiFixed
{
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class NodeChildAssociationPagingFixed : NodeChildAssociationPaging, IListPagination<List19Fixed, NodeChildAssociationEntry>
    {
        #region Implementation of IListPagination<List19Fixed,NodeChildAssociationEntry>

        [Newtonsoft.Json.JsonProperty("list", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public new List19Fixed List { get; set; } = new List19Fixed();

        #endregion
    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class List19Fixed : List19, IAlfrescoList<NodeChildAssociationEntry>
    {}
}
