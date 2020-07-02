using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AutoMapper;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Data.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.Pdf.Interfaces;
using ISFG.Signer.Client.Generated.Signer;
using ISFG.Signer.Client.Interfaces;
using ISFG.Signer.Client.Models;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class SignerService : ISignerService
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfiguration;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuditLogService _auditLogService;
        private readonly IIdentityUser _identityUser;
        private readonly IMapper _mapper;
        private readonly IPdfService _pdfService;
        private readonly IPersonService _personService;
        private readonly IRepositoryService _repositoryService;
        private readonly ISignerClient _signerClient;
        private readonly ISimpleMemoryCache _simpleMemoryCache;
        private readonly ISystemLoginService _systemLoginService;
        private readonly ITransactionHistoryService _transactionHistoryService;

        #endregion

        #region Constructors

        public SignerService(
            ISignerClient signerClient,
            ISimpleMemoryCache simpleMemoryCache, 
            IAlfrescoConfiguration alfrescoConfiguration,
            IPersonService personService, 
            IIdentityUser identityUser, 
            IMapper mapper, 
            ITransactionHistoryService transactionHistoryService,
            IAuditLogService auditLogService,
            IRepositoryService repositoryService, 
            IAlfrescoHttpClient alfrescoHttpClient,
            IPdfService pdfService,
            ISystemLoginService systemLoginService
        )
        {
            _signerClient = signerClient;
            _simpleMemoryCache = simpleMemoryCache;
            _alfrescoConfiguration = alfrescoConfiguration;
            _personService = personService;
            _identityUser = identityUser;
            _mapper = mapper;
            _transactionHistoryService = transactionHistoryService;
            _auditLogService = auditLogService;
            _repositoryService = repositoryService;
            _alfrescoHttpClient = alfrescoHttpClient;
            _pdfService = pdfService;
            _systemLoginService = systemLoginService;
        }

        #endregion

        #region Implementation of ISignerService

        public Task<SignerCreateResponse> CreateXml(string baseUrl, string documentId, string[] componentId, bool visual)
        {
            if (componentId.Length == 1)
            {
                var compGuid = IdGenerator.ShortGuid();
                return Task.FromResult(new SignerCreateResponse($"call-signer:{BuildXml(baseUrl, _identityUser.Token, componentId[0],  documentId, compGuid, visual).ToBase64()}", null, $"{compGuid}_{componentId[0]}"));
            }

            var batchGuid = $"{IdGenerator.ShortGuid()}_{Guid.NewGuid().ToString()}";
            var batchUrl = $"{baseUrl}api/app/v1/signer-app/batch?token=" + _identityUser.Token + "&visual=" + visual.ToString().ToLower() + "&documentId=" + documentId;
            var components = new List<string>();
            
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<external type='batch'>");
            stringBuilder.Append("<input type='url'>");
            stringBuilder.Append(componentId.Aggregate(batchUrl, (current, component) =>
            {
                var guid = IdGenerator.ShortGuid(); 
                components.Add($"{guid}_{component}"); 
                
                return current + $"&componentId={guid}_{component}";
            }).ToBase64());
            stringBuilder.Append("</input>");
            stringBuilder.AppendLine($"<status type='url'>{$"{baseUrl}api/app/v1/signer-app/status?token={_identityUser?.Token}&componentId={batchGuid}".ToBase64()}</status>");
            stringBuilder.AppendLine("</external>");

            return Task.FromResult(new SignerCreateResponse($"call-signer:{stringBuilder.ToString().ToBase64()}", batchGuid, components.ToArray()));
        }

        public async Task<byte[]> DownloadFile(string token, string componentId)
        {
            var alfrescoClient = new AlfrescoHttpClient(_alfrescoConfiguration, new SignerAuthentification(_simpleMemoryCache, token));
            var fileContent = await alfrescoClient.NodeContent(componentId);

            return fileContent.File;
        }

        public Task<string> GenerateBatch(string baseUrl, string token, string documentId, string[] componentId, bool visual)
        {
            var stringBuilder = new StringBuilder();
            
            stringBuilder.AppendLine("<batch>");
            componentId.ForEach(x =>
            {
                var component = x.Split('_');
                if (component.Length != 2)
                    throw new BadRequestException($"Component {componentId} is not in form 'guid_componentId'");
                
                stringBuilder.Append(BuildXml(baseUrl, token, component[1], documentId, component[0], visual));
            });
            stringBuilder.AppendLine("</batch>");

            return Task.FromResult(stringBuilder.ToString());
        }

        public async Task<bool> CheckAndUpdateComponent(string componentId, byte[] component)
        {
            try
            {
                var pdfValidation = await _signerClient.Validate(component);
                var certValidation = await _signerClient.ValidateCertificate(pdfValidation?.Report?.sigInfos[0]?.signCert.Data);
            
                await _alfrescoHttpClient.UpdateNode(componentId, GetSignerProperties(pdfValidation, certValidation, false));
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task UploadFile(string token, string documentId, string componentId, byte[] newComponent)
        {
            var alfrescoClient = new AlfrescoHttpClient(_alfrescoConfiguration, new SignerAuthentification(_simpleMemoryCache, token));
            var nodeService = new NodesService(_alfrescoConfiguration, alfrescoClient, _auditLogService, _identityUser, _mapper, _simpleMemoryCache, _transactionHistoryService, _systemLoginService, _repositoryService);
            var componentService = new ComponentService(alfrescoClient, nodeService, _personService, _identityUser, _auditLogService);
            
            var nodeEntry = await alfrescoClient.GetNodeInfo(componentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties}, {AlfrescoNames.Includes.Path}", ParameterType.QueryString)));
            var properties = nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
            
            var pdfValidation = await _signerClient.Validate(newComponent);
            var certValidation = await _signerClient.ValidateCertificate(pdfValidation?.Report?.sigInfos[0]?.signCert.Data);

            await UnlockNode(alfrescoClient, documentId);
            await UnlockNode(alfrescoClient, componentId);

            await componentService.UploadNewVersionComponent(documentId, componentId, newComponent, 
                Path.ChangeExtension(properties.GetNestedValueOrDefault(SpisumNames.Properties.FileName)?.ToString(), ".pdf"), MediaTypeNames.Application.Pdf);
            var node = await alfrescoClient.UpdateNode(componentId, GetSignerProperties(pdfValidation, certValidation, true));
            
            await LockNode(alfrescoClient, documentId);
            await LockNode(alfrescoClient, componentId);

            try
            {
                var componentPid = nodeEntry?.GetPid();

                // Audit log for a document
                if (node?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.PripojeniPodpisu,
                        TransactinoHistoryMessages.DocumentSignComponent);

                // Audit log for a file
                var documentFileParent = await _alfrescoHttpClient.GetNodeParents(documentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                var fileId = documentFileParent?.List?.Entries?.FirstOrDefault()?.Entry?.Id;
                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.PripojeniPodpisu,
                        TransactinoHistoryMessages.DocumentSignComponent);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        #endregion

        #region Private Methods

        private string BuildXml(string baseUrl, string token, string componentId, string documentId, string componentGuid, bool visualSign)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(visualSign ? "<external type='visual'>" : "<external type='sign'>");
            stringBuilder.AppendLine($"<input type='url'>{$"{GetUrl("download")}&componentId={componentId}".ToBase64()}</input>");
            stringBuilder.AppendLine($"<status type='url'>{$"{GetUrl("status")}&componentId={componentGuid}_{componentId}".ToBase64()}</status>");
            stringBuilder.AppendLine($"<output type='url'>{$"{GetUrl("upload")}&componentId={componentId}&documentId={documentId}".ToBase64()}</output>");
            stringBuilder.AppendLine("</external>");
            
            return stringBuilder.ToString();

            string GetUrl(string endpoint) => $"{baseUrl}api/app/v1/signer-app/{endpoint}?token={_identityUser?.Token ?? token}";
        }

        private NodeBodyUpdate GetSignerProperties(ValidateResponse pdfValidation, ValidateCertificateResponse certValidation, bool isSigned)
        {
            var publisher = Dn.Parse(pdfValidation?.Report?.sigInfos[0]?.signCert?.Issuer);
            var holder = Dn.Parse(pdfValidation?.Report?.sigInfos[0]?.signCert?.Subject);
            var verifier = Dn.Parse(GetVerifier(pdfValidation?.XMLReport));
            
            return new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.FileIsSigned, isSigned)
                .AddProperty(SpisumNames.Properties.UsedTime, pdfValidation?.Report?.CreationDateTime)
                .AddProperty(SpisumNames.Properties.VerificationTime, pdfValidation?.Report?.validationProperties?.ValidationTime)
                .AddProperty(SpisumNames.Properties.ValiditySafetyElement, pdfValidation?.Report?.globalStatus switch
                {
                    SignerNames.Ok => SpisumNames.Signer.Valid, 
                    SignerNames.Warning => SpisumNames.Signer.ValidityAssessed,
                    SignerNames.Error => SpisumNames.Signer.NotValid, 
                    _ => null
                })
                .AddProperty(SpisumNames.Properties.AssessmentMoment, pdfValidation?.Report?.sigInfos[0]?.DecisiveMoment)
                .AddProperty(SpisumNames.Properties.ValiditySafetyCert, pdfValidation?.Report?.globalStatus switch
                {
                    SignerNames.Ok => SpisumNames.Signer.Valid,
                    SignerNames.Warning => SpisumNames.Signer.ValidityAssessed,
                    SignerNames.Error => SpisumNames.Signer.NotValid,
                    _ => null
                })
                .AddProperty(SpisumNames.Properties.RevocationState, certValidation?.certificateValidationInfo?.statusIndication == SignerNames.Revoked)
                .AddProperty(SpisumNames.Properties.ValiditySafetyCertRevocation, SpisumNames.Signer.Valid)
                .AddProperty(SpisumNames.Properties.CertValidityPath, certValidation?.certificateValidationInfo?.statusSubindication switch
                {
                    var x when x == SignerNames.Ok ||
                               x == SignerNames.Expired ||
                               x == SignerNames.Revoked => SpisumNames.Signer.Valid,
                    SignerNames.FormatFailure => SpisumNames.Signer.ValidityAssessed,
                    SignerNames.NoCertificateChainFound => SpisumNames.Signer.NotValid,
                    _ => null
                })
                .AddProperty(SpisumNames.Properties.CertValidity, certValidation?.certificateValidationInfo?.statusIndication switch
                {
                    SignerNames.Valid => SpisumNames.Signer.Valid,
                    SignerNames.Indeterminate => SpisumNames.Signer.ValidityAssessed,
                    SignerNames.Invalid => SpisumNames.Signer.NotValid,
                    _ => null
                })
                .AddProperty(SpisumNames.Properties.VerifierName, verifier.GetValue(SpisumNames.Dn.CommonName))
                .AddProperty(SpisumNames.Properties.VerifierOrgName, verifier.GetValue(SpisumNames.Dn.Organization))
                .AddProperty(SpisumNames.Properties.VerifierOrgUnit, verifier.GetValue(SpisumNames.Dn.OrganizationalUnit))
                .AddProperty(SpisumNames.Properties.VerifierOrgAddress, verifier.GetValue(SpisumNames.Dn.Locality))
                .AddProperty(SpisumNames.Properties.SerialNumber, pdfValidation?.Report?.sigInfos[0]?.signCert?.Serial)
                .AddProperty(SpisumNames.Properties.PublisherAddress, publisher.GetValue(SpisumNames.Dn.Locality))
                .AddProperty(SpisumNames.Properties.PublisherContact, publisher.GetValue(SpisumNames.Dn.Email))
                .AddProperty(SpisumNames.Properties.PublisherName, publisher.GetValue(SpisumNames.Dn.CommonName))
                .AddProperty(SpisumNames.Properties.PublisherOrgName, publisher.GetValue(SpisumNames.Dn.Organization))
                .AddProperty(SpisumNames.Properties.PublisherOrgUnit, publisher.GetValue(SpisumNames.Dn.OrganizationalUnit))
                .AddProperty(SpisumNames.Properties.HolderAddress, holder.GetValue(SpisumNames.Dn.Locality))
                .AddProperty(SpisumNames.Properties.HolderContact, holder.GetValue(SpisumNames.Dn.Email))
                .AddProperty(SpisumNames.Properties.HolderName, holder.GetValue(SpisumNames.Dn.CommonName))
                .AddProperty(SpisumNames.Properties.HolderOrgName, holder.GetValue(SpisumNames.Dn.Organization))
                .AddProperty(SpisumNames.Properties.HolderOrgUnit, holder.GetValue(SpisumNames.Dn.OrganizationalUnit))
                .AddProperty(SpisumNames.Properties.ValidityFrom, pdfValidation?.Report?.sigInfos[0]?.signCert?.NotBefore)
                .AddProperty(SpisumNames.Properties.ValidityTo, pdfValidation?.Report?.sigInfos[0]?.signCert?.NotAfter)
                .AddProperty(SpisumNames.Properties.QualifiedCertType, certValidation?.certificateValidationInfo?.qualifiedCertType)
                .AddProperty(SpisumNames.Properties.IsSign, certValidation?.certificateValidationInfo?.qualifiedCertType == SignerNames.ESign)
                .AddProperty(SpisumNames.Properties.IsSealed, certValidation?.certificateValidationInfo?.qualifiedCertType == SignerNames.ESeal)
                .AddProperty(SpisumNames.Properties.SecurityType, certValidation?.certificateValidationInfo?.certType switch
                {
                    SignerNames.Qualified => SpisumNames.Signer.Qualified,
                    SignerNames.Commercial => SpisumNames.Signer.Commercial,
                    SignerNames.InternalStorage => SpisumNames.Signer.InternalStorage,
                    SignerNames.Unknown => SpisumNames.Signer.Unknown,
                    _ => null
                });   
        }

        private string GetVerifier(byte[] xmlReport)
        {
            if (xmlReport == null) return string.Empty;
            
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(Encoding.UTF8.GetString(xmlReport));
            XmlNodeList subjectName = xDoc.GetElementsByTagName("ds:X509SubjectName");

            return subjectName.Count == 1 ? subjectName.Item(0).InnerText : string.Empty;
        }

        private async Task LockNode(AlfrescoHttpClient alfrescoClient, string nodeId)
        {
            await alfrescoClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));          
        }

        private async Task UnlockNode(AlfrescoHttpClient alfrescoClient, string nodeId)
        {
            var nodeInfo = await alfrescoClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));
            
            if (nodeInfo?.Entry?.IsLocked != null && nodeInfo.Entry.IsLocked == true)
                await alfrescoClient.NodeUnlock(nodeId);            
        }

        #endregion
    }
}   