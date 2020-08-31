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

        public readonly List<string> _allowedExtensions = new List<string>
        {
            "application/pdf",  // PDF
            "image/jpeg",       // JPEG/JPG
            "image/pjpeg",      // JFIF
            "image/png",        // PNG
            "image/gif",        // GIF
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
            IAuditLogService auditLogService
           )
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
        public List<string> GetAllowedExtensions()
        {
            return _allowedExtensions;
        }
        public string[] GetFileExtensions()
        {
            return _fileExtensions;
        }
        public async Task<NodeEntry> ConvertToOutputFormat(string documentId, string componentId, string reason, string organization)
        {
            var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(componentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties}, {AlfrescoNames.Includes.Path}", ParameterType.QueryString)));

            var documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentId);
            
            var extension = Path.GetExtension(nodeEntry?.Entry?.Name)?.ToLower();

            if (!_fileExtensions.Any(x => extension != null && extension.Contains(x)))
                return await _alfrescoHttpClient.UpdateNode(componentId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Converted)
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

                await _componentService.UpdateComponent(componentId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Converted)
                    .AddProperty(SpisumNames.Properties.CanBeSigned, false), false);
            }

            var data = await _pdfService.ConvertToPdfA2B(new MemoryStream(pdfFile));

            if (_signerConfiguration.Base != null || _signerConfiguration.Url != null)
            {
                SealResponse signer = await _signerClient.Seal(data);
                if (isOwnDocument)
                    await _signerService.CheckAndUpdateComponent(componentId, signer.Output);
                data = signer.Output;
            }

            ImmutableList<Parameter> parameters = ImmutableList.Create<Parameter>();
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.CanBeSigned, true, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Yes, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.FinalVersion, true, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.SettleReason, reason, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.KeepForm, SpisumNames.KeepForm.Original_InOutputFormat, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.LinkRendering, int.Parse(componentPid[1]) + 1, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ListOriginalComponent, int.Parse(componentPid[1]), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.CompanyImplementingDataFormat, organization, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AuthorChangeOfDataFormat, $"{_identityUser.FirstName} {_identityUser.LastName}", ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.OriginalDataFormat, nodeEntry?.Entry?.Content?.MimeType, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ImprintFile, Hashes.Sha256CheckSum(new MemoryStream(data)), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.DataCompleteVerificationItem, DateTime.Now.ToAlfrescoDateTimeString(), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.UsedAlgorithm, SpisumNames.Global.Sha256, ParameterType.GetOrPost));

            if (!isOwnDocument)
            {

                if (_signerConfiguration.Base != null || _signerConfiguration.Url != null)
                {
                    var parametersSigner = await _signerService.CheckComponentParameters(data);
                    parameters = parameters.AddRange(parametersSigner);
                }

                newComponent = await _componentService.CreateVersionedComponent(documentId,
                    new FormDataParam(data, $"{IdGenerator.GenerateId()}.pdf"),
                    Path.ChangeExtension($"VF_{componentFilename}", ".pdf"),
                    parameters);

                await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, newComponent.GetPid(), NodeTypeCodes.Komponenta, EventCodes.VlozeniKDokumentu, string.Format(TransactinoHistoryMessages.DocumentComponentCreateDocument, documentEntry?.GetPid()));

                return newComponent;
            }
            else
            {
                return await _componentService.UploadNewVersionComponent(documentId, componentId, data,
                    Path.ChangeExtension(componentFilename, ".pdf"), MediaTypeNames.Application.Pdf, parameters);
            }
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
            if (_allowedExtensions.Any(mimeType.Contains))
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
        public async Task<Dictionary<string, object>> GetCheckOutputFormatProperties(byte[] file, string mimeType)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            if (_allowedExtensions.Any(mimeType.Contains))
            {
                if (mimeType.Contains(MediaTypeNames.Application.Pdf))
                {
                    var parametersSigner = await _signerService.CheckComponentProperties(file);
                    parameters = parametersSigner;

                    var isPdf2 = await _pdfService.IsPdfA2B(new MemoryStream(file));
                    var parametersOutputFormat = GetOutputFormatProperties(isPdf2, isPdf2);

                    foreach(var parameter in parametersOutputFormat)
                    {
                        parameters.Add(parameter.Key, parameter.Value);
                    }

                    return parameters;
                }

                return GetOutputFormatProperties(true, false);
            }

            return GetOutputFormatProperties(false, false);
        }

        public async Task<NodeEntry> UpdateDocumentOutputFormat(string documentId) =>
            await UpdateOutputFormat(documentId, SpisumNames.Associations.Components, SpisumNames.Properties.FileIsInOutputFormat);

        public async Task<NodeEntry> UpdateFileOutputFormat(string fileId) =>
            await UpdateOutputFormat(fileId, SpisumNames.Associations.Documents, SpisumNames.Properties.FileIsInOutputFormat);

        public async Task<NodeEntry> UpdateDocumentSecurityFeatures(string documentId) =>
            await UpdateSecurityFeatures(documentId, SpisumNames.Associations.Components, SpisumNames.Properties.SafetyElementsCheck);

        public async Task<NodeEntry> UpdateFileSecurityFeatures(string fileId) =>
            await UpdateSecurityFeatures(fileId, SpisumNames.Associations.Documents, SpisumNames.Properties.SafetyElementsCheck);
        #endregion

        #region Private Methods

        private async Task<List<string>> GetChildrenPropertyValues(string parentId, string assocType, string searchProperty)
        {
            var children = await _alfrescoHttpClient.GetNodeSecondaryChildren(parentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{assocType}')", ParameterType.QueryString)));

            return children?.List?.Entries?
                .Select(item => item?.Entry?.Properties?.As<JObject>()?
                    .ToDictionary()?
                    .GetNestedValueOrDefault(searchProperty)?
                    .ToString())
                .ToList();
        }

        private async Task<NodeEntry> UpdateSecurityFeatures(string parentId, string assocType, string property)
        {
            var componentsSecurityFeatures = await GetChildrenPropertyValues(parentId, assocType, property);
            var nodeBody = new NodeBodyUpdate();

            if (assocType == SpisumNames.Associations.Components)
            {
                if (componentsSecurityFeatures.Where(x => x != null).Any(bool.Parse))
                    nodeBody.AddProperty(property, true);
                else if (componentsSecurityFeatures.Any() && componentsSecurityFeatures.Where(x => x != null).All(x => bool.Parse(x) == false))
                    nodeBody.AddProperty(property, false);
                else
                    nodeBody.AddProperty(property, null);
            }

            if (assocType == SpisumNames.Associations.Documents)
            {
                if (componentsSecurityFeatures.Any() && componentsSecurityFeatures.Where(x => x != null).All(bool.Parse))
                    nodeBody.AddProperty(property, true);
                else if (componentsSecurityFeatures.Where(x => x != null).Any(x => bool.Parse(x) == false))
                    nodeBody.AddProperty(property, false);
                else
                    nodeBody.AddProperty(property, null);
            }

            return await _alfrescoHttpClient.UpdateNode(parentId, nodeBody);
        }

        private async Task<NodeEntry> UpdateOutputFormat(string parentId, string assocType, string property)
        {
            var componentsOutputFormats = await GetChildrenPropertyValues(parentId, assocType, property);
            var nodeBody = new NodeBodyUpdate();

            if (componentsOutputFormats.Any() && componentsOutputFormats.All(x => x ==SpisumNames.Global.Yes || x == SpisumNames.Global.Converted))
                nodeBody.AddProperty(property, SpisumNames.Global.Yes);
            else if (componentsOutputFormats.Any(x => x == SpisumNames.Global.No))
                nodeBody.AddProperty(property, SpisumNames.Global.No);
            else if (componentsOutputFormats.Any(x => (x == SpisumNames.Global.Impossible) && componentsOutputFormats.Count(x => x == SpisumNames.Global.No) == 0))
                nodeBody.AddProperty(property, SpisumNames.Global.Impossible);
            else
                nodeBody.AddProperty(property, null);

            return await _alfrescoHttpClient.UpdateNode(parentId, nodeBody);
        }

        private async Task<NodeEntry> UpdateOutputFormatProperties(string nodeId, bool valid, bool canBeSigned)
        {
            return await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate().AddProperties(GetOutputFormatProperties(valid, canBeSigned)));
        }
        private Dictionary<string, object> GetOutputFormatProperties(bool valid, bool canBeSigned)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            if (valid)
            {
                properties.Add(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Yes);
                properties.Add(SpisumNames.Properties.FinalVersion, true);
                properties.Add(SpisumNames.Properties.KeepForm, SpisumNames.KeepForm.Original_InOutputFormat);
                properties.Add(SpisumNames.Properties.CanBeSigned, canBeSigned);
            }
            else
            {
                properties.Add(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.No);
                properties.Add(SpisumNames.Properties.FinalVersion, false);
                properties.Add(SpisumNames.Properties.KeepForm, SpisumNames.KeepForm.Original);
                properties.Add(SpisumNames.Properties.CanBeSigned, canBeSigned);
            }

            return properties;
        }
        #endregion
    }
}
