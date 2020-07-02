using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Common.Exceptions;
using ISFG.Pdf.Interfaces;
using ISFG.Signer.Client.Interfaces;
using ISFG.SpisUm.Jobs.Interfaces;
using ISFG.SpisUm.Jobs.Models;
using ISFG.SpisUm.Jobs.Services;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.Jobs.Jobs
{
    public class AuditLogThumbprintJob : CronJobService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IPdfService _pdfService;
        private readonly ISignerClient _signerClient;
        private readonly ISignerConfiguration _signerConfiguration;
        private readonly ITransactionHistoryConfiguration _transactionHistoryConfiguration;
        private readonly ITransformTransactionHistory _transformTransactionHistory;

        #endregion

        #region Constructors

        public AuditLogThumbprintJob(
            IScheduleConfig<AuditLogThumbprintJob> config,
            ISignerClient signerClient, 
            ITransformTransactionHistory transformTransactionHistory, 
            IPdfService pdfService, 
            IAlfrescoHttpClient alfrescoHttpClient,
            ISignerConfiguration signerConfiguration,
            ITransactionHistoryConfiguration transactionHistoryConfiguration) : base(config.CronExpression, config.TimeZoneInfo)
        {
            _signerClient = signerClient;
            _transformTransactionHistory = transformTransactionHistory;
            _pdfService = pdfService;
            _alfrescoHttpClient = alfrescoHttpClient;
            _transactionHistoryConfiguration = transactionHistoryConfiguration;
            _signerConfiguration = signerConfiguration;
        }

        #endregion

        #region Override of CronJobService

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            Log.Information($"{nameof(AuditLogThumbprintJob)} {DateTime.Now:HH:mm:ss} is running the Job.");

            var transactionHistoryDate = DateTime.Now.AddDays(-1);
            
            try
            {
                var pdfModel = await _transformTransactionHistory.ToPdfModel(transactionHistoryDate);
                var dailyFingerPrintPdf = await _pdfService.GenerateTransactionHistory(pdfModel);

                if (_signerConfiguration.Base != null || _signerConfiguration.Url != null)
                    dailyFingerPrintPdf = (await _signerClient.Seal(dailyFingerPrintPdf)).Output;

                await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root,
                    new FormDataParam(dailyFingerPrintPdf, string.Format(JobsNames.DailyFingerPrintPdfName, transactionHistoryDate.ToString("yyyy-MM-dd"), _transactionHistoryConfiguration.Originator)),
                    ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.RelativePath, JobsNames.DailyFingerPrintPath, ParameterType.GetOrPost)));
            }
            catch (Exception ex) when (ex is HttpClientException httpClientException && httpClientException.HttpStatusCode == HttpStatusCode.Conflict)
            {
                Log.Warning($"Fingerprint for {transactionHistoryDate:dd.MM.yyyy} already exists.");
            }
            catch (Exception ex)
            {
                Log.Error($"Couldn't create daily fingerprint. Message:{ex.Message}, StackTrace:{ex.StackTrace}");
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information($"{nameof(AuditLogThumbprintJob)} has started.");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information($"{nameof(AuditLogThumbprintJob)} has stopped.");
            return base.StopAsync(cancellationToken);
        }

        #endregion
    }
}