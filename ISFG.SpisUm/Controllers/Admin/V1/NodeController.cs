using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace ISFG.SpisUm.Controllers.Admin.V1
{
    [AlfrescoAdminAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.AdminRoute + "/node")]
    public class NodeController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly INodesService _nodesService;

        #endregion

        #region Constructors

        public NodeController(IAlfrescoHttpClient alfrescoHttpClient, INodesService nodesService)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _nodesService = nodesService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Download node
        /// </summary>
        /// <param name="nodesId">Node ids</param>
        /// <returns>File</returns>
        [HttpPost("download")]
        public async Task<FileContentResult> Download(List<string> nodesId)
        {
            return await _nodesService.Download(nodesId);
        }

        /// <summary>
        /// Get node children
        /// </summary>
        /// <param name="nodeId">Node id</param>
        /// <param name="queryParams">Parameters</param>
        /// <returns>Children list</returns>
        [HttpGet("{nodeId}/children")]
        public async Task<NodeChildAssociationPaging> GetChildren([FromRoute] string nodeId, [FromQuery] BasicNodeQueryParamsWithRelativePath queryParams)
        {
            return await _alfrescoHttpClient.GetNodeChildren(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        #endregion
    }
}