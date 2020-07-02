using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.Alfresco.Api.Models.CoreApi
{
    public interface IAlfrescoList<T>
    {
        #region Properties

        public System.Collections.Generic.ICollection<T> Entries { get; set; }
        public Pagination Pagination { get; set; }
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties { get; set; }

        #endregion
    }

    public interface IListPagination<T, U> where T : IAlfrescoList<U>
    {
        #region Properties

        public T List { get; set; }
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties { get; set; }

        #endregion
    }
}
