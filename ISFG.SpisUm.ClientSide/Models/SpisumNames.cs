namespace ISFG.SpisUm.ClientSide.Models
{
    public static class SpisumNames
    {
        #region Nested Types, Enums, Delegates

        public static class Associations
        {
            #region Fields
            
            public static readonly string Components = "ssl:components";
            public static readonly string DeletedComponents = "ssl:deletedComponents";
            public static readonly string Documents = "ssl:documents";
            public static readonly string ShipmentsCreated = "ssl:shipmentsCreated";
            public static readonly string ShipmentsToDispatch = "ssl:shipmentsToDispatch";
            public static readonly string ShipmentsDispatched = "ssl:shipmentsDispatched";
            public static readonly string ShipmentsToReturn = "ssl:shipmentsReturned";
            public static readonly string ShipmentsComponents = "ssl:shComponents";
            public static readonly string ShreddingObjects = "ssl:shreddingObjects";
            public static readonly string DigitalDeliveryDocuments = "ssl:digitalDeliveryDocuments";
            public static readonly string DigitalDeliveryCopies = "ssl:digitalDeliveryCopies";
            public static readonly string DigitalDeliveryDocumentsUnfinished = "ssl:digitalDeliveryDocumentsUnfinished";
            public static readonly string DigitalDeliveryAttachments = "ssl:digitalDeliveryAttachments";

            #endregion
        }

        public static class SettleMethod
        {
            #region Fields

            public static readonly string Document = "dokumentem";
            public static readonly string Referral = "postoupenim";
            public static readonly string Acknowledge = "vzetimNaVedomi";
            public static readonly string RecordOnDocument = "zaznamemNaDokumentu";
            public static readonly string Other = "jinyZpusob";

            #endregion
        }

        public static class Headers
        {
            #region Fields

            public static readonly string Group = "Group";

            #endregion
        }

        public static class Groups
        {
            #region Fields

            public static readonly string DispatchGroup = "GROUP_Dispatch";
            public static readonly string Everyone = "GROUP_EVERYONE";
            public static readonly string MainGroup = "GROUP_Spisum"; // for all users
            public static readonly string MailroomGroup = "GROUP_Mailroom";
            public static readonly string RepositoryGroup = "GROUP_Repository";
            public static readonly string RolesGroup = "GROUP_Roles";
            public static readonly string SpisumAdmin = "GROUP_ESSL_Admin"; // for spisum admins
            public static readonly string EmailBox = "GROUP_EMAILBOX";
            public static readonly string DataBox = "GROUP_DATABOX";

            #endregion
        }

        public static class Paths
        {
            #region Fields

            public static readonly string Concepts = "/Concepts";
            public static readonly string ConceptsWaitingForTakeOver = "/Concepts/WaitingForTakeOver";
            public static readonly string Components = "Sites/Components/documentLibrary";
            public static readonly string FilesDocumentsForSignature = "/Files/Documents/ForProcessing/ForSignature";
            public static readonly string Dispatch = "Sites/Dispatch/documentLibrary";
            public static readonly string DispatchCreated = "Sites/Dispatch/documentLibrary/Created";
            public static readonly string DispatchDispatched = "Sites/Dispatch/documentLibrary/Dispatched";
            public static readonly string DispatchReturned = "Sites/Dispatch/documentLibrary/Returned";
            public static readonly string DispatchToDispatch = "Sites/Dispatch/documentLibrary/ToDispatch";
            public static readonly string Documents = "/Documents";
            public static readonly string DocumentsProcessed = "/Documents/Processed";
            public static readonly string DocumentsForProcessing = "/Documents/ForProcessing";
            public static readonly string DocumentsForSignature = "/Documents/ForProcessing/ForSignature";
            public static readonly string DocumentsForProcessingWaitingForTakeOver = "/Documents/ForProcessing/WaitingForTakeOver";
            public static readonly string DocumentsProcessedWaitingForTakeOver = "/Documents/Processed/WaitingForTakeOver";
            public static readonly string Evidence = "Sites/Evidence";
            public static readonly string FilesClosed = "/Files/Closed";
            public static readonly string FilesClosedWaitingForTakeOver = "/Files/Closed/WaitingForTakeOver";
            public static readonly string FilesDocumentsForProcessing = "/Files/Documents/ForProcessing";
            public static readonly string FilesDocumentsForProcessed = "/Files/Documents/Processed";
            public static readonly string FilesOpen = "/Files/Open";
            public static readonly string FilesOpenWaitingForTakeOver = "/Files/Open/WaitingForTakeOver";
            public static readonly string MailRoomDataBox = "Sites/Mailroom/documentLibrary/DataBox";
            public static readonly string MailRoomDataBoxArchived = "Sites/Mailroom/documentLibrary/DataBox/Archived";
            public static readonly string MailRoomDataBoxNotRegistered = "Sites/Mailroom/documentLibrary/DataBox/NotRegistered";
            public static readonly string MailRoomDataBoxUnprocessed = "Sites/Mailroom/documentLibrary/DataBox/Unprocessed";
            public static readonly string MailRoomEmail = "Sites/Mailroom/documentLibrary/MailBox";
            public static readonly string MailRoomEmailArchived = "Sites/Mailroom/documentLibrary/MailBox/Archived";
            public static readonly string MailRoomEmailNotRegistered = "Sites/Mailroom/documentLibrary/MailBox/NotRegistered";
            public static readonly string MailRoomEmailUnprocessed = "Sites/Mailroom/documentLibrary/MailBox/Unprocessed";
            public static readonly string MailRoomNotPassed = "Sites/Mailroom/documentLibrary/NotPassed";
            public static readonly string MailRoomUnfinished = "Sites/Mailroom/documentLibrary/Unfinished";
            public static readonly string MailRoomWaitingForTakeOver = "Sites/Mailroom/documentLibrary/WaitingForTakeOver";
            public static readonly string RepositoryArchived = "Sites/Repository/documentLibrary/Archived";
            public static readonly string RepositoryDocumentsInFiles = "Sites/Repository/documentLibrary/DocumentsInFiles";
            public static readonly string RepositoryRented = "Sites/Repository/documentLibrary/Rented";
            public static readonly string RepositoryShredded = "Sites/Repository/documentLibrary/Shredded";
            public static readonly string RepositoryShreddingProposal = "Sites/Repository/documentLibrary/ShreddingProposal";
            public static readonly string RepositoryStored = "Sites/Repository/documentLibrary/Stored";
            public static readonly string RMDocumentLibrary = "Sites/rm/documentLibrary";
            public static readonly string RMShreddingPlan = "Sites/rm/documentLibrary/ShreddingPlan";

            #endregion

            #region Static Methods

            public static string EvidenceCancelled(string group) { return $"Sites/Evidence/documentLibrary/{group}/Cancelled"; }
            public static string EvidenceConcepts(string group) { return $"Sites/Evidence/documentLibrary/{group}/Concepts"; }
            public static string EvidenceConceptsWaitingForTakeOver(string group) { return $"Sites/Evidence/documentLibrary/{group}/Concepts/WaitingForTakeOver"; }
            public static string EvidenceDocuments(string group) { return $"Sites/Evidence/documentLibrary/{group}/Documents"; }
            public static string EvidenceDocumentsForProcessing(string group) { return $"Sites/Evidence/documentLibrary/{group}/Documents/ForProcessing"; }
            public static string EvidenceDocumentsForProcessingForSignature(string group) { return $"Sites/Evidence/documentLibrary/{group}/Documents/ForProcessing/ForSignature"; }
            public static string EvidenceDocumentsForProcessingWaitingForTakeOver(string group) { return $"Sites/Evidence/documentLibrary/{group}/Documents/ForProcessing/WaitingForTakeOver"; }
            public static string EvidenceDocumentsProcessed(string group) { return $"Sites/Evidence/documentLibrary/{group}/Documents/Processed"; }
            public static string EvidenceDocumentsProcessedWaitingForTakeOver(string group) { return $"Sites/Evidence/documentLibrary/{group}/Documents/Processed/WaitingForTakeOver"; }
            public static string EvidenceDocumentsStored(string group) { return $"Sites/Evidence/documentLibrary/{group}/Documents/Stored"; }
            public static string EvidenceFiles(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files"; }
            public static string EvidenceFilesClosed(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Closed"; }
            public static string EvidenceFilesClosedWaitingForTakeOver(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Closed/WaitingForTakeOver"; }
            public static string EvidenceFilesDocuments(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Documents"; }
            public static string EvidenceFilesDocumentsCancelled(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Documents/Cancelled"; }
            public static string EvidenceFilesDocumentsForProcessing(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Documents/ForProcessing"; }
            public static string EvidenceFilesDocumentsForProcessingForSignature(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Documents/ForProcessing/ForSignature"; }
            public static string EvidenceFilesDocumentsProcessed(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Documents/Processed"; }
            public static string EvidenceFilesDocumentsStored(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Documents/Stored"; }
            public static string EvidenceFilesOpen(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Open"; }
            public static string EvidenceFilesOpenWaitingForTakeOver(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Open/WaitingForTakeOver"; }
            public static string EvidenceFilesStored(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Stored"; }
            public static string EvidenceLostDestroyed(string group) { return $"Sites/Evidence/documentLibrary/{group}/LostDestroyed"; }
            public static string EvidenceFilesLostDestroyed(string group) { return $"Sites/Evidence/documentLibrary/{group}/Files/Documents/LostDestroyed"; }
            public static string EvidenceForSignature(string group) { return $"Sites/Evidence/documentLibrary/{group}/ForSignature"; }
            public static string EvidenceToTakeOver(string group) { return $"Sites/Evidence/documentLibrary/{group}/ToTakeOver"; }
            public static string EvidenceGroup(string group) { return $"Sites/Evidence/documentLibrary/{group}"; }
            public static string EvidenceWaitingForTakeOver(string group) { return $"Sites/Evidence/documentLibrary/{group}/WaitingForTakeOver"; }
            public static string EvidenceWaitingForSignature(string group) { return $"Sites/Evidence/documentLibrary/{group}/WaitingForSignature"; }
            public static string RMShreddingPlanFolder(string filePlan, string fileMark) => $"Sites/rm/documentLibrary/ShreddingPlan/{filePlan}/{fileMark}";
            public static string RMShreddingPlanFolderContents(string filePlan, string fileMark) => $"Sites/rm/documentLibrary/ShreddingPlan/{filePlan}/{fileMark}/Contents";

            #endregion
        }

        public static class NodeTypes
        {
            #region Fields

            public static readonly string Component = "ssl:component";
            public static readonly string Concept = "ssl:concept";
            public static readonly string DataBox = "ssl:databox";
            public static readonly string DataBoxComponent = "ssl:databoxComponent";
            public static readonly string DataFolder = "ssl:dataFolder";
            public static readonly string Document = "ssl:document";
            public static readonly string DocumentRM = "ssl:documentRM";
            public static readonly string Email = "ssl:email";
            public static readonly string EmailComponent = "ssl:emailComponent";
            public static readonly string File = "ssl:file";
            public static readonly string FileRM = "ssl:fileRM";
            public static readonly string Shipment = "ssl:shipment";
            public static readonly string ShipmentDatabox = "ssl:shipmentDatabox";
            public static readonly string ShipmentEmail = "ssl:shipmentEmail";
            public static readonly string ShipmentPersonally = "ssl:shipmentPersonally";
            public static readonly string ShipmentPost = "ssl:shipmentPost";
            public static readonly string ShipmentPublish = "ssl:shipmentPublish";
            public static readonly string ShreddingProposal = "ssl:shreddingProposal";
            public static readonly string TakeConcept = "ssl:takeConcept";
            public static readonly string TakeDocumentForProcessing = "ssl:takeDocumentForProcessing";
            public static readonly string TakeDocumentProcessed = "ssl:takeDocumentProcessed";
            public static readonly string TakeFileClosed = "ssl:takeFileClosed";
            public static readonly string TakeFileOpen = "ssl:takeFileOpen";

            #endregion
        }

        public static class Postfixes
        {
            #region Fields

            public static readonly string Sign = "_Sign";

            #endregion
        }

        public static class Prefixes
        {
            #region Fields

            public static readonly string Group = "GROUP_";
            public static readonly string FileSsidPrefix = "S-";
            public static readonly string UserGroup = "GROUP_USER_";

            #endregion
        }

        public static class Properties
        {
            #region Fields

            public static readonly string ComponentVersionJSON = "ssl:componentsVersionJSON";
            public static readonly string Version = "ssl:version";
            public static readonly string Address1 = "ssl:address1";
            public static readonly string Address2 = "ssl:address2";
            public static readonly string Address3 = "ssl:address3";
            public static readonly string Address4 = "ssl:address4";
            public static readonly string AddressCity = "ssl:addressCity";
            public static readonly string AddressState = "ssl:addressState";
            public static readonly string AddressStreet = "ssl:addressStreet";
            public static readonly string AddressZip = "ssl:addressZip";
            public static readonly string AssessmentMoment = "ssl:assessmentMoment";
            public static readonly string AttachmentsCount = "ssl:attachmentsCount";
            public static readonly string AttachmentsType = "ssl:attachmentsType";
            public static readonly string Author = "ssl:author";
            public static readonly string AuthorId = "ssl:author_id";
            public static readonly string AuthorOrgId = "ssl:author_orgId";
            public static readonly string AuthorOrgName = "ssl:author_orgName";
            public static readonly string AuthorOrgUnit = "ssl:author_orgUnit";
            public static readonly string AuthorJob = "ssl:author_job";
            public static readonly string AuthorOrgAddress = "ssl:author_orgAddress";
            public static readonly string AllowSubstDelivery = "ssl:allowSubstDelivery";
            public static readonly string AssociationCount = "ssl:associationCount";
            public static readonly string BorrowDate = "ssl:borrowDate";
            public static readonly string Borrower = "ssl:borrower";
            public static readonly string BorrowGroup = "ssl:borrowGroup";
            public static readonly string BorrowReturnDate = "ssl:borrowReturnDate";
            public static readonly string CanBeSigned = "ssl:canBeSigned";              
            public static readonly string CancelDate = "ssl:cancelDate";
            public static readonly string CancelReason = "ssl:cancelReason";
            public static readonly string CertValidity = "ssl:certValidity";
            public static readonly string CertValidityPath = "ssl:certValidityPath";
            public static readonly string ClosureDate = "ssl:closureDate";
            public static readonly string ComponentType = "ssl:componentType";
            public static readonly string CreatedDate = "ssl:createdDate";
            public static readonly string CrlNumber = "ssl:crlNumber";
            public static readonly string CurrentOwner = "ssl:currentOwner";
            public static readonly string CustomSettleMethod = "ssl:customSettleMethod";
            public static readonly string DateFrom = "ssl:dateFrom";
            public static readonly string DateTo = "ssl:dateTo";
            public static readonly string DataboxSender = "ssl:databoxSender";
            public static readonly string DataboxSenderName = "ssl:databoxSenderName";
            public static readonly string DecisionDA = "ssl:decisionDA";
            public static readonly string DeliveryDate = "ssl:deliveryDate";
            public static readonly string DeliveryMode = "ssl:deliveryMode";
            public static readonly string DirectionMethod = "ssl:directionMethod";
            public static readonly string DiscardDate = "ssl:discardDate";
            public static readonly string DiscardReason = "ssl:discardReason";
            public static readonly string DiscardTo = "ssl:discardTo";
            public static readonly string DispatchedDate = "ssl:dispatchedDate";
            public static readonly string DocumentType = "ssl:documentType";
            public static readonly string FinalVersion = "ssl:finalVersion";
            public static readonly string ComponentCounter = "ssl:componentCounter";
            public static readonly string ComponentVersion = "ssl:componentVersion";
            public static readonly string ComponentVersionId = "ssl:componentVersionId";
            public static readonly string ComponentVersionOperation = "ssl:componentVersionOperation";
            public static readonly string ShipmentCounter = "ssl:shipmentCounter";
            public static readonly string DigitalDeliveryAttachmentsCount = "ssl:digitalDeliveryAttachmentsCount";
            public static readonly string DigitalDeliveryDeliveryDate = "ssl:digitalDeliveryDeliveryDate";
            public static readonly string DigitalDeliveryNotRegisteredReasion = "ssl:digitalDeliveryNotRegisteredReason";
            public static readonly string EmailRecipient = "ssl:emailRecipient";
            public static readonly string EmailSender = "ssl:emailSender";
            public static readonly string DigitalDeliverySubject = "ssl:digitalDeliverySubject";
            public static readonly string EraseDate = "ssl:eraseDate";
            public static readonly string FileIdentificator = "ssl:fileIdentificator";
            public static readonly string FileIsReadable = "ssl:fileIsReadable";
            public static readonly string FileIsInOutputFormat = "ssl:fileIsInOutputFormat";
            public static readonly string FileMark = "ssl:fileMark";
            public static readonly string FileName = "ssl:fileName";
            public static readonly string FilePlan = "ssl:filePlan";
            public static readonly string Form = "ssl:form";
            public static readonly string ForSignatureDate = "ssl:forSignatureDate";
            public static readonly string ForSignatureGroup = "ssl:forSignatureGroup";
            public static readonly string ForSignatureUser = "ssl:forSignatureUser";
            public static readonly string Group = "ssl:group";
            public static readonly string HandoverDate = "ssl:handoverDate";
            public static readonly string IdDA = "ssl:idDA";
            public static readonly string IsInFile = "ssl:isInFile";
            public static readonly string IsSealed = "ssl:isSealed";
            public static readonly string IsSign = "ssl:isSign";
            public static readonly string ItemId = "ssl:itemId";
            public static readonly string InternalState = "ssl:internalState";
            public static readonly string KeepForm = "ssl:keepForm";
            public static readonly string LegalTitleSect = "ssl:legalTitleSect";
            public static readonly string LegalTitleLaw = "ssl:legalTitleLaw";
            public static readonly string LegalTitlePar = "ssl:legalTitlePar";
            public static readonly string LegalTitlePoint = "ssl:legalTitlePoint";
            public static readonly string LegalTitleYear = "ssl:legalTitleYear";
            public static readonly string ListCount = "ssl:listCount";
            public static readonly string ListCountAttachments = "ssl:listCountAttachments";
            public static readonly string Location = "ssl:location";
            public static readonly string LostDate = "ssl:lostDate";
            public static readonly string LostReason = "ssl:lostReason";
            public static readonly string NextGroup = "ssl:nextGroup";
            public static readonly string NextOwner = "ssl:nextOwner";
            public static readonly string NextOwnerDecline = "ssl:nextOwnerDecline";
            public static readonly string Note = "ssl:note";           
            public static readonly string ShRef = "ssl:shRef";
            public static readonly string ShRefId = "ssl:shRefId";
            public static readonly string PersonalDelivery = "ssl:personalDelivery";
            public static readonly string Pid = "ssl:pid";
            public static readonly string PidArchive = "ssl:pidArchive";
            public static readonly string PidRef = "ssl:pidRef";
            public static readonly string Ref = "ssl:ref";
            public static readonly string Name = "ssl:name";
            public static readonly string PreviousPath = "ssl:previousPath";
            public static readonly string PreviousIsLocked = "ssl:previousIsLocked";
            public static readonly string PreviousState = "ssl:previousState";
            public static readonly string QualifiedCertType = "ssl:qualifiedCertType";
            public static readonly string RetentionMark = "ssl:retentionMark";
            public static readonly string RetentionMode = "ssl:retentionMode";
            public static readonly string RetentionPeriod = "ssl:retentionPeriod";
            public static readonly string PostItemCashOnDelivery = "ssl:postItemCashOnDelivery";
            public static readonly string PostItemId = "ssl:postItemId";
            public static readonly string PostItemType = "ssl:postItemType";
            public static readonly string PostItemTypeOther = "ssl:postItemTypeOther";
            public static readonly string PostItemNumber = "ssl:postItemNumber";
            public static readonly string PostItemPrice = "ssl:postItemPrice";
            public static readonly string PostItemStatedPrice = "ssl:postItemStatedPrice";
            public static readonly string PostItemWeight = "ssl:postItemWeight";
            public static readonly string PostType = "ssl:postType";
            public static readonly string PostTypeOther = "ssl:postTypeOther";
            public static readonly string Processor = "ssl:processor";
            public static readonly string ProposalName = "ssl:proposalName";
            public static readonly string ReasonForReturn = "ssl:reasonForReturn";
            public static readonly string ReasonForRework = "ssl:reasonForRework";
            public static readonly string ReturnedForReworkDate = "ssl:returnedForReworkDate";
            public static readonly string Recipient = "ssl:recipient";
            public static readonly string ReturnedDate = "ssl:returnedDate";
            public static readonly string RepositoryName = "ssl:repositoryName";
            public static readonly string RevocationState = "ssl:revocationState";
            public static readonly string SafetyElementsCheck = "ssl:safetyElementsCheck";
            public static readonly string SerialNumber = "ssl:serialNumber";
            public static readonly string Sender = "ssl:sender";
            public static readonly string Sender_Address = "ssl:sender_address";
            public static readonly string Sender_Contact = "ssl:sender_contact";
            public static readonly string Sender_Job = "ssl:sender_job";
            public static readonly string Sender_Name = "ssl:sender_name";
            public static readonly string Sender_OrgName = "ssl:sender_orgName";
            public static readonly string Sender_OrgUnit = "ssl:sender_orgUnit";
            public static readonly string SenderType = "ssl:senderType";
            public static readonly string SettleDate = "ssl:settleDate";
            public static readonly string SettleMethod = "ssl:settleMethod";
            public static readonly string SettleReason = "ssl:settleReason";
            public static readonly string SettleToDate = "ssl:settleToDate";
            public static readonly string SecurityType = "ssl:securityType";
            public static readonly string SenderIdent = "ssl:senderIdent";
            public static readonly string ShComponentsRef = "ssl:shComponentsRef";
            public static readonly string ShEmailBody = "ssl:shEmailBody";
            public static readonly string ShFilesSize = "ssl:shFilesSize";
            public static readonly string ShreddingDate = "ssl:shreddingDate";
            public static readonly string ShreddingResolution = "ssl:shreddingResolution";
            public static readonly string ShreddingYear = "ssl:shreddingYear";
            public static readonly string SignLocation = "ssl:signLocation";
            public static readonly string SignReason = "ssl:signReason";
            public static readonly string Ssid = "ssl:ssid";
            public static readonly string SenderSSID = "ssl:senderSSID";
            public static readonly string SendMode = "ssl:sendMode";
            public static readonly string SsidNumber = "ssl:ssidNumber";
            public static readonly string State = "ssl:state";
            public static readonly string ShipmentPostState = "ssl:shipmentPostState";
            public static readonly string ShipmentDataboxState = "ssl:shipmentDataBoxState";
            public static readonly string Subject = "ssl:subject";
            public static readonly string TakeRef = "ssl:takeRef";
            public static readonly string TimeStampText = "ssl:timestampText";
            public static readonly string ToDispatchDate = "ssl:toDispatchDate";
            public static readonly string ToHands = "ssl:toHands";
            public static readonly string ToArchiveShreddingDate = "ssl:toArchiveShreddingDate";
            public static readonly string ToRepositoryDate = "ssl:toRepositoryDate";
            public static readonly string TriggerActionYear = "ssl:triggerActionYear";
            public static readonly string UsedTime = "ssl:usedTime";
            public static readonly string UserId = "ssl:user_id";
            public static readonly string UserName = "ssl:user_name";
            public static readonly string UserOrgId = "ssl:user_orgId";
            public static readonly string UserOrgName = "ssl:user_orgName";
            public static readonly string UserOrgUnit = "ssl:user_orgUnit";
            public static readonly string UserJob = "ssl:user_job";
            public static readonly string UserOrgAddress = "ssl:user_orgAddress";
            public static readonly string ProcessorId = "ssl:processor_id";
            public static readonly string ProcessorOrgId = "ssl:processor_orgId";
            public static readonly string ProcessorOrgName = "ssl:processor_orgName";
            public static readonly string ProcessorOrgUnit = "ssl:processor_orgUnit";
            public static readonly string ProcessorJob = "ssl:processor_job";
            public static readonly string ProcessorOrgAddress = "ssl:processor_orgAddress";
            public static readonly string ValidityFrom = "ssl:validityFrom";
            public static readonly string ValidityTo = "ssl:validityTo";
            public static readonly string ValiditySafetyElement = "ssl:validitySafetyElement";
            public static readonly string ValiditySafetyCert = "ssl:validitySafetyCert";
            public static readonly string ValiditySafetyCertRevocation = "ssl:validitySafetyCertRevocation";
            public static readonly string VerificationTime = "ssl:verificationTime";
            public static readonly string VerifierId = "ssl:verifier_id";
            public static readonly string VerifierName = "ssl:verifier_name";
            public static readonly string VerifierOrgId = "ssl:verifier_orgId";
            public static readonly string VerifierOrgName = "ssl:verifier_orgName";
            public static readonly string VerifierOrgUnit = "ssl:verifier_orgUnit";
            public static readonly string VerifierJob = "ssl:verifier_job";
            public static readonly string VerifierOrgAddress = "ssl:verifier_orgAddress";
            public static readonly string PublisherAddress = "ssl:publisher_address";
            public static readonly string PublisherContact = "ssl:publisher_contact";
            public static readonly string PublisherName = "ssl:publisher_name";
            public static readonly string PublisherOrgName = "ssl:publisher_orgName";
            public static readonly string PublisherOrgUnit = "ssl:publisher_orgUnit";
            public static readonly string PublisherJob = "ssl:publisher_job";
            public static readonly string PublisherOrgAddress = "ssl:publisher_orgAddress";
            public static readonly string HolderAddress = "ssl:holder_address";
            public static readonly string HolderContact = "ssl:holder_contact";
            public static readonly string HolderName = "ssl:holder_name";
            public static readonly string HolderOrgName = "ssl:holder_orgName";
            public static readonly string HolderOrgUnit = "ssl:holder_orgUnit";
            public static readonly string HolderJob = "ssl:holder_job";
            public static readonly string HolderOrgAddress = "ssl:holder_orgAddress";
            public static readonly string FileIsSigned = "ssl:fileIsSigned";
            public static readonly string LinkRendering = "ssl:linkRendering";
            public static readonly string ListOriginalComponent = "ssl:linkOriginalComponent";
            public static readonly string CompanyImplementingDataFormat = "ssl:companyImplementingDataFormat";
            public static readonly string AuthorChangeOfDataFormat = "ssl:autorChangeOfDataFormat";
            public static readonly string OriginalDataFormat = "ssl:originalDataFormat";
            public static readonly string OriginalDestinationHandover = "ssl:originalDestinationHandover";
            public static readonly string OriginalDestinationGroupHandover = "ssl:originalDestinationGroupHandover";
            public static readonly string DataCompleteVerificationItem = "ssl:dateCompleteVerificationItem";
            public static readonly string ImprintFile = "ssl:imprintFile";
            public static readonly string UsedAlgorithm = "ssl:usedAlgorithm";
            public static readonly string WaitingRef = "ssl:waitingRef";

            #endregion
        }

        public static class SenderType
        {
            #region Fields

            public static readonly string Individual = "individual";
            public static readonly string Legal = "legal";
            public static readonly string Own = "own";

            #endregion
        }

        public static class ShipmentPostState
        {
            public static readonly string Nevypraveno = "nevypraveno";
            public static readonly string Vypraveno = "vypraveno";
            public static readonly string Doruceno = "doruceno";
            public static readonly string VracenoJinyDuvodOvereno = "vraceno-jiny-duvod-overeno";
            public static readonly string VracenoJinyDuvodNeovereno = "vraceno-jiny-duvod-neovereno";
            public static readonly string VracenoAdresatNeznamy = "vraceno-adresat-neznamy";
            public static readonly string VracenoAdresatSeOdstehoval = "vraceno-adresat-se-odstehoval";
            public static readonly string VracenoNeprijato = "vraceno-neprijato";
            public static readonly string VracenoNevyzadano = "vraceno-nevyzadano";
            public static readonly string NedorucenoZpracovano = "nedoruceno-zpracovano";
            public static readonly string VracenoAdresaNedostatecna = "vraceno-adresa-nedostatecna";
            public static readonly string Stornovano = "stornovano";
        }
        public static class ShipmentDataboxState
        {
            public static readonly string One = "1";
            public static readonly string Two = "2";
            public static readonly string Three = "3";
            public static readonly string Four = "4";
            public static readonly string Five = "5";
            public static readonly string Six = "6";
            public static readonly string Seven = "7";
            public static readonly string Eight = "8";
            public static readonly string Nine = "9";
            public static readonly string Ten = "10";
        }
        public static class State
        {
            #region Fields

            public static readonly string Unprocessed = "nevyrizen";
            public static readonly string Settled = "vyrizen";
            public static readonly string Closed = "uzavren";
            public static readonly string HandoverToRepository = "predanDoSpisovny";
            public static readonly string HandoverToArchive = "predanDoArchivu";
            public static readonly string Cancel = "stornovan";
            public static readonly string Shredded = "skartovan";

            #endregion
        }

        public static class InternalState
        {
            #region Fields

            public static readonly string Created = "Vytvorena";
            public static readonly string ToDispatch = "kVypraveni";
            public static readonly string Dispatched = "Vypravena";
            public static readonly string Delivered = "Dorucena";
            public static readonly string Returned = "Vracena";

            #endregion
        }

        public static class SystemUsers
        {
            #region Fields

            public static readonly string Admin = "admin";
            public static readonly string Databox = "databox";
            public static readonly string Emailbox = "emailbox";
            public static readonly string SAdmin = "sAdmin";
            public static readonly string Spisum = "spisum";

            #endregion
        }
        public static class Component
        {
            #region Fields

            public static readonly string Main = "main";

            #endregion
        }

        public static class StoreForm
        {
            #region Fields

            public static readonly string Original = "original";

            #endregion
        }

        public static class KeepForm
        {
            #region Fields

            public static readonly string Original = "original";
            public static readonly string Original_InOutputFormat = "original_inOutputFormat";

            #endregion
        }

        public static class Form
        {
            #region Fields

            public static readonly string Analog = "analog";
            public static readonly string Digital = "digital";
            public static readonly string Hybrid = "hybrid";

            #endregion
        }

        public static class Other
        {
            #region Fields

            public static readonly string Unprocessed = "nevyrizen";
            public static readonly string Priorace = "Priorace";
            public static readonly string Own = "Vlastni";

            #endregion
        }

        public static class VersionOperation
        {
            #region Fields

            public static readonly string Add = "add";
            public static readonly string Update = "update";
            public static readonly string Remove = "remove";

            #endregion
        }

        public static class Signer
        {
            #region Fields

            public static readonly string Valid = "platný";
            public static readonly string NotValid = "neplatný";
            public static readonly string ValidityAssessed = "platnost nelze posoudit";
            public static readonly string Qualified = "kvalifikovaný certifikát";
            public static readonly string Commercial = "komerční certifikát";
            public static readonly string InternalStorage = "certifikát pocházející z interního úložiště";
            public static readonly string Unknown = "neznámý certifikát";

            public static readonly string External = "external";
            public static readonly string Type = "type";
            public static readonly string Url = "url";
            public static readonly string Input = "input";
            public static readonly string Status = "status";
            public static readonly string Output = "output";
            public static readonly string Batch = "batch";
            
            public static readonly string Visual = "visual";
            public static readonly string Sign = "sign";

            public static readonly string CallSigner = "call-signer";
            
            #endregion
        }

        public static class Dn
        {
            #region Fields

            public static readonly string CommonName = "CN";
            public static readonly string Organization = "O";
            public static readonly string OrganizationalUnit = "OU";
            public static readonly string Email = "E";
            public static readonly string Locality = "L";
            public static readonly string Country = "C";
            public static readonly string SerialNumber = "SERIALNUMBER";
            public static readonly string Surname = "SN";
            public static readonly string GivenName = "GN";

            #endregion
        }
        
        public static class Global
        {
            #region Fields

            public static readonly string Yes = "yes";
            public static readonly string No = "no";
            public static readonly string Impossible = "impossible";
            public static readonly string Converted = "converted";
            public static readonly string Sha256 = "SHA-256";
            
            #endregion
        }

        #endregion
    }
}