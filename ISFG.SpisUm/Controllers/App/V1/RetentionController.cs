using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models.Shredding;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/retention")]
    public class RetentionController : ControllerBase
    {
        #region Fields

        private readonly IShreddingService _shreddingService;

        #endregion

        #region Constructors

        public RetentionController(IShreddingService shreddingService)
        {
            _shreddingService = shreddingService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Create a shredding proposal
        /// </summary>
        [HttpPost("shredding-proposal/create")]
        public async Task<NodeEntry> Cancel([FromBody] ShreddingProposalCreate input)
        {
            return await _shreddingService.ShreddingProposalCreate(input.Name, input.Ids);
        }

        #endregion
    }
}
