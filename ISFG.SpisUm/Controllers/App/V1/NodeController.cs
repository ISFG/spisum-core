using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/node")]
    public class NodeController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public NodeController(IAlfrescoHttpClient alfrescoHttpClient)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get's childrens of the node
        /// </summary>
        [HttpGet("{nodeId}/children")]
        public async Task<NodeChildAssociationPaging> GetChildren([FromRoute] string nodeId, [FromQuery] BasicNodeQueryParamsWithRelativePath queryParams)
        {
            return await _alfrescoHttpClient.GetNodeChildren(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        /// <summary>
        /// Get's secondary childrens of the node
        /// </summary>
        [HttpGet("{nodeId}/secondary-children")]
        public async Task<NodeChildAssociationPagingFixed> GetSecondaryChildren([FromRoute] string nodeId, [FromQuery] BasicNodeQueryParams queryParams)
        {
            return await _alfrescoHttpClient.GetNodeSecondaryChildren(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        /// <summary>
        /// Get's node versions
        /// </summary>
        [HttpGet("{nodeId}/versions")]
        public async Task<VersionPaging> GetVersions([FromRoute] string nodeId, [FromQuery] AdvancedBasicQueryParams queryParams)
        {
            return await _alfrescoHttpClient.GetVersions(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        #endregion
    }
}