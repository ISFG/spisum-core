using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Common.Interfaces;
using ISFG.Pdf.Interfaces;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Models.V1;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

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
        private readonly IPdfService _pdfService;
        private readonly ITransformService _transformService;

        #endregion

        #region Constructors

        public CodeListsController(
            IAlfrescoConfiguration alfrescoConfiguration,
            IAlfrescoHttpClient alfrescoHttpClient, 
            ISimpleMemoryCache simpleMemoryCache, 
            INodesService nodesService, 
            IPdfService pdfService, 
            ITransformService transformService)
        {
            _alfrescoConfig = alfrescoConfiguration;
            _alfrescoHttpClient = alfrescoHttpClient;
            _simpleMemoryCache = simpleMemoryCache;
            _nodesService = nodesService;
            _pdfService = pdfService;
            _transformService = transformService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets all Lists of Values and their values that exists.
        /// </summary>
        /// <returns>List of all List of Values</returns>
        [HttpGet("all")]
        public List<CodeListModel> GetAllListsOfValues()
        {
            return new List<CodeListModel>();
        }

        /// <summary>
        /// Gets all shredding plans
        /// </summary>
        [HttpGet("shredding-plans")]
        public List<ShreddingPlanModel> GetShreddingPlans()
        {
            return _nodesService.GetShreddingPlans();
        }
        
        [HttpGet("shredding-plan-print")]
        public async Task<FileContentResult> GetShreddingPlanPrint(string shreddingPlanId)
        {
            var transform = await _transformService.ShreddingPlan(shreddingPlanId);
            var pdf = await _pdfService.GenerateShreddingPlan(transform);
            
            return new FileContentResult(pdf.ToArray(), MediaTypeNames.Application.Pdf)
            {
                FileDownloadName = $"{shreddingPlanId}.pdf"
            };
        }        
 
        #endregion
    }
}