using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Pdf.Interfaces;
using ISFG.Signer.Client.Generated.Signer;
using ISFG.Signer.Client.Interfaces;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class ValidationService : IValidationService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IComponentService _componentService;

        private readonly string[] _fileExtensions =
{
            ".docx",
            ".docm",
            ".dotx",
            ".dotm",
            ".doc",
            ".dot",
            ".pdf",
            ".txt",
            ".png",
            ".eml",
            ".xlsx",
            ".xls"
        };

        private readonly IIdentityUser _identityUser;
        private readonly IPdfService _pdfService;
        private readonly ISignerClient _signerClient;
        private readonly ISignerConfiguration _signerConfiguration;
        private readonly ISignerService _signerService;

        #endregion

        #region Constructors

        public ValidationService(IAlfrescoHttpClient alfrescoHttpClient, ISignerService signerService, IPdfService pdfService, IComponentService componentService, ISignerClient signerClient, IIdentityUser identityUser, ISignerConfiguration signerConfiguration)
        {
            _signerService = signerService;
            _pdfService = pdfService;
            _componentService = componentService;
            _signerClient = signerClient;
            _identityUser = identityUser;
            _alfrescoHttpClient = alfrescoHttpClient;
            _signerConfiguration = signerConfiguration;
        }

        #endregion

        #region Implementation of IValidationService

        public async Task<NodeEntry> ConvertToOutputFormat(string documentId, string componentId, string reason, string organization)
        {
            var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(componentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties}, {AlfrescoNames.Includes.Path}", ParameterType.QueryString)));

            var extension = Path.GetExtension(nodeEntry?.Entry?.Name);

            if (!_fileExtensions.Any(x => extension.Contains(x)))
                return await _alfrescoHttpClient.UpdateNode(componentId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, "impossible"));

            var properties = nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
            string pid = properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString();
            
            var componentPid = pid.Split('/');
            
            FormDataParam pdf = null;

            if (nodeEntry?.Entry?.Content.MimeType != MediaTypeNames.Application.Pdf)
                pdf = await _alfrescoHttpClient.GetThumbnailPdf(componentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter("c", "force", ParameterType.QueryString)));
            else
                pdf = await _alfrescoHttpClient.NodeContent(componentId);
            
            var data = await _pdfService.ConvertToPdfA2B(new MemoryStream(pdf.File));

            if (_signerConfiguration.Base != null || _signerConfiguration.Url != null)
            {                
                SealResponse signer = await _signerClient.Seal(data);
                await _signerService.CheckAndUpdateComponent(componentId, signer.Output);
                data = signer.Output;
            }            

            await _componentService.UploadNewVersionComponent(documentId, componentId, data,
                Path.ChangeExtension(properties.GetNestedValueOrDefault(SpisumNames.Properties.FileName)?.ToString(), ".pdf"), MediaTypeNames.Application.Pdf);

            return await _alfrescoHttpClient.UpdateNode(componentId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, "yes")
                .AddProperty(SpisumNames.Properties.FinalVersion, true)
                .AddProperty(SpisumNames.Properties.SettleReason, reason)
                .AddProperty(SpisumNames.Properties.KeepForm, SpisumNames.KeepForm.Original_InOutputFormat)
                .AddProperty(SpisumNames.Properties.LinkRendering, int.Parse(componentPid[1]) + 1)
                .AddProperty(SpisumNames.Properties.ListOriginalComponent, int.Parse(componentPid[1]))
                .AddProperty(SpisumNames.Properties.CompanyImplementingDataFormat, organization)
                .AddProperty(SpisumNames.Properties.AuthorChangeOfDataFormat, $"{_identityUser.FirstName} {_identityUser.LastName}")
                .AddProperty(SpisumNames.Properties.OriginalDataFormat, nodeEntry?.Entry?.Content?.MimeType)
                .AddProperty(SpisumNames.Properties.ImprintFile, Hashes.Sha256CheckSum(new MemoryStream(data)))
                .AddProperty(SpisumNames.Properties.DataCompleteVerificationItem, DateTime.Now)
                .AddProperty(SpisumNames.Properties.UsedAlgorithm, "SHA-256"));
        }

        public async Task<NodeEntry> CheckOutputFormat(string nodeId)
        {
            var nodeEntry = await _alfrescoHttpClient.NodeContent(nodeId);
            return await CheckOutputFormat(nodeId, nodeEntry?.File, nodeEntry?.ContentType);
        }

        public async Task<NodeEntry> CheckOutputFormat(string nodeId, IFormFile componentFile) =>
            await CheckOutputFormat(nodeId, await componentFile.GetBytes(), componentFile.ContentType);

        public async Task<NodeEntry> CheckOutputFormat(string nodeId, byte[] file, string mimeType)
        {
            List<string> allowedExtensions = new List<string>
            {
                "application/pdf",  // PDF
                "image/jpeg",       // JPEG/JPG
                "image/pjpeg",      // JFIF
                "image/png",        // PNG
                "image/tiff",       // TIF/TIF
                "video/mpeg",       // MPEG ???
                "audio/mpeg",       // MPEG
                "application/xml"   // XML
            };

            if (allowedExtensions.Any(x => mimeType.Contains(x)))
            {
                if (mimeType == "application/pdf")
                {
                    await _signerService.CheckAndUpdateComponent(nodeId, file);
                    return await UpdateOutputFormatProperties(nodeId, await _pdfService.IsPdfA2B(new MemoryStream(file)));
                }

                return await UpdateOutputFormatProperties(nodeId, true);
            }

            return await UpdateOutputFormatProperties(nodeId, false);
        }

        #endregion

        #region Private Methods

        private async Task<NodeEntry> UpdateOutputFormatProperties(string nodeId, bool valid)
        {
            if (valid)
                return await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, "yes")
                    .AddProperty(SpisumNames.Properties.FinalVersion, true)
                    .AddProperty(SpisumNames.Properties.KeepForm, SpisumNames.KeepForm.Original_InOutputFormat)
                );
            return await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, "no")
                .AddProperty(SpisumNames.Properties.FinalVersion, false)
                .AddProperty(SpisumNames.Properties.KeepForm, SpisumNames.KeepForm.Original)
            );
        }

        #endregion
    }
}
