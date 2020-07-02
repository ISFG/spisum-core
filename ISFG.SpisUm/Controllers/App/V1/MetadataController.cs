using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/metadata")]
    public class MetadataController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public MetadataController(IAlfrescoHttpClient alfrescoHttpClient)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get metadata of provided node
        /// </summary>
        [HttpGet("{nodeId}")]
        public async Task<Dictionary<string, object>> GetMetadata([FromRoute] string nodeId)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            return metadataMapper(nodeInfo);
        }

        #endregion

        #region Private Methods

        private string getKeepFormType(string type)
        {
            if (type == "concept")
                return "koncept";
            if (type == "original")
                return "originál";
            if (type == "original_inOutputFormat")
                return "originál ve výstupním datovém formátu";
            return null;
        }

        private string getShipmentType(string type)
        {
            if (type == SpisumNames.NodeTypes.ShipmentDatabox)
                return "databox";
            if (type == SpisumNames.NodeTypes.ShipmentEmail)
                return "email";
            if (type == SpisumNames.NodeTypes.ShipmentPersonally)
                return "personally";
            if (type == SpisumNames.NodeTypes.ShipmentPost)
                return "post";
            if (type == SpisumNames.NodeTypes.ShipmentPublish)
                return "publish";
            return null;
        }

        private Dictionary<string, object> metadataMapper(NodeEntry nodeInfo)
        {
            var nodeEntry = nodeInfo?.Entry;
            var properties = nodeEntry?.Properties;
            if (nodeEntry == null || properties == null)
                return new Dictionary<string, object>();

            var propertiesDictionary = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            return new Dictionary<string, object>
            {
                { "Autor", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Author) },
                { "CasOvereni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.VerificationTime) },
                { "CasPouziti", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.UsedTime) },
                { "CisloSeznamuCRL", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.CrlNumber) },
                { "CisloJednaci", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Ssid) },
                { "CjOdesilatel", nodeEntry.NodeType == SpisumNames.NodeTypes.Document ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderSSID) : null },
                { "CjOdesilatelSpis", nodeEntry.NodeType == SpisumNames.NodeTypes.File ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderSSID) : null },
                { "DatumCasPredaniDoSpisovny", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ToRepositoryDate) },
                { "DatumCasPredaniDoArchivuNeboSkartace", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ToArchiveShreddingDate) },
                { "DatumCasUzavreni", nodeEntry.NodeType == SpisumNames.NodeTypes.File ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ClosureDate) : null },
                { "DatumCasVyrizeni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SettleDate) },
                { "DatumCasVytvoreni", nodeEntry.CreatedAt },
                { "DatumEvidenceSpisu", nodeEntry.NodeType == SpisumNames.NodeTypes.File ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.CreatedDate) : null },
                { "DatumDoruceni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DeliveryDate) },
                { "DatumPredaniPodpisu", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ForSignatureDate) },
                { "DatumStorna", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.CancelDate) },
                { "DatumVypujcky", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.BorrowDate) },
                { "DatumVyrazeni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DiscardDate) },
                { "DatumVymazu", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.EraseDate) },
                { "DatumVytvoreni", nodeEntry.NodeType == SpisumNames.NodeTypes.Document ? nodeEntry.CreatedAt.ToString() : null },
                { "DatumZtraty", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.LostDate) },
                { "databoxAttachmentsCount", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DataboxAttachmentsCount) },
                { "databoxNotRegisteredReason", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DataBoxNotRegisteredReason) },
                { "dmFileMetaType", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ComponentType) },
                { "dmFinalniVerze", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.FinalVersion) },
                { "dmSender", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DataboxSenderName) },
                { "DruhPriloh", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AttachmentsType) },
                { "DuvodVyrazeni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DiscardReason) },
                { "emailAttachmentsCount", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.EmailAttachmentsCount) },
                { "emailDeliveryDate", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.EmailDeliveryDate) },
                { "emailNotRegisteredReason", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.EmailNotRegisteredReason) },
                { "emailSenderAddress", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.EmailSender) },
                { "emailSubject", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.EmailSubject) },
                { "FileIsReadable", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.FileIsReadable) },
                { "FormaDokumentu", nodeEntry.NodeType == SpisumNames.NodeTypes.Document ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Form) : null },
                { "FormaSpisu", nodeEntry.NodeType == SpisumNames.NodeTypes.File ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Form) : null },
                { "Identifikator", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Pid) },
                { "IdentifikatorDA", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.IdDA) },
                { "JePecet", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.IsSealed) },
                { "JePodpis", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.IsSign) },
                { "NazevNavrhu", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ProposalName) },
                { "novyZpracovatel", $"{properties.TryGetValueFromProperties<string>(SpisumNames.Properties.NextGroup)};{properties.TryGetValueFromProperties<string>(SpisumNames.Properties.NextOwner)}" },
                { "ObsahVyrizeni", nodeEntry.NodeType == SpisumNames.NodeTypes.Document ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.CustomSettleMethod) : null },
                { "Odesilatel", nodeEntry.NodeType == SpisumNames.NodeTypes.Document ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender) : null },
                { "OdesilatelovoEvidencniCislo", nodeEntry.NodeType == SpisumNames.NodeTypes.Document ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderIdent) : null },
                { "OdesilatelSpis", nodeEntry.NodeType == SpisumNames.NodeTypes.File ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender) : null },
                { "Oduvodneni", nodeEntry.NodeType == SpisumNames.NodeTypes.Document ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SettleReason) : null },
                { "OduvodneniStorna", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.CancelReason) },
                { "OduvodneniZtraty", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.LostReason) },
                { "OpravnenaOsobaPodpisu", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ForSignatureUser) },
                { "PlatnostBezpecnostnihoPrvku", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ValiditySafetyElement) },
                { "PlatnostBezpecnostnihoPrvkuCertifikatu", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ValiditySafetyCert) },
                { "PlatnostBezpecnostnihoPrvkuRevokaceCertifikatu", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ValiditySafetyCertRevocation) },
                { "PlatnostCertifikacniCesty", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.CertValidityPath) },
                { "PlatnostCertifikatu", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.CertValidity) },
                { "PlatnostDo", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ValidityTo) },
                { "PlatnostOd", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ValidityFrom) },                
                { "PocetDokumentu", nodeEntry.NodeType == SpisumNames.NodeTypes.File ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AssociationCount) : null },
                { "PocetListu", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ListCount) },
                { "PocetListuPriloh", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ListCountAttachments) },
                { "PocetPriloh", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Form) == "analog" ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AttachmentsCount) : properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AssociationCount) },
                { "PosuzovanyOkamzik", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AssessmentMoment) },
                { "predanoKdy", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.HandoverDate) },
                { "RokSkartacniOperace", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ShreddingYear) },
                { "RokSpousteciUdalosti", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.TriggerActionYear) },
                { "RozhodnutiDA", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DecisionDA) },
                { "SerioveCislo", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SerialNumber) },
                { "SkartacniRezim", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.RetentionMode) },
                { "SkartacniZnak", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.RetentionMark) },
                { "SpisovaZnacka", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.FileIdentificator) },
                { "SpisovyPlan", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.FilePlan) },
                { "SpisovyZnak", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.FileMark) },
                { "StavDokumentu", nodeEntry.NodeType == SpisumNames.NodeTypes.Document ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.State) : null },
                { "StavRevokace", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.RevocationState) },
                { "StavSpisu", nodeEntry.NodeType == SpisumNames.NodeTypes.File ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.State) : null },
                { "tDatum", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ShreddingDate) },
                { "tEvidencniCislo", nodeEntry.NodeType == SpisumNames.NodeTypes.File ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderIdent) : null },
                { "tFormaUchovani", getKeepFormType(properties.TryGetValueFromProperties<string>(SpisumNames.Properties.KeepForm)) },
                { "tNazev", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Subject) },
                { "tUzavreni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.TClose) },
                { "tVyrizeni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.TSettle) },
                { "TypBezpecnostnihoPrvku", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SecurityType) },
                { "UlozneMisto", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Location) },
                { "UkladaciJednotka", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Location) },
                { "VlastniKdo", $"{properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Group)};{propertiesDictionary.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")}" },
                { "Vypujcitel", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Borrower) },
                { "VyraditDo", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DiscardTo) },
                { "VyriditDo", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SettleToDate) },
                { "VysledekSkartacnihoRizeni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ShreddingResolution) },
                { "ZpusobVedeni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DirectionMethod) },
                { "ZpusobVyrizeni", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SettleMethod) },
                // odesilatel
                { "ElektronickyKontakt", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender_Contact) },
                { "NazevFyzickeOsoby", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender_Name) },
                { "NazevOrganizace", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderType) == SpisumNames.SenderType.Legal ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender_OrgName) : null },
                { "OrganizacniUtvar", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderType) == SpisumNames.SenderType.Legal ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender_OrgUnit) : null },
                { "PostovniAdresa", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderType) == SpisumNames.SenderType.Individual ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender_Address) : null },
                { "PracovniPozice", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderType) == SpisumNames.SenderType.Legal ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender_Job) : null },
                { "SidloOrganizace", properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderType) == SpisumNames.SenderType.Legal ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender_Address) : null },                
                // zasilky
                { "AdresaRadek1", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Address1) : null },
                { "AdresaRadek2", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Address2) : null },
                { "AdresaRadek3", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Address3) : null },
                { "AdresaRadek4", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Address4) : null },
                { "AdresaRadek5", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AddressStreet) : null },
                { "AdresaRadek6", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AddressCity) : null },
                { "AdresaRadek7", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AddressZip) : null },
                { "AdresaRadek8", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AddressState) : null },
                { "DatumDo", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPublish ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DateTo) : null },
                { "DatumOd", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPublish ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DateFrom) : null },
                { "DatumPredaniVypravne", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ToDispatchDate) : null },
                { "DatumVraceni", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ReturnedDate) : properties.TryGetValueFromProperties<string>(SpisumNames.Properties.BorrowReturnDate) ?? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ReturnedForReworkDate) },
                { "DatumVypraveni", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DispatchedDate) : null },
                { "DatumVytvoreniZasilky", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? nodeEntry.CreatedAt.ToString() : null },
                { "DruhZasilkyId", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPost ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.PostItemType) : null },
                { "DruhZasilkyText", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPost ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.PostItemTypeOther) : null },
                { "dbIDRecipient", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Recipient) : null },
                { "dbIDSender", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender) : null  },
                { "dmAllowSubstDelivery", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.AllowSubstDelivery) : null },
                { "dmAnnotation", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Subject) : properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DataboxSubject) },
                { "dmDeliveryTime", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DeliveryDate) : properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DataboxDeliveryDate) },
                { "dmID", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ItemId) : null },                
                { "dmLegalTitleLaw", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.LegalTitleLaw) : null },
                { "dmLegalTitlePar", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.LegalTitlePar) : null },
                { "dmLegalTitlePoint", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.LegalTitlePoint) : null },
                { "dmLegalTitleSect", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.LegalTitleSect) : null },
                { "dmLegalTitleYear", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.LegalTitleYear) : null },
                { "dmMessageStatus", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.State) : null },
                { "dmPersonalDelivery", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.PersonalDelivery) : null },
                { "dmRecipientIdent", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderIdent) : null },
                { "dmRecipientRefNumber", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.SenderSSID) : null },
                { "dmSenderRefNumber", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Ssid) : null },
                { "dmSenderIdent", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.FileIdentificator) : null },
                { "dmToHands", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ToHands) : null },
                { "emailRecipientAddress", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentEmail ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Recipient) : null },
                { "emailSenderAddress", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentEmail ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Sender) : null },
                { "emailSubject", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentEmail ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.Subject) : null },
                { "DuvodVraceni", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentEmail ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ReasonForReturn) : properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ReasonForRework) },
                { "emailBody", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentEmail ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ShEmailBody) : null },
                { "IdZasilky", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPost ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.PostItemId) : null },
                { "InterniStavZasilky", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.InternalState) : null },
                { "Odkaz" , nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ShRef) : null },
                { "OdkazyNaSoubory", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ShComponentsRef) : null },
                { "PodaciCislo", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPost ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.PostItemNumber) : null },
                { "Poplatek", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPost ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.PostItemPrice) : null },
                { "PostovniSluzbaId", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPost ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.PostType) : null },
                { "PostovniSluzbyPostovniSluzbaText", nodeEntry.NodeType == SpisumNames.NodeTypes.ShipmentPost ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.PostType) : null },
                { "VelikostZpravy", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? properties.TryGetValueFromProperties<string>(SpisumNames.Properties.ShFilesSize) : null },
                { "ZpusobDoruceni", nodeEntry.NodeType.StartsWith(SpisumNames.NodeTypes.Shipment) ? getShipmentType(nodeEntry.NodeType) :  properties.TryGetValueFromProperties<string>(SpisumNames.Properties.DeliveryMode) },
            };
        }

        #endregion
    }
}

