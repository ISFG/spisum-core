using System;
using System.Collections.Generic;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.Alfresco.Api.Models.CoreApiFixed
{
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class PersonPagingFixed
    {
        #region Fields

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        #endregion

        #region Properties

        [Newtonsoft.Json.JsonProperty("list", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public PersonPagingList List { get; set; }

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }

        #endregion
    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class PersonPagingList
    {
        #region Fields

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        #endregion

        #region Properties

        [Newtonsoft.Json.JsonProperty("entries", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ICollection<PersonEntryFixed> Entries { get; set; }

        [Newtonsoft.Json.JsonProperty("pagination", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public Pagination Pagination { get; set; }

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }

        #endregion
    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class PersonEntryFixed
    {
        #region Fields

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        #endregion

        #region Properties

        [Newtonsoft.Json.JsonProperty("entry", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        public PersonFixed Entry { get; set; } = new PersonFixed();

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }

        #endregion
    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.1.7.0 (Newtonsoft.Json v12.0.0.0)")]
    public class PersonFixed
    {
        #region Fields

        private IDictionary<string, object> _additionalProperties = new Dictionary<string, object>();

        #endregion

        #region Properties

        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty("firstName", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string FirstName { get; set; }

        [Newtonsoft.Json.JsonProperty("lastName", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string LastName { get; set; }

        [Newtonsoft.Json.JsonProperty("displayName", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        [Newtonsoft.Json.JsonProperty("description", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Description { get; set; }

        [Newtonsoft.Json.JsonProperty("avatarId", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string AvatarId { get; set; }

        [Newtonsoft.Json.JsonProperty("email", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Email { get; set; }

        [Newtonsoft.Json.JsonProperty("skypeId", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string SkypeId { get; set; }

        [Newtonsoft.Json.JsonProperty("googleId", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string GoogleId { get; set; }

        [Newtonsoft.Json.JsonProperty("instantMessageId", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string InstantMessageId { get; set; }

        [Newtonsoft.Json.JsonProperty("jobTitle", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string JobTitle { get; set; }

        [Newtonsoft.Json.JsonProperty("location", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Location { get; set; }

        [Newtonsoft.Json.JsonProperty("company", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public Company Company { get; set; }

        [Newtonsoft.Json.JsonProperty("mobile", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Mobile { get; set; }

        [Newtonsoft.Json.JsonProperty("telephone", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Telephone { get; set; }

        [Newtonsoft.Json.JsonProperty("statusUpdatedAt", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public DateTimeOffset? StatusUpdatedAt { get; set; }

        [Newtonsoft.Json.JsonProperty("userStatus", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string UserStatus { get; set; }

        [Newtonsoft.Json.JsonProperty("enabled", Required = Newtonsoft.Json.Required.Always)]
        public bool Enabled { get; set; } = true;

        [Newtonsoft.Json.JsonProperty("emailNotificationsEnabled", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? EmailNotificationsEnabled { get; set; } = true;

        [Newtonsoft.Json.JsonProperty("aspectNames", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public ICollection<string> AspectNames { get; set; }

        [Newtonsoft.Json.JsonProperty("properties", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public object Properties { get; set; }

        [Newtonsoft.Json.JsonProperty("capabilities", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public object Capabilities { get; set; }

        [Newtonsoft.Json.JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties; }
            set { _additionalProperties = value; }
        }

        #endregion
    }
}
