using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Data.Models;
using ISFG.Signer.Client.Generated.Signer;
using ISFG.Signer.Client.Interfaces;
using ISFG.Signer.Client.Models;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Signer;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class SignerService : ISignerService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuditLogService _auditLogService;
        private readonly IComponentService _componentService;
        private readonly IIdentityUser _identityUser;
        private readonly INodesService _nodesService;
        private readonly ISignerClient _signerClient;

        #endregion

        #region Constructors

        public SignerService(
            ISignerClient signerClient,
            IIdentityUser identityUser,
            IAuditLogService auditLogService,
            IAlfrescoHttpClient alfrescoHttpClient,
            INodesService nodesService, 
            IComponentService componentService)
        {
            _signerClient = signerClient;
            _identityUser = identityUser;
            _auditLogService = auditLogService;
            _alfrescoHttpClient = alfrescoHttpClient;
            _nodesService = nodesService;
            _componentService = componentService;
        }

        #endregion

        #region Implementation of ISignerService

        public Task<SignerCreateResponse> CreateXml(string baseUrl, string documentId, string[] componentId, bool visual)
        {
            if (componentId.Length == 1)
            {
                var compGuid = IdGenerator.ShortGuid();
                var componentCallSigner = $"{SpisumNames.Signer.CallSigner}:{BuildSignXml(baseUrl, documentId, componentId[0], compGuid, visual).ToString().ToBase64()}";
                
                return Task.FromResult(new SignerCreateResponse(componentCallSigner, null, $"{compGuid}_{componentId[0]}"));
            }

            var componentsWithGuid = new List<string>();
            var batchGuid = $"{IdGenerator.ShortGuid()}_{Guid.NewGuid().ToString()}";
            
            var statusUrl = $"{BuildEndpointUrl(baseUrl, "status")}&componentId={batchGuid}";
            var inputUrl = componentId.Aggregate($"{BuildEndpointUrl(baseUrl, "batch")}&documentId={documentId}&visual={visual.ToLowerString()}", (current, component) =>
            {
                var guidComponent = $"{IdGenerator.ShortGuid()}_{component}";
                componentsWithGuid.Add(guidComponent);

                return current + $"&componentId={guidComponent}";
            });

            var componentsCallSigner = $"{SpisumNames.Signer.CallSigner}:{GenerateBatchXml(inputUrl, statusUrl).ToString().ToBase64()}";
            
            return Task.FromResult(new SignerCreateResponse(componentsCallSigner, batchGuid, componentsWithGuid.ToArray()));
        }
        
        public Task<string> GenerateBatch(string baseUrl, string documentId, string[] componentId, bool visual) => Task.FromResult(
            new XElement(SpisumNames.Signer.Batch,
                from component in componentId
                select BuildSignXml(baseUrl, documentId, component.Split('_')[1], component.Split('_')[0], visual)).ToString()
            );

        public async Task<bool> CheckAndUpdateComponent(string componentId, byte[] component)
        {
            try
            {
                var pdfValidation = await _signerClient.Validate(component);
                var certValidation = await _signerClient.ValidateCertificate(pdfValidation?.Report?.sigInfos?.LastOrDefault()?.signCert?.Data);
            
                await _alfrescoHttpClient.UpdateNode(componentId, GetSignerProperties(pdfValidation, certValidation, false));
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task UploadFile(string documentId, string componentId, byte[] newComponent, bool visual)
        {
            var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(componentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties}, {AlfrescoNames.Includes.Path}", ParameterType.QueryString)));
            var properties = nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
            
            var pdfValidation = await _signerClient.Validate(newComponent);
            var certValidation = await _signerClient.ValidateCertificate(pdfValidation?.Report?.sigInfos?.LastOrDefault()?.signCert?.Data);

            if (await _nodesService.IsNodeLocked(documentId))
                await _alfrescoHttpClient.NodeUnlock(documentId);
            
            if (await _nodesService.IsNodeLocked(componentId))
                await _alfrescoHttpClient.NodeUnlock(componentId);            

            await _componentService.UploadNewVersionComponent(documentId, componentId, newComponent, 
                Path.ChangeExtension(properties.GetNestedValueOrDefault(SpisumNames.Properties.FileName)?.ToString(), ".pdf"), MediaTypeNames.Application.Pdf);
            var node = await _alfrescoHttpClient.UpdateNode(componentId, GetSignerProperties(pdfValidation, certValidation, true));
            
            await _alfrescoHttpClient.NodeLock(documentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)); 
            await _alfrescoHttpClient.NodeLock(componentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)); 

            try
            {
                var componentPid = nodeEntry?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.PripojeniPodpisu,
                        visual ? TransactinoHistoryMessages.DocumentSignComponentWithVisual : TransactinoHistoryMessages.DocumentSignComponentWithoutVisual);

                // Audit log for a file
                var documentFileParent = await _alfrescoHttpClient.GetNodeParents(documentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                var fileId = documentFileParent?.List?.Entries?.FirstOrDefault()?.Entry?.Id;
                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.PripojeniPodpisu,
                        visual ? TransactinoHistoryMessages.DocumentSignComponentWithVisual : TransactinoHistoryMessages.DocumentSignComponentWithoutVisual);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        #endregion

        #region Private Methods
        
        private XElement BuildSignXml(string baseUrl, string documentId, string componentId, string componentGuid, bool visualSign)
        {
            var inputUrl = $"{BuildEndpointUrl(baseUrl, "download")}&componentId={componentId}";
            var statusUrl = $"{BuildEndpointUrl(baseUrl, "status")}&componentId={componentGuid}_{componentId}";
            var outputUrl = $"{BuildEndpointUrl(baseUrl, "upload")}&componentId={componentId}&documentId={documentId}&visual={visualSign.ToLowerString()}";

            return GenerateXml(inputUrl, statusUrl, outputUrl, visualSign);
        }

        private XElement GenerateBatchXml(string inputUrl, string statusUrl) =>
            new XElement(SpisumNames.Signer.External, XAttributeType(SpisumNames.Signer.Batch),
                new XElement(SpisumNames.Signer.Input, XAttributeType(SpisumNames.Signer.Url), inputUrl.ToBase64()),
                new XElement(SpisumNames.Signer.Status, XAttributeType(SpisumNames.Signer.Url), statusUrl.ToBase64())
            );
        
        private XElement GenerateXml(string inputUrl, string statusUrl, string outputUrl, bool visualSign) =>
            new XElement(SpisumNames.Signer.External, XAttributeType(visualSign ? SpisumNames.Signer.Visual : SpisumNames.Signer.Sign),
                new XElement(SpisumNames.Signer.Input, XAttributeType(SpisumNames.Signer.Url), inputUrl.ToBase64()),
                new XElement(SpisumNames.Signer.Status, XAttributeType(SpisumNames.Signer.Url), statusUrl.ToBase64()),
                new XElement(SpisumNames.Signer.Output, XAttributeType(SpisumNames.Signer.Url), outputUrl.ToBase64())
            );

        private XAttribute XAttributeType(string type) => new XAttribute(SpisumNames.Signer.Type, type);
        
        private string BuildEndpointUrl(string baseUrl, string endpoint) => $"{baseUrl}api/app/v1/signer-app/{endpoint}?token={_identityUser?.Token}&requestGroup={_identityUser?.RequestGroup}";

        private NodeBodyUpdate GetSignerProperties(ValidateResponse pdfValidation, ValidateCertificateResponse certValidation, bool isSigned)
        {
            var signInfo = pdfValidation?.Report?.sigInfos?.LastOrDefault();
            var publisher = Dn.Parse(signInfo?.signCert?.Issuer);
            var holder = Dn.Parse(signInfo?.signCert?.Subject);
            var verifier = Dn.Parse(GetVerifier(pdfValidation?.XMLReport));
            
            return new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.FileIsSigned, isSigned)
                .AddProperty(SpisumNames.Properties.SafetyElementsCheck, true)
                .AddProperty(SpisumNames.Properties.UsedTime, pdfValidation?.Report?.CreationDateTime)
                .AddProperty(SpisumNames.Properties.VerificationTime, pdfValidation?.Report?.validationProperties?.ValidationTime)
                .AddProperty(SpisumNames.Properties.ValiditySafetyElement, pdfValidation?.Report?.globalStatus switch
                {
                    SignerNames.Ok => SpisumNames.Signer.Valid, 
                    SignerNames.Warning => SpisumNames.Signer.ValidityAssessed,
                    SignerNames.Error => SpisumNames.Signer.NotValid, 
                    _ => null
                })
                .AddProperty(SpisumNames.Properties.AssessmentMoment, signInfo?.DecisiveMoment)
                .AddProperty(SpisumNames.Properties.SignLocation, string.IsNullOrEmpty(signInfo?.Location) ? null : signInfo?.Location)
                .AddProperty(SpisumNames.Properties.SignReason, string.IsNullOrEmpty(signInfo?.Reason) ? null : signInfo?.Reason)
                .AddProperty(SpisumNames.Properties.ValiditySafetyCert, pdfValidation?.Report?.globalStatus switch
                {
                    SignerNames.Ok => SpisumNames.Signer.Valid,
                    SignerNames.Warning => SpisumNames.Signer.ValidityAssessed,
                    SignerNames.Error => SpisumNames.Signer.NotValid,
                    _ => null
                })
                .AddProperty(SpisumNames.Properties.RevocationState, certValidation?.certificateValidationInfo?.statusIndication == SignerNames.Revoked)
                .AddProperty(SpisumNames.Properties.ValiditySafetyCertRevocation, null)
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
                .AddProperty(SpisumNames.Properties.SerialNumber, signInfo?.signCert?.Serial)
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
                .AddProperty(SpisumNames.Properties.ValidityFrom, signInfo?.signCert?.NotBefore)
                .AddProperty(SpisumNames.Properties.ValidityTo, signInfo?.signCert?.NotAfter)
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

        #endregion
    }
}   