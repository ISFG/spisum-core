﻿using System;
 using System.IO;
 using System.Net.Mime;
 using System.Text;
 using System.Threading.Tasks;
 using ISFG.Common.Interfaces;
 using ISFG.SpisUm.ClientSide.Interfaces;
 using ISFG.SpisUm.Endpoints;
 using ISFG.SpisUm.Interfaces;
 using Microsoft.AspNetCore.Mvc;
 using Microsoft.Extensions.Caching.Memory;

 namespace ISFG.SpisUm.Controllers.App.V1
{
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/signer-app")]
    public class SignerAppController : ControllerBase
    {
        #region Fields

        private const string SignerStatus = "signerStatus_";
        private readonly IApiConfiguration _apiConfiguration;
        private readonly ISignerService _signerService;
        private readonly ISimpleMemoryCache _simpleMemoryCache;

        #endregion

        #region Constructors

        public SignerAppController(
            ISimpleMemoryCache simpleMemoryCache,
            IApiConfiguration apiConfiguration,
            ISignerService signerService
        )
        {
            _simpleMemoryCache = simpleMemoryCache;
            _apiConfiguration = apiConfiguration;
            _signerService = signerService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Download a file
        /// </summary>
        [HttpGet("download")]
        public async Task<Stream> DownloadFile([FromQuery] string token, [FromQuery] string componentId)
        {
            var file = await _signerService.DownloadFile(token, componentId);

            return new MemoryStream(file);
        }

        /// <summary>
        /// Generate a batch
        /// </summary>
        [HttpGet("batch")]
        public async Task<FileResult> GenerateBatch([FromQuery] string token, [FromQuery] bool visual, [FromQuery] string documentId, [FromQuery] string[] componentId)
        {
            var createBatch = await _signerService.GenerateBatch(_apiConfiguration.Url, token, documentId, componentId, visual);

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
 
            _simpleMemoryCache.Create($"{SignerStatus}{componentId}", Encoding.Default.GetString(memoryStream.ToArray()), new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(15)
            });
        }

        /// <summary>
        /// Upload a new file
        /// </summary>
        [HttpPost("upload")]
        public async Task UploadFile([FromQuery] string token, [FromQuery] string documentId, [FromQuery] string componentId)
        {
            await using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            
            await _signerService.UploadFile(token, documentId, componentId, memoryStream.ToArray());
        }

        #endregion
    }
}   