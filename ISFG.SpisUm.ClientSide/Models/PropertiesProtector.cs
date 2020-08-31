using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.Models
{
    public static class PropertiesProtector
    {
        private static List<string> propertiesGlobal
        {
            get
            {
                return new List<string>()
                {
                    SpisumNames.Properties.ComponentVersionJSON,
                    SpisumNames.Properties.Version,
                    SpisumNames.Properties.Author,
                    SpisumNames.Properties.AuthorId,
                    SpisumNames.Properties.AuthorOrgId,
                    SpisumNames.Properties.AuthorOrgName,
                    SpisumNames.Properties.AuthorOrgUnit,
                    SpisumNames.Properties.AuthorJob,
                    SpisumNames.Properties.AuthorOrgAddress,
                    SpisumNames.Properties.AssociationCount,
                    SpisumNames.Properties.BorrowDate,
                    SpisumNames.Properties.Borrower,
                    SpisumNames.Properties.BorrowGroup,
                    SpisumNames.Properties.BorrowReturnDate,
                    SpisumNames.Properties.CancelDate,
                    SpisumNames.Properties.CancelReason,
                    SpisumNames.Properties.CertValidity,
                    SpisumNames.Properties.CertValidityPath,
                    SpisumNames.Properties.ClosureDate,
                    SpisumNames.Properties.CrlNumber,
                    SpisumNames.Properties.CurrentOwner,
                    SpisumNames.Properties.DigitalDeliveryAttachmentsCount,
                    SpisumNames.Properties.DigitalDeliveryDeliveryDate,
                    SpisumNames.Properties.DigitalDeliveryNotRegisteredReasion,
                    SpisumNames.Properties.DigitalDeliverySubject,
                    SpisumNames.Properties.DataboxSender,
                    SpisumNames.Properties.DataboxSenderName,                    
                    SpisumNames.Properties.DecisionDA,
                    SpisumNames.Properties.DispatchedDate,
                    SpisumNames.Properties.DocumentType,
                    SpisumNames.Properties.FinalVersion,
                    SpisumNames.Properties.Form,
                    SpisumNames.Properties.ComponentCounter,
                    SpisumNames.Properties.ComponentVersion,
                    SpisumNames.Properties.ComponentVersionId,
                    SpisumNames.Properties.ComponentVersionOperation,
                    SpisumNames.Properties.ShipmentCounter,
                    SpisumNames.Properties.EmailRecipient,
                    SpisumNames.Properties.EmailSender,
                    SpisumNames.Properties.EraseDate,
                    SpisumNames.Properties.FileIdentificator,
                    SpisumNames.Properties.FileIsInOutputFormat,
                    SpisumNames.Properties.SafetyElementsCheck,
                    SpisumNames.Properties.FileName,
                    SpisumNames.Properties.ForSignatureDate,
                    SpisumNames.Properties.ForSignatureGroup,
                    SpisumNames.Properties.ForSignatureUser,
                    SpisumNames.Properties.Group,
                    SpisumNames.Properties.HandoverDate,
                    SpisumNames.Properties.IdDA,
                    SpisumNames.Properties.IsInFile,
                    SpisumNames.Properties.IsSealed,
                    SpisumNames.Properties.IsSign,
                    SpisumNames.Properties.ItemId,
                    SpisumNames.Properties.InternalState,
                    SpisumNames.Properties.LostDate,
                    SpisumNames.Properties.LostReason,
                    SpisumNames.Properties.NextGroup,
                    SpisumNames.Properties.NextOwner,
                    SpisumNames.Properties.NextOwnerDecline,
                    SpisumNames.Properties.ShRef,
                    SpisumNames.Properties.ShRefId,
                    SpisumNames.Properties.Pid,
                    SpisumNames.Properties.PidArchive,
                    SpisumNames.Properties.PidRef,
                    SpisumNames.Properties.Ref,
                    SpisumNames.Properties.Ssid,
                    SpisumNames.Properties.PreviousPath,
                    SpisumNames.Properties.PreviousIsLocked,
                    SpisumNames.Properties.PreviousState,
                    SpisumNames.Properties.QualifiedCertType,
                    SpisumNames.Properties.Processor,
                    SpisumNames.Properties.ReasonForReturn,
                    SpisumNames.Properties.ReasonForRework,
                    SpisumNames.Properties.ReturnedForReworkDate,
                    SpisumNames.Properties.ReturnedDate,
                    SpisumNames.Properties.ShComponentsRef,
                    SpisumNames.Properties.ShEmailBody,
                    SpisumNames.Properties.ShFilesSize,
                    SpisumNames.Properties.ShreddingDate,
                    SpisumNames.Properties.ShreddingResolution,
                    SpisumNames.Properties.ShreddingYear,
                    SpisumNames.Properties.State,
                    SpisumNames.Properties.TakeRef,
                    SpisumNames.Properties.TimeStampText,
                    SpisumNames.Properties.ToDispatchDate,
                    SpisumNames.Properties.UsedTime,
                    SpisumNames.Properties.UserId,
                    SpisumNames.Properties.UserName,
                    SpisumNames.Properties.UserOrgId,
                    SpisumNames.Properties.UserOrgName,
                    SpisumNames.Properties.UserOrgUnit,
                    SpisumNames.Properties.UserJob,
                    SpisumNames.Properties.UserOrgAddress,
                    SpisumNames.Properties.ProcessorId,
                    SpisumNames.Properties.ProcessorOrgId,
                    SpisumNames.Properties.ProcessorOrgName,
                    SpisumNames.Properties.ProcessorOrgUnit,
                    SpisumNames.Properties.ProcessorJob,
                    SpisumNames.Properties.ProcessorOrgAddress,
                    SpisumNames.Properties.ValidityFrom,
                    SpisumNames.Properties.ValidityTo,
                    SpisumNames.Properties.ValiditySafetyElement,
                    SpisumNames.Properties.ValiditySafetyCert,
                    SpisumNames.Properties.ValiditySafetyCertRevocation,
                    SpisumNames.Properties.VerificationTime,
                    SpisumNames.Properties.VerifierId,
                    SpisumNames.Properties.VerifierName,
                    SpisumNames.Properties.VerifierOrgId,
                    SpisumNames.Properties.VerifierOrgName,
                    SpisumNames.Properties.VerifierOrgUnit,
                    SpisumNames.Properties.VerifierJob,
                    SpisumNames.Properties.VerifierOrgAddress,
                    SpisumNames.Properties.FileIsSigned,
                    SpisumNames.Properties.LinkRendering,
                    SpisumNames.Properties.ListOriginalComponent,
                    SpisumNames.Properties.CompanyImplementingDataFormat,
                    SpisumNames.Properties.AuthorChangeOfDataFormat,
                    SpisumNames.Properties.OriginalDataFormat,
                    SpisumNames.Properties.DataCompleteVerificationItem,
                    SpisumNames.Properties.ImprintFile,
                    SpisumNames.Properties.UsedAlgorithm,
                    SpisumNames.Properties.WaitingRef
                };
            }
        }

        private static List<string> propertiesComponent
        {
            get
            {
                return new List<string>(propertiesGlobal)
                {

                };
            }
        }

        private static List<string> propertiesConcept
        {
            get
            {
                return new List<string>(propertiesDocument)
                {

                };
            }
        }

        private static List<string> propertiesDocument
        {
            get
            {
                return new List<string>(propertiesGlobal)
                {

                };
            }
        }

        private static List<string> propertiesFile
        {
            get
            {
                return new List<string>(propertiesGlobal)
                {

                };
            }
        }

        private static List<string> propertiesShipment
        {
            get
            {
                return new List<string>()
                {

                };
            }
        }

        public static Dictionary<string, object> Filter(string nodeType, Dictionary<string, object> properties)
        {
            if (properties == null)
                return null;
            if (nodeType == SpisumNames.NodeTypes.Concept)
                return protect(properties, propertiesConcept);
            if (nodeType == SpisumNames.NodeTypes.Document)
                return protect(properties, propertiesDocument);
            if (nodeType == SpisumNames.NodeTypes.File)
                return protect(properties, propertiesFile);
            if (nodeType.StartsWith(SpisumNames.NodeTypes.Shipment))
                return protect(properties, propertiesShipment);
            if (nodeType == SpisumNames.NodeTypes.Component)
                return protect(properties, propertiesComponent);

            return properties;
        }

        private static Dictionary<string, object> protect(Dictionary<string, object> properties, List<string> forbiddenValues)
        {
            var result = new Dictionary<string, object>();

            foreach (var item in properties)
            {
                if (!forbiddenValues.Contains(item.Key))
                    result.Add(item.Key, item.Value);
            }

            return result;
        }
    }
}
