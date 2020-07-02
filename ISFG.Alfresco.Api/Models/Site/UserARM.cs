using System.Collections.Generic;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.Alfresco.Api.Models.Sites
{
    public class UserARM
    {
        #region Properties

        public PersonBodyCreate Body { get; set; }
        public List<string> Groups { get; set; }
        public string MainGroup { get; set; }

        #endregion
    }
}
