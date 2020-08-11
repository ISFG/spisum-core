using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Pdf.Interfaces;
using ISFG.Pdf.Models;
using ISFG.Pdf.Models.Clause;
using ISFG.Pdf.Models.ShreddingPlan;
using ISFG.Signer.Client.Interfaces;
using Newtonsoft.Json;
using Serilog;

namespace ISFG.Pdf.Services
{
    public class TransformService : ITransformService
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IIdentityUser _identityUser;
        private readonly ISignerClient _signerClient;

        #endregion

        #region Constructors

        public TransformService(
            IAlfrescoConfiguration alfrescoConfig,
            ISignerClient signerClient, 
            IIdentityUser identityUser)
        {
            _alfrescoConfig = alfrescoConfig;
            _signerClient = signerClient;
            _identityUser = identityUser;
        }

        #endregion

        #region Implementation of ITransformService

        public async Task<ClauseModel> Clause(NodeEntry nodeEntry, byte[] pdf)
        {
            var pdfValidation = await _signerClient.Validate(pdf);

            var result = new ClauseModel(PdfNames.Clause.Paragraph1, PdfNames.Clause.Paragraph2);

            result.OriginalFileFormat = PdfNames.Clause.OriginalFileFormat;
            result.OriginalFileFormatValue = Path.GetExtension(nodeEntry?.Entry?.Name);
            result.FilePrint = PdfNames.Clause.FilePrint;
            result.FilePrintValue = Hashes.Sha256CheckSum(new MemoryStream(pdf));
            result.UsedAlgorithm = PdfNames.Clause.UsedAlgorithm;
            result.UsedAlgorithmValue = "SHA-256";
            result.Organizer = PdfNames.Clause.Organizer;
            result.OrganizerValue = "ISFG"; //TODO: change this
            result.NameLastName = PdfNames.Clause.NameLastName;
            result.NameLastNameValue = $"{_identityUser.FirstName} {_identityUser.LastName} ({_identityUser.Id})";
            result.DateOfIssue = PdfNames.Clause.DateOfIssue;
            result.DateOfIssueValue = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            
            if (pdfValidation?.Report?.sigInfos == null || !pdfValidation.Report.sigInfos.Any())
                return result;

            foreach (var signInfo in pdfValidation.Report.sigInfos)
            {
                var certType = signInfo.eidasType;
                var certPublisher = Dn.Parse(signInfo?.signCert?.Issuer);
                var certHolder = Dn.Parse(signInfo?.signCert?.Subject);
                var certRevocation = signInfo?.crlPath?.FirstOrDefault();
                var certInfo = signInfo?.certPath?.FirstOrDefault();

                var timestamp = signInfo?.sigTimestamps?.FirstOrDefault();
                var timestampRevocation = timestamp?.crlPath?.FirstOrDefault();
                var timestampPublisher = Dn.Parse(timestamp?.signCert?.Issuer);

                if (certType == PdfNames.Clause.AdvancedSignature || certType == PdfNames.Clause.AcreditedSignature || certType == PdfNames.Clause.QualifiedSginature)
                {
                    result.Sign = GetSign(DateTime.Now, certRevocation?.ThisUpdate, signInfo?.signCert?.Serial, certPublisher.GetValue("CN"), certHolder.GetValue("CN"));
                    result.SignTimestamp = timestamp != null ? GetTimeStamp(timestampRevocation?.ThisUpdate, timestamp?.time, timestamp?.signCert?.Serial, timestampPublisher.GetValue("CN")) : null;
                }

                if (certType == PdfNames.Clause.AdvancedSeal || certType == PdfNames.Clause.TrustedSeal || certType == PdfNames.Clause.AcreditedSeal || certType == PdfNames.Clause.QualifiedSeal)
                {
                    result.Seal = GetSeal(DateTime.Now, certRevocation?.ThisUpdate, signInfo?.signCert?.Serial, certPublisher.GetValue("CN"), certHolder.GetValue("CN"));
                    result.SealTimestamp = timestamp != null ? GetTimeStamp(timestampRevocation?.ThisUpdate, timestamp?.time, timestamp?.signCert?.Serial, timestampPublisher.GetValue("CN")) : null;         
                }
                
                if (certType == PdfNames.Clause.Timestamp || certType == PdfNames.Clause.QualifiedTimestamp)
                    result.Timestamp = GetTimeStamp(certRevocation?.ThisUpdate, signInfo?.time, certInfo?.Serial, Dn.Parse(certInfo?.Issuer).GetValue("CN"));
            }
            
            return result;

            static string GetTimeStamp(DateTime? revocationThisUpdate, DateTime? time, string serial, string publisherName) =>
                string.Format(PdfNames.Clause.TimeStamp, 
                    revocationThisUpdate?.ToString("dd.MM.yyyy HH:mm:ss"), 
                    time?.ToString("dd.MM.yyyy HH:mm:ss"),
                    serial,
                    publisherName);

            static string GetSeal(DateTime now, DateTime? revocationThisUpdate, string serial, string publisherName, string holderName) =>
                string.Format(PdfNames.Clause.Seal, 
                    now.ToString("dd.MM.yyyy HH:mm:ss"), 
                    revocationThisUpdate?.ToString("dd.MM.yyyy HH:mm:ss"),
                    serial,
                    publisherName,
                    holderName);

            static string GetSign(DateTime now, DateTime? revocationThisUpdate, string serial, string publisherName, string holderName) =>
                string.Format(PdfNames.Clause.Sign, 
                    now.ToString("dd.MM.yyyy HH:mm:ss"), 
                    revocationThisUpdate?.ToString("dd.MM.yyyy HH:mm:ss"),
                    serial,
                    publisherName,
                    holderName);
        }

        public async Task<ShreddingPlan> ShreddingPlan(string id)
        {
            ShreddingPlanModel shreddingPlanById = await LoadShreddingPlanById(id);
            
            if (shreddingPlanById?.Id == null)
                return new ShreddingPlan();
            
            var shreddingPlan = new ShreddingPlan
            {
                Title = shreddingPlanById.Name,
                Rows = new ShreddingPlanRows
                {
                    FileMark = PdfNames.ShreddingPlan.FileMark,
                    FileMarkText = PdfNames.ShreddingPlan.DocumentType,
                    ShreddingMode = PdfNames.ShreddingPlan.ShreddingMode,
                },
                Columns = new List<ShreddingPlanColumns>()
            };

            var groupByParentFileMark = shreddingPlanById.Items.Where(x => x.ParentFileMark != null).GroupBy(x => x.ParentFileMark).OrderBy(x => x.Key);

            foreach (var parentFileMark in groupByParentFileMark)
            {
                var keyCaption = shreddingPlanById.Items.FirstOrDefault(x => x.FileMark == parentFileMark.Key && x.IsCaption != null && x.IsCaption == true);
                if (keyCaption != null)
                    shreddingPlan.Columns.Add(new ShreddingPlanColumns
                    {
                        FileMark = keyCaption.FileMark,
                        FileMarkText = keyCaption.SubjectGroup,
                        RetentionMark = keyCaption.RetentionMark,
                        Period = keyCaption.Period,
                        IsParent = true
                    });

                foreach (var fileMark in parentFileMark)
                    shreddingPlan.Columns.Add(new ShreddingPlanColumns
                    {
                        FileMark = fileMark.FileMark,
                        FileMarkText = fileMark.SubjectGroup,
                        RetentionMark = fileMark.RetentionMark,
                        Period = fileMark.Period,
                        IsParent = false
                    });
            }

            return shreddingPlan;
        }

        #endregion

        #region Private Methods

        private Task<ShreddingPlanModel> LoadShreddingPlanById(string id)
        {
            ShreddingPlanModel shreddingPlanById;
            
            try
            {
                var shreddingPlans = JsonConvert.DeserializeObject<List<ShreddingPlanModel>>(File.ReadAllText(_alfrescoConfig?.ShreddingPlan));
                shreddingPlanById = shreddingPlans.FirstOrDefault(x => x.Id == id);
            }
            catch
            {
                Log.Error($"Something went wrong during parsing '{nameof(_alfrescoConfig.ShreddingPlan)}' in '{nameof(TransformService)}'. You might want to check configuration file 'shreddingPlan.json'.");
                return null;
            }

            if (shreddingPlanById?.Items != null && shreddingPlanById.Items.Any())
                return Task.FromResult(shreddingPlanById);
            
            Log.Error($"Shredding plan '{id}' does not exists in configuration file 'shreddingPlan.json'.");
            return null;
        }

        #endregion
    }
}