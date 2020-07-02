using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using Newtonsoft.Json;

namespace ISFG.Alfresco.Api.Models.CoreApiFixed
{
    public class GroupPagingFixed
    {
        #region Properties

        [JsonProperty("list", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public GroupPagingList List { get; set; }

        #endregion
    }
    
    public class GroupPagingList
    {
        #region Properties

        [JsonProperty("entries", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<GroupEntryFixed> Entries { get; set; }

        [JsonProperty("pagination", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public Pagination Pagination { get; set; }

        #endregion
    }
    
    public class GroupEntryFixed 
    {
        #region Properties

        [JsonProperty("entry", Required = Required.Always)]
        [Required]
        public GroupFixed Entry { get; set; } = new GroupFixed();

        #endregion
    }

    public class GroupFixed 
    {
        #region Properties

        [JsonProperty("id", Required = Required.Always)]
        [Required(AllowEmptyStrings = true)]
        public string Id { get; set; }

        [JsonProperty("displayName")]
        [Required(AllowEmptyStrings = true)]
        public string DisplayName { get; set; }

        [JsonProperty("isRoot", Required = Required.Always)]
        public bool IsRoot { get; set; } = true;

        [JsonProperty("parentIds", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<string> ParentIds { get; set; }

        [JsonProperty("zones", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<string> Zones { get; set; }

        #endregion
    }
}