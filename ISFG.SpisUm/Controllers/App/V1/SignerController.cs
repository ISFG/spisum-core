using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using ISFG.SpisUm.ClientSide.Validators;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [ApiVersion("1")]
    [AlfrescoAuthentication]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/signer")]
    public class SignerController : ControllerBase
    {
        #region Fields

        private const string SignerStatus = "signerStatus_";
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IApiConfiguration _apiConfiguration;
        private readonly ISignerService _signerService;
        private readonly ISimpleMemoryCache _simpleMemoryCache;

        #endregion

        #region Constructors

        public SignerController(
            ISimpleMemoryCache simpleMemoryCache,
            IApiConfiguration apiConfiguration, 
            IAlfrescoHttpClient alfrescoHttpClient,
            ISignerService signerService
        )
        {
            _simpleMemoryCache = simpleMemoryCache;
            _apiConfiguration = apiConfiguration;
            _alfrescoHttpClient = alfrescoHttpClient;
            _signerService = signerService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Create XML for signer
        /// </summary>
        [HttpGet("create")]
        public async Task<SignerCreateResponse> Create([FromQuery] SignerCreate signerCreate)
        {
            var componentValidator = new ComponentValidator(_alfrescoHttpClient);
            await signerCreate.ComponentId.ForEachAsync(async x => await componentValidator.ValidateAsync(new DocumentProperties(x)));
            
            return await _signerService.CreateXml(_apiConfiguration.Url, signerCreate.DocumentId, signerCreate.ComponentId, signerCreate.Visual);
        }

        /// <summary>
        /// Check status of component
        /// </summary>
        [HttpGet("status")]
        public async Task<List<SignerStatus>> Status([FromQuery] SignerGetStatus signerStatus)
        {
            var componentValidator = new ComponentValidator(_alfrescoHttpClient);
            await signerStatus.ComponentId.ForEachAsync(async x =>
            {
                var component = x.Split('_');
                if (component.Length != 2)
                    throw new BadRequestException($"Component {x} is not in form 'guid_componentId'");
                    
                //await componentValidator.ValidateAsync(new DocumentProperties(component[1]));
            });
            
            return (from component in signerStatus.ComponentId 
                where _simpleMemoryCache.IsExist($"{SignerStatus}{component}") 
                select new SignerStatus { Id = component.Split('_')[0] ,Component = component.Split('_')[1], Status = _simpleMemoryCache.Get<string>($"{SignerStatus}{component}")}).ToList();
        }

        #endregion
    }
}   