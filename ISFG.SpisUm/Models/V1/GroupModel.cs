using System.Collections.Generic;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.SpisUm.Models.V1
{
    public class AllGroupModel
    {
        #region Properties

        public IEnumerable<GroupMember> Dispatch { get; set; }
        public IEnumerable<GroupMember> Main { get; set; }
        public IEnumerable<GroupMember> Repository { get; set; }

        #endregion
    }

    public class GroupModel
    {
        #region Properties

        public GroupBodyCreate Body { get; set; }
        public bool? IsDispatch { get; set; }
        public bool? IsRepository { get; set; }

        #endregion
    }
}
