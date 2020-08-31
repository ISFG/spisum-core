using System;
using System.Collections.Generic;

namespace ISFG.Alfresco.Api.Models
{
    public static class TransactinoHistoryMessages
    {
        #region Fields

        public static string ConceptCreate = "Založení konceptu.";
        public static string ConceptUpdate = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string ConceptComponentUpdateDocument = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string ConceptComponentUpdateFile = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string ConceptComponentCreate = "Přiložení komponenty ke konceptu.";
        public static string ConceptComponentCreateFile = "Přiložení komponenty ke konceptu.";
        public static string ConceptComponentDownloadDocument = "Stažení komponenty.";
        public static string ConceptComponentDownloadFile = "Stažení komponenty.";
        public static string ConceptComponentGetContentDocument = "Zobrazení náhledu komponenty.";
        public static string ConceptComponentGetContentFile = "Zobrazení náhledu komponenty.";
        public static string ConceptComponentPostContentDocument = "Nahrání nové verze komponenty.";
        public static string ConceptComponentPostContentFile = "Nahrání nové verze komponenty.";
        public static string ConceptComponentDeleteDocument = "Odebrání komponenty od konceptu.";
        public static string Concept = "Zobrazení detailu konceptu.";
        public static string ConceptToDocument = "Povýšení konceptu {0} na dokument.";
        public static string ConceptRevert = "Vznik verze {0} obnovením verze {1} .";

        public static string ShipmentEmailCreate = "Založení zásilky emailem.";
        public static string ShipmentPostCreate = "Založení zásilky poštou.";
        public static string ShipmentPublishCreate = "Založení zásilky zveřejněním.";
        public static string ShipmentDataBoxCreate = "Založení zásilky datovou schránkou.";
        public static string ShipmentPersonallyCreate = "Založení zásilky osobním převzetím.";
        public static string ShipmentEmailUpdate = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string ShipmentPostUpdate = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string ShipmentPublishUpdate = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string ShipmentDataBoxUpdate = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string ShipmentPersonallyUpdate = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string ShipmentCancel = "Odstranění zásilky.";
        public static string ShipmentDispatchPost = "Vypravení zásilky.";
        public static string ShipmentDispatchPublish = "Vypravení zásilky.";
        public static string ShipmentDeliveredPublish = "Doručení zásilky.";
        public static string ShipmentSendPostPublish = "Předání zásilky do výpravny.";
        public static string ShipmentSendPersonallyToDispatch = "Předání zásilky do výpravny.";
        public static string ShipmentSendPersonallyDispatched = "Vypravení zásilky.";
        public static string ShipmentSendPersonallyDelivered = "Doručení zásilky.";
        public static string ShipmentSendEmailDataBoxToDispatch = "Předání zásilky do výpravny.";
        public static string ShipmentSendEmailDataBoxDispatched = "Vypravení zásilky.";
        public static string ShipmentSendEmailDataBoxFailed = "Neúspěšné vypravení zásilky.";
        public static string ShipmentReturn = "Vrácení zásilky z výpravny z důvodu {0}.";
        public static string ShipmentResend = "Předání zásilky do výpravny.";

        public static string Document = "Zobrazení detailu dokumentu.";
        public static string DocumentSignComponent = "Podepsání komponenty.";
        public static string DocumentSignComponentWithVisual = "Podepsání komponenty s vizualizací podpisu.";
        public static string DocumentSignComponentWithoutVisual = "Podepsání komponenty bez vizualizace podpisu.";
        public static string DocumentCreate = "Založení dokumentu.";
        public static string DocumentSettle = "Vyřízení dokumentu způsobem \"{0}\".";
        public static string DocumentRevert = "Vznik verze {0} obnovením verze {1}.";
        public static string DocumentSettleMethodOther = "Vyřízení dokumentu způsobem \"{0}\". Obsahem vyřízení je \"{1}\" z důvodu \"{2}\".";
        public static string DocumentSettleCancel = "Zrušení vyřízení dokumentu z důvodu \"{0}\".";
        public static string DocumentChangeLocation = "Změna úložného místa z \"{0}\" na \"{1}\".";
        public static string DocumentToRepository = "Dokument předán do spisovny.";
        public static string DocumentBorrow = "Zapůjčení dokumentu uživateli {0}, {1}.";
        public static string DocumentReturn = "Vrácení zapůjčeného dokumentu do spisovny.";
        public static string DocumentLostDestroy = "Ztráta/zničení dokumentu z důvodu \"{0}\".";
        public static string DocumentChangeRetentionMark = "Změna skartačního znaku z \"{0}\" na \"{1}\".";
        public static string DocumentChangeRetentionMarkFile = "Změna skartačního znaku z \"{0}\" na \"{1}\".";
        public static string DocumentChangeFileMark = "Změna spisového znaku z \"{0}\" na \"{1}\". " + Environment.NewLine + "Změna skartačního režimu z \"{2}\" na \"{3}\". ";
        public static string DocumentFavoriteAdd = "Označení dokumentu jako oblíbený.";
        public static string DocumentFavoriteRemove = "Zrušení označení dokumentu jako oblíbený.";
        public static string DocumentRecover = "Zrušení storna dokumentu z důvodu \"{0}\".";
        public static string ConceptRecover = "Zrušení storna konceptu z důvodu \"{0}\".";
        public static string FileRecover = "Zrušení storna spisu z důvodu \"{0}\".";
        public static string DocumentFound = "Nalezení dokumentu.";
        public static string FileFound = "Nalezení spisu.";
        public static string DocumentShreddingDiscard = "Vyřazení dokumentu ze skartačního řízení do {0} z důvodu {1}.";
        public static string FileShreddingDiscard = "Vyřazení spisu ze skartačního řízení do {0} z důvodu {1}.";
        public static string DocumentShreddingDiscardCancel = "Zrušení vyřazení dokumentu ze skartačního řízení.";
        public static string FileChangeFileMark = "Změna spisového znaku z \"{0}\" na \"{1}\". " + Environment.NewLine + "Změna skartačního režimu z \"{2}\" na \"{3}\". ";

        public static string File = "Zobrazení detailu spisu.";
        public static string FileCreate = "Založení spisu.";
        public static string FileCreateAddDocument = "Vložení dokumentu do spisu {0}.";
        public static string FileCreateAddFile = "Vložení dokumentu {0} do spisu.";
        public static string FileShreddingDiscardCancel = "Zrušení vyřazení spisu ze skartačního řízení.";
        public static string FileDocumentAddDocumentDocument = "Vložení dokumentu do spisu {0}.";
        public static string FileDocumentAddDocumentFile = "Vložení dokumentu {0} do spisu.";
        public static string FileDocumentRemoveDocumentDocument = "Vyjmutí dokumentu ze spisu {0}.";
        public static string FileDocumentRemoveDocumentFile = "Vyjmutí dokumentu {0} ze spisu.";
        public static string FileToRepository = "Spis předán do spisovny.";
        public static string FileShreddingChange = "Změna skartačního znaku z \"{0}\" na \"{1}\".";
        public static string FileOpen = "Otevtření spisu z důvodu \"{0}\".";
        public static string FileClose = "Spis vyřízen způsobem {0}. Obsahem vyřízení je {1} z důvodu {2}.";
        public static string FileCloseSettle = "Vyřízení spisu způsobem \"{0}\".";
        public static string FileCloseSettleOther = "Vyřízení spisu způsobem \"{0}\". Obsahem vyřízení je \"{1}\" z důvodu \"{2}\".";
        public static string FileFavoriteAdd = "Označení spisu jako oblíbený.";
        public static string FileFavoriteRemove = "Zrušení označení spisu jako oblíbený.";
        public static string FileUpdate = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string FileChangeLocation = "Změna úložného místa z \"{0}\" na \"{1}\".";
        public static string FileBorrow = "Zapůjčení spisu uživateli {0}, {1}.";
        public static string FileReturn = "Vrácení zapůjčeného spisu do spisovny.";
        public static string FileLostDestroy = "Ztráta/zničení spisu z důvodu \"{0}\".";

        public static string DocumentForSignature = "Předání dokumentu k podpisu uživateli {0}, {1}.";
        public static string DocumentReturnForRework = "Vrácení dokumentu k přepracování uživateli {0}, {1}.";
        public static string DocumentFromSignature = "Vrácení dokumentu uživateli {0}, {1}.";

        public static string DocumentHandover = "Předání dokumentu uživateli {0}, {1}.";
        public static string ConceptHandover = "Předání konceptu uživateli {0}, {1}.";
        public static string FileHandover = "Předání spisu uživateli {0}, {1}.";

        public static string DocumentHandoverGroup = "Předání dokumentu na skupinu {0}.";
        public static string ConceptHandoverGroup = "Předání konceptu na skupinu {0}.";
        public static string FileHandoverGroup = "Předání spisu na skupinu {0}.";

        public static string DocumentHandoverAccept = "Převzetí dokumentu.";
        public static string ConceptHandoverAccept = "Převzetí konceptu.";
        public static string FileHandoverAccept = "Převzetí spisu.";

        public static string DocumentHandoverDecline = "Odmítnutí převzetí dokumentu.";
        public static string ConceptHandoverDecline = "Odmítnutí převzetí konceptu.";
        public static string FileHandoverDecline = "Odmítnutí převzetí spisu.";

        public static string DocumentHandoverCancel = "Zrušení předání dokumentu.";
        public static string ConceptHandoverCancel = "Zrušení předání konceptu.";
        public static string FileHandoverCancel = "Zrušení předání spisu.";

        public static string DocumentHandoverRepositoryAccept = "Převzetí dokumentu do spisovny.";
        public static string ConceptHandoverRepositoryAccept = "Převzetí konceptu do spisovny.";
        public static string FileHandoverRepositoryAccept = "Převzetí spisu do spisovny.";

        public static string DocumentComponentCreateComponent = "Přiložení komponenty k dokumentu {0}.";
        public static string DocumentComponentCreateDocument = "Přiložení komponenty k dokumentu.";
        public static string DocumentComponentCreateFile = "Přiložení komponenty k dokumentu {0}.";

        public static string DocumentComponentDeleteComponent = "Odebrání komponenty od dokumentu.";
        public static string DocumentComponentDeleteDocument = "Odebrání komponenty od dokumentu.";
        public static string DocumentComponentDeleteFile = "Odebrání komponenty od dokumentu {0}.";

        public static string DocumentComponentGetContentComponent = "Zobrazení náhledu komponenty.";
        public static string DocumentComponentGetContentDocument = "Zobrazení náhledu komponenty.";
        public static string DocumentComponentGetContentFile = "Zobrazení náhledu komponenty.";

        public static string DocumentComponentPostContentComponent = "Nahrání nové verze komponenty.";
        public static string DocumentComponentPostContentDocument = "Nahrání nové verze komponenty.";
        public static string DocumentComponentPostContentFile = "Nahrání nové verze komponenty.";

        public static string DocumentComponentDownloadComponent = "Stažení komponenty.";
        public static string DocumentComponentDownloadDocument = "Stažení komponenty.";
        public static string DocumentComponentDownloadFile = "Stažení komponenty.";

        public static string ConceptCancel = "Storno konceptu z důvodu \"{0}\".";
        public static string DocumentCancel = "Storno dokumentu z důvodu \"{0}\".";
        public static string FileCancel = "Storno spisu z důvodu \"{0}\".";

        public static string DocumentRegister = "Zaevidování dokumentu.";

        public static string DocumentUpdateDocument = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string DocumentUpdateFile = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";

        public static string DocumentComponentUpdateComponent = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string DocumentComponentUpdateDocument = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";
        public static string DocumentComponentUpdateFile = "Změna hodnoty pole {0} z \"{1}\" na \"{2}\". ";

        #endregion

        #region Static Methods

        public static string GetMessagePropertiesChange(string text, List<ObjectDifference> properties)
        {
            string message = string.Empty;

            bool addNewLine = false;

            properties.ForEach(x =>
            {
                if (addNewLine) 
                    message += Environment.NewLine;

                message += string.Format(text, x.Key, x.OldValue, x.NewValue);

                if (!addNewLine) 
                    addNewLine = true;
            });

            return message;
        }

        #endregion
    }
}
