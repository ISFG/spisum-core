using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.Site;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.SpisUm.Models.V1;

namespace ISFG.SpisUm.Interfaces
{
    public interface IInitialSiteService
    {
        #region Public Methods

        Task CreateRMShreddingPlan(List<ShreddingPlanItemModel> items);
        Task CreateSiteRmAndFolders(SitePaging sites, RMSiteARM configSiteRM, List<GroupModel> configGroups);
        Task CreateSitesAndFolders(SitePaging sites, List<SiteARM> configSites, List<GroupModel> configGroups);
        Task CheckCreatePermissions(string nodeId, List<Permission> permissions);
        Task CheckSiteChilds<T, U>(bool isRecordManagement, List<ChildrenARM<T, U>> childs, bool groupStructure, string guid, List<GroupModel> configGroups);

        #endregion
    }
}
