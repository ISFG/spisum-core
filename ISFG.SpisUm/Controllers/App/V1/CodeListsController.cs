using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Models.V1;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/codelists")]
    public class CodeListsController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly string _cacheKey = nameof(CodeListsController);
        private readonly INodesService _nodesService;
        private readonly ISimpleMemoryCache _simpleMemoryCache;

        #endregion

        #region Constructors

        public CodeListsController(IAlfrescoConfiguration alfrescoConfiguration, IAlfrescoHttpClient alfrescoHttpClient, ISimpleMemoryCache simpleMemoryCache, INodesService nodesService)
        {
            _alfrescoConfig = alfrescoConfiguration;
            _alfrescoHttpClient = alfrescoHttpClient;
            _simpleMemoryCache = simpleMemoryCache;
            _nodesService = nodesService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets all Lists of Values and their values that exists.
        /// </summary>
        /// <returns>List of all List of Values</returns>
        [HttpGet("all")]
        public async Task<List<CodeListModel>> GetAllListsOfValues()
        {
            if (_simpleMemoryCache.IsExist(_cacheKey))
                return _simpleMemoryCache.Get<List<CodeListModel>>(_cacheKey);

            var codeLists = new List<CodeListModel>();

            var alfrescoResponse = await _alfrescoHttpClient.CodeListGetAll();

            foreach (var list in alfrescoResponse.CodeLists.Where(x => x.Name != "rmc_smList"))
                codeLists.Add(await GetListValues(list.Name));

            _simpleMemoryCache.Create(_cacheKey, codeLists);

            return codeLists;
        }

        /// <summary>
        ///     Get's values of specified List of Values
        /// </summary>
        /// <param name="listname">List name (not title) you want values from</param>
        /// <returns>List with values</returns>
        [HttpGet("{listname}/values")]
        public async Task<CodeListModel> GetListValues([FromRoute] string listname)
        {
            var alfrescoResponse = await _alfrescoHttpClient.CodeListGetWithValues(listname);

            return new CodeListModel
            {
                Name = alfrescoResponse.CodeList.Name,
                Title = alfrescoResponse.CodeList.Title,
                Values = alfrescoResponse.CodeList.Values?.Select(x => x.ValueName)?.OrderBy(x => x)?.ToList()
            };
        }

        /// <summary>
        /// Gets all shredding plans
        /// </summary>
        [HttpGet("shredding-plans")]
        public List<ShreddingPlanModel> GetShreddingPlans()
        {
            return _nodesService.GetShreddingPlans();
        }

        #endregion
    }
}