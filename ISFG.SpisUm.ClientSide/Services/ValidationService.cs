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
using ISFG.Common.Wcf.Configurations;
using ISFG.Data.Models;
using ISFG.Pdf.Interfaces;
using ISFG.Signer.Client.Generated.Signer;
using ISFG.Signer.Client.Interfaces;
using ISFG.Signer.Client.Services;
using ISFG.SpisUm.ClientSide.Extensions;
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

        private readonly List<string> _allowedExtensions = new List<string>
        {
            "application/pdf",  // PDF
            "image/jpeg",       // JPEG/JPG
            "image/pjpeg",      // JFIF
            "image/png",        // PNG
            "image/tiff",       // TIF/TIF
            "video/mpeg",       // MPEG ???
            "audio/mpeg",       // MPEG
            "application/xml",   // XML
            "text/xml"
        };

        private readonly IAuditLogService _auditLogService;
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
        private readonly ITransformService _transformService;

        #endregion

        #region Constructors

        public ValidationService(
            IAlfrescoHttpClient alfrescoHttpClient, 
            ISignerService signerService, 
            IPdfService pdfService, 
            IComponentService componentService,
            IIdentityUser identityUser, 
            ISignerConfiguration signerConfiguration, 
            ITransformService transformService, 
            IAuditLogService auditLogService)
        {
            _signerService = signerService;
            _pdfService = pdfService;
            _componentService = componentService;
            _signerClient = new SignerBaseClient(new NoAuthenticationChannelConfig<TSPWebServiceSoap>(signerConfiguration.Base.Uri));
            _identityUser = identityUser;
            _alfrescoHttpClient = alfrescoHttpClient;
            _signerConfiguration = signerConfiguration;
            _transformService = transformService;
            _auditLogService = auditLogService;
        }

        #endregion

        #region Implementation of IValidationService

        public async Task<NodeEntry> ConvertToOutputFormat(string documentId, string componentId, string reason, string organization)
        {
            var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(componentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties}, {AlfrescoNames.Includes.Path}", ParameterType.QueryString)));

            var documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentId);
            
            var extension = Path.GetExtension(nodeEntry?.Entry?.Name);

            if (!_fileExtensions.Any(x => extension.Contains(x)))
                return await _alfrescoHttpClient.UpdateNode(componentId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Impossible)
                    .AddProperty(SpisumNames.Properties.SafetyElementsCheck, false)
                    .AddProperty(SpisumNames.Properties.CanBeSigned, false));

            var documentProperties = documentEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
            var componentProperties = nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
            var pid = componentProperties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString();
            var componentFilename = componentProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileName)?.ToString();
            var isOwnDocument = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.SenderType)?.ToString() == SpisumNames.SenderType.Own;
            
            var componentPid = pid.Split('/');
            
            FormDataParam pdf = null;

            if (nodeEntry?.Entry?.Content.MimeType != MediaTypeNames.Application.Pdf)
                pdf = await _alfrescoHttpClient.GetThumbnailPdf(componentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Query.C, AlfrescoNames.Query.Force, ParameterType.QueryString)));
            else
                pdf = await _alfrescoHttpClient.NodeContent(componentId);

            byte[] pdfFile = pdf.File;
            NodeEntry newComponent = null;

            if (!isOwnDocument)
            {
                var clause = await _transformService.Clause(nodeEntry, pdfFile);
                pdfFile = await _pdfService.AddClause(new MemoryStream(pdfFile), clause);
                
                await _signerService.CheckAndUpdateComponent(componentId, pdfFile);
                
                await _alfrescoHttpClient.UpdateNode(componentId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Impossible)
                    .AddProperty(SpisumNames.Properties.CanBeSigned, false));  
    
                newComponent = await _componentService.CreateVersionedComponent(documentId,
                    new FormFile(new MemoryStream(pdfFile), 0, pdfFile.Length, nodeEntry?.Entry?.Name, $"VF_{componentFilename}"));
                
                await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, newComponent.GetPid(), NodeTypeCodes.Komponenta, EventCodes.VlozeniKDokumentu, string.Format(TransactinoHistoryMessages.DocumentComponentCreateDocument, documentEntry?.GetPid()));
            }

            var data = await _pdfService.ConvertToPdfA2B(new MemoryStream(pdfFile));

            if (_signerConfiguration.Base != null || _signerConfiguration.Url != null)
            {                
                SealResponse signer = await _signerClient.Seal(data);
                await _signerService.CheckAndUpdateComponent(isOwnDocument ? componentId : newComponent?.Entry?.Id, signer.Output);
                data = signer.Output;
            }            

            await _componentService.UploadNewVersionComponent(documentId, isOwnDocument ? componentId : newComponent?.Entry?.Id, data, 
                Path.ChangeExtension(isOwnDocument ? componentFilename : $"VF_{componentFilename}", ".pdf"), MediaTypeNames.Application.Pdf);

            return await _alfrescoHttpClient.UpdateNode(isOwnDocument ? componentId : newComponent?.Entry?.Id, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.CanBeSigned, true)
                .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Yes)
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
                .AddProperty(SpisumNames.Properties.UsedAlgorithm, SpisumNames.Global.Sha256));
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
            if (_allowedExtensions.Any(x => mimeType.Contains(x)))
            {
                if (mimeType.Contains(MediaTypeNames.Application.Pdf))
                {
                    await _signerService.CheckAndUpdateComponent(nodeId, file);
                    var isPdf2 = await _pdfService.IsPdfA2B(new MemoryStream(file));
                    return await UpdateOutputFormatProperties(nodeId, isPdf2, isPdf2);
                }

                return await UpdateOutputFormatProperties(nodeId, true, false);
            }

            return await UpdateOutputFormatProperties(nodeId, false, false);
        }


        public async Task<NodeEntry> UpdateDocumentOutputFormat(string documentId) => 
            await UpdateOutputFormat(documentId, SpisumNames.Associations.Components, SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Properties.FileIsInOutputFormatDocument);

        public async Task<NodeEntry> UpdateFileOutputFormat(string fileId) => 
            await UpdateOutputFormat(fileId, SpisumNames.Associations.Documents, SpisumNames.Properties.FileIsInOutputFormatDocument, SpisumNames.Properties.FileIsInOutputFormatFile);

        #endregion

        #region Private Methods

        private async Task<NodeEntry> UpdateOutputFormat(string parentId, string assocType, string searchProperty, string updateProperty)
        {
            var children = await _alfrescoHttpClient.GetNodeSecondaryChildren(parentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{assocType}')", ParameterType.QueryString)));

            if (children?.List?.Entries == null)
                return null;

            var componentsOutputFormats = children.List.Entries
                .Select(item => item?.Entry?.Properties?.As<JObject>()?.ToDictionary()?.GetNestedValueOrDefault(searchProperty)?.ToString()).ToList();

            var nodeBody = new NodeBodyUpdate();

            if (componentsOutputFormats.All(x => x == SpisumNames.Global.Yes))
                nodeBody.AddProperty(updateProperty, SpisumNames.Global.Yes);
            if (componentsOutputFormats.Any(x => x == SpisumNames.Global.No))
                nodeBody.AddProperty(updateProperty, SpisumNames.Global.No);
            if (componentsOutputFormats.Any(x => x == SpisumNames.Global.Impossible) && componentsOutputFormats.Count(x => x == SpisumNames.Global.No) == 0)
                nodeBody.AddProperty(updateProperty, SpisumNames.Global.Impossible);

            return await _alfrescoHttpClient.UpdateNode(parentId, nodeBody);
        }

        private async Task<NodeEntry> UpdateOutputFormatProperties(string nodeId, bool valid, bool canBeSigned)
        {
            if (valid)
                return await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Yes)
                    .AddProperty(SpisumNames.Properties.FinalVersion, true)
                    .AddProperty(SpisumNames.Properties.KeepForm, SpisumNames.KeepForm.Original_InOutputFormat)
                    .AddProperty(SpisumNames.Properties.CanBeSigned, canBeSigned));
            
            return await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.No)
                .AddProperty(SpisumNames.Properties.FinalVersion, false)
                .AddProperty(SpisumNames.Properties.KeepForm, SpisumNames.KeepForm.Original)
                .AddProperty(SpisumNames.Properties.CanBeSigned, canBeSigned));
        }

        #endregion
    }
}
