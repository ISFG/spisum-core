using System;
using System.Collections.Generic;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.Alfresco.Api.Models.CoreApiFixed
{
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class CommentPagingFixed
    {
        #region Fields

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        #endregion

        #region Properties

        [Newtonsoft.Json.JsonProperty("list", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public List8Fixed List { get; set; } = new List8Fixed();

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }

        #endregion
    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class List8Fixed
    {
        #region Fields

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        #endregion

        #region Properties

        [Newtonsoft.Json.JsonProperty("entries", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public ICollection<CommentEntryFixed> Entries { get; set; } = new System.Collections.ObjectModel.Collection<CommentEntryFixed>();

        [Newtonsoft.Json.JsonProperty("pagination", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public Pagination Pagination { get; set; } = new Pagination();

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }

        #endregion
    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class CommentEntryFixed
    {
        #region Fields

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        #endregion

        #region Properties

        [Newtonsoft.Json.JsonProperty("entry", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public CommentFixed Entry { get; set; } = new CommentFixed();

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }

        #endregion
    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class CommentFixed
    {
        #region Fields

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        #endregion

        #region Properties

        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty("title")]
        public string Title { get; set; }

        [Newtonsoft.Json.JsonProperty("content", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Content { get; set; }

        [Newtonsoft.Json.JsonProperty("createdBy", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public Person CreatedBy { get; set; } = new Person();

        [Newtonsoft.Json.JsonProperty("createdAt", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public DateTimeOffset CreatedAt { get; set; }

        [Newtonsoft.Json.JsonProperty("edited", Required = Newtonsoft.Json.Required.Always)]
        public bool Edited { get; set; }

        [Newtonsoft.Json.JsonProperty("modifiedBy", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public Person ModifiedBy { get; set; } = new Person();

        [Newtonsoft.Json.JsonProperty("modifiedAt", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public DateTimeOffset ModifiedAt { get; set; }

        [Newtonsoft.Json.JsonProperty("canEdit", Required = Newtonsoft.Json.Required.Always)]
        public bool CanEdit { get; set; }

        [Newtonsoft.Json.JsonProperty("canDelete", Required = Newtonsoft.Json.Required.Always)]
        public bool CanDelete { get; set; }

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }

        #endregion
    }
}
