using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.DataBox.Api.Interfaces;
using ISFG.DataBox.Api.Models;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models.EmailDatabox;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/databox")]
    public class DataboxController
    {
        #region Fields

        private readonly IDataBoxHttpClient _dataBoxHttpClient;
        private readonly IEmailDataBoxService _emailDataBoxService;

        #endregion

        #region Constructors

        public DataboxController(IEmailDataBoxService emailDataBoxService, IDataBoxHttpClient dataBoxHttpClient)
        {
            _emailDataBoxService = emailDataBoxService;
            _dataBoxHttpClient = dataBoxHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get available accounts in databox server
        /// </summary>
        [HttpGet("accounts")]
        public async Task<List<DataboxAccount>> Accounts()
        {
            return await _dataBoxHttpClient.Accounts();
        }

        /// <summary>
        /// Dont register provided databox message
        /// </summary>
        [HttpPost("{nodeId}/dont-register")]
        public async Task DontRegister([FromRoute] DontRegister parameters)
        {
            await _emailDataBoxService.DontRegister(parameters, ClientSide.Enums.EmailOrDataboxEnum.Databox);
        }

        /// <summary>
        /// Try to get new messages
        /// </summary>
        [HttpPost("refresh")]
        public async Task<int> Refresh()
        {
            return await _dataBoxHttpClient.Refresh();
        }

        /// <summary>
        /// Get information about refresh status
        /// </summary>
        [HttpGet("status")]
        public async Task<DataBoxStatusResponse> Status([FromQuery] int id)
        {
            return await _dataBoxHttpClient.Status(id);
        }

        #endregion
    }    
}
