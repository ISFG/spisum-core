﻿using System;
 using System.IO;
 using System.Net.Mime;
 using System.Text;
 using System.Threading.Tasks;
 using ISFG.Alfresco.Api.Interfaces;
 using ISFG.Common.Extensions;
 using ISFG.Common.Interfaces;
 using ISFG.Exceptions.Exceptions;
 using ISFG.SpisUm.Attributes;
 using ISFG.SpisUm.ClientSide.Interfaces;
 using ISFG.SpisUm.ClientSide.Models;
 using ISFG.SpisUm.Endpoints;
 using ISFG.SpisUm.Interfaces;
 using Microsoft.AspNetCore.Mvc;
 using Microsoft.Extensions.Caching.Memory;

 namespace ISFG.SpisUm.Controllers.App.V1
{
    [ApiVersion("1")]
    [ApiController]
    [AlfrescoAuthentication]
    [Route(EndpointsUrl.ApiRoute + "/signer-app")]
    public class SignerAppController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IApiConfiguration _apiConfiguration;
        private readonly ISignerService _signerService;
        private readonly ISimpleMemoryCache _simpleMemoryCache;

        #endregion

        #region Constructors

        public SignerAppController(
            ISimpleMemoryCache simpleMemoryCache,
            IApiConfiguration apiConfiguration,
            ISignerService signerService, 
            IAlfrescoHttpClient alfrescoHttpClient)
        {
            _simpleMemoryCache = simpleMemoryCache;
            _apiConfiguration = apiConfiguration;
            _signerService = signerService;
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Download a file
        /// </summary>
        [HttpGet("download")]
        public async Task<Stream> DownloadFile([FromQuery] string token, [FromQuery] string requestGroup, [FromQuery] string componentId)
        {
            var fileContent = await _alfrescoHttpClient.NodeContent(componentId);

            return new MemoryStream(fileContent.File);
        }

        /// <summary>
        /// Generate a batch
        /// </summary>
        [HttpGet("batch")]
        public async Task<FileResult> GenerateBatch([FromQuery] string token, [FromQuery] string requestGroup, [FromQuery] bool visual, [FromQuery] string documentId, [FromQuery] string[] componentId)
        {
            componentId.ForEach(x =>
            {
                if (x.Split('_').Length != 2)
                    throw new BadRequestException($"Component {componentId} is not in form 'guid_componentId'");
            });
            
            var createBatch = await _signerService.GenerateBatch(_apiConfiguration.Url, documentId, componentId, visual);

           return File(new MemoryStream(Encoding.ASCII.GetBytes(createBatch)), MediaTypeNames.Text.Xml, "batch.xml");
        }

        /// <summary>
        /// Saves a status
        /// </summary>
        [HttpPost("status")]
        public async Task Status([FromQuery] string componentId)
        {
            await using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
 
            _simpleMemoryCache.Create($"{MemoryCacheNames.SignerStatus}{componentId}", Encoding.Default.GetString(memoryStream.ToArray()), new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(15)
            });
        }

        /// <summary>
        /// Upload a new file
        /// </summary>
        [HttpPost("upload")]
        public async Task UploadFile([FromQuery] string token, [FromQuery] string requestGroup, [FromQuery] string documentId, [FromQuery] string componentId, [FromQuery] bool visual)
        {
            await using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            
            await _signerService.UploadFile(documentId, componentId, memoryStream.ToArray(), visual);
        }

        #endregion
    }
}   