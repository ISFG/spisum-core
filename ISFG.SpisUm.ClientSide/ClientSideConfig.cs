using FluentValidation;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Concept;
using ISFG.SpisUm.ClientSide.Models.Document;
using ISFG.SpisUm.ClientSide.Models.File;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using ISFG.SpisUm.ClientSide.Models.Shredding;
using ISFG.SpisUm.ClientSide.Models.Signer;
using ISFG.SpisUm.ClientSide.Services;
using ISFG.SpisUm.ClientSide.Validators;
using ISFG.SpisUm.ClientSide.Validators.Concept;
using ISFG.SpisUm.ClientSide.Validators.Shipments;
using ISFG.SpisUm.ClientSide.Validators.Shredding;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.SpisUm.ClientSide
{
    public static class ClientSideConfig
    {
        #region Static Methods

        public static void AddClientSide(this IServiceCollection services)
        {
            services.AddSingleton<ISimpleMemoryCache, SimpleMemoryCache>();

            // Services
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<INodesService, NodesService>();
            services.AddScoped<IEmailDataBoxService, EmailDataBoxService>();
            services.AddScoped<ITransactionHistoryService, TransactionHistoryService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IUsersService, UserService>();
            services.AddScoped<IShipmentsService, ShipmentsService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IConceptService, ConceptService>();
            services.AddScoped<IComponentService, ComponentService>();
            services.AddScoped<IPersonService, PersonService>();
            services.AddScoped<ISignerService, SignerService>();            
            services.AddScoped<IShreddingService, ShreddingService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IDataBoxService, DataBoxService>();
            services.AddTransient<ISystemLoginService, SystemLoginService>();

            //Signer
            services.AddTransient<IValidator<SignerCreate>, SignerCreateValidator>();
            services.AddTransient<IValidator<SignerGetStatus>, SignerGetStatusValidator>();
            
            // Concept Validators
            services.AddTransient<IValidator<ConceptCancel>, ConceptCancelValidator>();
            services.AddTransient<IValidator<ConceptRecover>, ConceptRecoverValidator>();
            services.AddTransient<IValidator<ConceptCreate>, ConceptCreateValidator>();
            services.AddTransient<IValidator<ConceptToDocument>, ConceptToDocumentValidator>();
            services.AddTransient<IValidator<ConceptComponentCreate>, ConceptComponentCreateValidator>();
            services.AddTransient<IValidator<ConceptComponentUpdateContent>, ConceptComponentUpdateContentValidator>();
            services.AddTransient<IValidator<ConceptComponentCancel>, ConceptComponentCancelValidator>();
            services.AddTransient<IValidator<ConceptRevert>, ConceptRevertValidator>();

            // Document Validators
            services.AddTransient<IValidator<NodeUpdate>, NodeUpdateValidator>();
            services.AddTransient<IValidator<ComponentUpdate>, ComponentUpdateValidator>();
            services.AddTransient<IValidator<DocumentOwnerHandOver>, DocumentOwnerHandOverValidator>();
            services.AddTransient<IValidator<DocumentOwnerDecline>, DocumentOwnerDeclineValidator>();
            services.AddTransient<IValidator<DocumentOwnerCancel>, DocumentOwnerCancelValidator>();
            services.AddTransient<IValidator<DocumentOwnerAccept>, DocumentOwnerAcceptValidator>();
            services.AddTransient<IValidator<DocumentCancel>, DocumentCancelValidator>();
            services.AddTransient<IValidator<DocumentCreate>, DocumentCreateValidator>();
            services.AddTransient<IValidator<DocumentComponentCreate>, DocumentComponentCreateValidator>();
            services.AddTransient<IValidator<DocumentComponentDelete>, DocumentComponentDeleteValidator>();
            services.AddTransient<IValidator<DocumentComponentUpdateContent>, DocumentComponentUpdateContentValidator>();
            services.AddTransient<IValidator<DocumentFavouriteAdd>, DocumentFavouriteAddValidator>();
            services.AddTransient<IValidator<DocumentFavouriteRemove>, DocumentFavouriteRemoveValidator>();
            services.AddTransient<IValidator<DocumentForSignature>, DocumentForSignatureValidator>();
            services.AddTransient<IValidator<DocumentRecover>, DocumentRecoverValidator>();
            services.AddTransient<IValidator<DocumentLostDestroyed>, DocumentLostDestroyedValidator>();
            services.AddTransient<IValidator<DocumentFound>, DocumentFoundValidator>();
            services.AddTransient<IValidator<DocumentSettle>, DocumentSettleValidator>();
            services.AddTransient<IValidator<DocumentSettleCancel>, DocumentSettleCancelValidator>();
            services.AddTransient<IValidator<DocumentProperties>, DocumentPropertiesValidator>();
            services.AddTransient<IValidator<DocumentToRepository>, DocumentToRepositoryValidator>();            
            services.AddTransient<IValidator<DocumentRevert>, DocumentRevertValidator>();
            services.AddTransient<IValidator<DocumentReturnForRework>, DocumentReturnForReworkValidator>();
            services.AddTransient<IValidator<DocumentChangeLocation>, DocumentChangeLocationValidator>();
            services.AddTransient<IValidator<DocumentChangeFileMark>, DocumentChangeFileMarkValidator>();
            services.AddTransient<IValidator<DocumentBorrow>, DocumentBorrowValidator>();
            services.AddTransient<IValidator<DocumentReturn>, DocumentReturnValidator>();
            services.AddTransient<IValidator<DocumentFromSignature>, DocumentFromSignatureValidator>();
            services.AddTransient<IValidator<DocumentShreddingA>, DocumentShreddingAValidator>();
            services.AddTransient<IValidator<DocumentShreddingS>, DocumentShreddingSValidator>();
            services.AddTransient<IValidator<DocumentShreddingDiscard>, DocumentShreddingDiscardValidator>();
            services.AddTransient<IValidator<DocumentComponentOutputFormat>, DocumentComponentOutputFormatValidator>();

            // File Validators
            services.AddTransient<IValidator<FileDocumentAdd>, FileDocumentAddValidator>();
            services.AddTransient<IValidator<FileCreate>, FileCreateValidator>();
            services.AddTransient<IValidator<FileCancel>, FileCancelValidator>();
            services.AddTransient<IValidator<FileFavouriteAdd>, FileFavouriteAddValidator>();
            services.AddTransient<IValidator<FileFavouriteRemove>, FileFavouriteRemoveValidator>();
            services.AddTransient<IValidator<FileRecover>, FileRecoverValidator>();
            services.AddTransient<IValidator<FileLostDestroyed>, FileLostDestroyedValidator>();
            services.AddTransient<IValidator<FileFound>, FileFoundValidator>();
            services.AddTransient<IValidator<FileOpen>, FileOpenValidator>();
            services.AddTransient<IValidator<FileClose>, FileCloseValidator>();
            services.AddTransient<IValidator<FileToRepository>, FileToRepositoryValidator>();
            services.AddTransient<IValidator<FileChangeLocation>, FileChangeLocationValidator>();
            services.AddTransient<IValidator<FileChangeFileMark>, FileChangeFileMarkValidator>();
            services.AddTransient<IValidator<FileBorrow>, FileBorrowValidator>();
            services.AddTransient<IValidator<FileReturn>, FileReturnValidator>();
            services.AddTransient<IValidator<FileShreddingA>, FileShreddingAValidator>();
            services.AddTransient<IValidator<FileShreddingS>, FileShreddingSValidator>();
            services.AddTransient<IValidator<FileShreddingDiscard>, FileShreddingDiscardValidator>();
            services.AddTransient<IValidator<FileShreddingCancelDiscard>, FileShreddingCancelDiscardValidator>();

            // Shipments validators
            services.AddTransient<IValidator<DocumentShipmentSend>, DocumentShipmentSendValidator>();
            services.AddTransient<IValidator<FileShipmentSend>, FileShipmentSendValidator>();
            services.AddTransient<IValidator<ShipmentCreateEmail>, ShipmentCreateEmailValidator>();
            services.AddTransient<IValidator<ShipmentCreateDataBox>, ShipmentCreateDataBoxValidator>();
            services.AddTransient<IValidator<ShipmentCreatePersonally>, ShipmentCreatePersonallyValidator>();
            services.AddTransient<IValidator<ShipmentCreatePublish>, ShipmentCreatePublishValidator>();
            services.AddTransient<IValidator<ShipmentCreatePost>, ShipmentCreatePostValidator>();
            services.AddTransient<IValidator<ShipmentCancel>, ShipmentCancelValidator>();
            services.AddTransient<IValidator<ShipmentUpdateEmail>, ShipmentUpdateEmailValidator>();
            services.AddTransient<IValidator<ShipmentUpdateDataBox>, ShipmentUpdateDataBoxValidator>();
            services.AddTransient<IValidator<ShipmentUpdatePersonally>, ShipmentUpdatePersonallyValidator>();
            services.AddTransient<IValidator<ShipmentUpdatePost>, ShipmentUpdatePostValidator>();
            services.AddTransient<IValidator<ShipmentUpdatePublish>, ShipmentUpdatePublishValidator>();
            services.AddTransient<IValidator<ShipmentFileCreatePost>, ShipmentFileCreatePostValidator>();
            services.AddTransient<IValidator<ShipmentFileCreatePersonally>, ShipmentFileCreatePersonallyValidator>();
            services.AddTransient<IValidator<ShipmentResend>, ShipmentResendValidator>();
            services.AddTransient<IValidator<ShipmentReturn>, ShipmentReturnValidator>();
            services.AddTransient<IValidator<ShipmentDispatchPost>, ShipmentDispatchPostValidator>();
            services.AddTransient<IValidator<ShipmentDispatchPublish>, ShipmentDispatchPublishValidator>();

            // Shredding validators
            services.AddTransient<IValidator<ShreddingProposalCreate>, ShreddingProposalCreateValidator>();

            // Other Validators
            services.AddTransient<IValidator<CreateGroup>, GroupValidator>();
            services.AddTransient<IValidator<UserCreate>, UserCreateValidator>();
        }

        #endregion
    }
}