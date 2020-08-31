using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Utils;
using ISFG.Email.Api.Interfaces;
using ISFG.Email.Api.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Email;
using ISFG.SpisUm.ClientSide.Models.EmailDatabox;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/email")]
    public class EmailController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IEmailDataBoxService _emailDataBoxService;
        private readonly IEmailHttpClient _emailHttpClient;
        private readonly INodesService _nodesService;

        #endregion

        #region Constructors

        public EmailController(
            IAlfrescoConfiguration alfrescoConfig,
            IAlfrescoHttpClient alfrescoHttpClient,
            INodesService nodesService,
            IEmailDataBoxService emailDataBoxService,
            IEmailHttpClient emailHttpClient
        )
        {
            _alfrescoConfig = alfrescoConfig;
            _alfrescoHttpClient = alfrescoHttpClient;
            _nodesService = nodesService;
            _emailDataBoxService = emailDataBoxService;
            _emailHttpClient = emailHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get all 
        /// </summary>
        [HttpGet("accounts")]
        public async Task<List<EmailAccount>> Accounts()
        {
            return await _emailHttpClient.Accounts();
        }

        /// <summary>
        /// Email mark as don't register
        /// </summary>
        [HttpPost("{nodeId}/dont-register")]
        public async Task DontRegister([FromRoute] DontRegister parameters)
        {
            await _emailDataBoxService.DontRegister(parameters, ClientSide.Enums.EmailOrDataboxEnum.Email);
        }

        /// <summary>
        /// Try to get new messages
        /// </summary>
        [HttpPost("refresh")]
        public async Task<int> Refresh()
        {
            return await _emailHttpClient.Refresh();
        }

        /// <summary>
        /// Mark email message as incomplete
        /// </summary>
        [HttpPost("{nodeId}/incomplete")]
        public async Task SendEmail([FromRoute] EmailSend parameters)
        {
            var emlNode = await _alfrescoHttpClient.GetNodeInfo(parameters.NodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            if (emlNode?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomEmail, StringComparison.OrdinalIgnoreCase) == false)
                throw new BadRequestException("", "Node is not in Mailroom");

            var emailProperties = emlNode.Entry.Properties.As<JObject>().ToDictionary();
            if (emailProperties.GetNestedValueOrDefault(SpisumNames.Properties.EmailSender) == null)
                throw new BadRequestException(ErrorCodes.V_EMAIL_NO_SENDER);

            if (emailProperties.GetNestedValueOrDefault(SpisumNames.Properties.EmailRecipient) == null)
                throw new BadRequestException(ErrorCodes.V_EMAIL_NO_RECIPIENT);

            string senderEmail = emailProperties.GetNestedValueOrDefault(SpisumNames.Properties.EmailSender).ToString();
            string recipientEmail = emailProperties.GetNestedValueOrDefault(SpisumNames.Properties.EmailRecipient).ToString();

            if (!EmailUtils.IsValidEmail(senderEmail))
                throw new BadRequestException(ErrorCodes.V_EMAIL_INVALID_SENDER);
            if (!EmailUtils.IsValidEmail(recipientEmail))
                throw new BadRequestException(ErrorCodes.V_EMAIL_INVALID_RECIPIENT);
            
            var emailConfiguration = (await _emailHttpClient.Accounts())?.FirstOrDefault(x => x?.Username?.ToLower() == recipientEmail?.ToLower());
            if (emailConfiguration == null) throw new BadRequestException(ErrorCodes.V_EMAIL_NO_CONFIGURATION);

            if (emlNode?.Entry?.Path?.Name?.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomEmailUnprocessed) != true)
                throw new BadRequestException("", "Node is not in expected path");

            await _emailHttpClient.Send(senderEmail, emailConfiguration.Username, parameters.Subject, parameters.Body, parameters.Files);
           
            // Move eml and children
            await _nodesService.MoveChildrenByPath(emlNode.Entry.Id, SpisumNames.Paths.MailRoomEmailNotRegistered);

            await _alfrescoHttpClient.UpdateNode(parameters.NodeId, new NodeBodyUpdate
                {
                    Properties = new Dictionary<string, object>
                    {
                        { SpisumNames.Properties.DigitalDeliveryNotRegisteredReasion, "EM_VAL_01" }
                    }
                }
            );
        }

        /// <summary>
        /// Get information about refresh status
        /// </summary>
        [HttpGet("status")]
        public async Task<EmailStatusResponse> Status([FromQuery] int id)
        {
            return await _emailHttpClient.Status(id);
        }

        #endregion
    }
}
