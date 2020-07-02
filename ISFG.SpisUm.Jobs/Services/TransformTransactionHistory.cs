using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Data.Interfaces;
using ISFG.Pdf.Models;
using ISFG.SpisUm.Jobs.Interfaces;
using ISFG.SpisUm.Jobs.Models;
using Newtonsoft.Json;
using RestSharp;

namespace ISFG.SpisUm.Jobs.Services
{
    public class TransformTransactionHistory : ITransformTransactionHistory
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly ITransactionHistoryConfiguration _transactionHistoryConfiguration;
        private readonly ITransactionHistoryRepository _transactionHistoryRepository;

        #endregion

        #region Constructors

        public TransformTransactionHistory(
            ITransactionHistoryRepository transactionHistoryRepository, 
            IAlfrescoHttpClient alfrescoHttpClient, 
            ITransactionHistoryConfiguration transactionHistoryConfiguration)
        {
            _transactionHistoryRepository = transactionHistoryRepository;
            _alfrescoHttpClient = alfrescoHttpClient;
            _transactionHistoryConfiguration = transactionHistoryConfiguration;
        }

        #endregion

        #region Implementation of ITransformTransactionHistory

        public async Task<TransactionHistoryPdf> ToPdfModel(DateTime currentDate)
        {
            var mainGroup = await _alfrescoHttpClient.GetGroupMembers(JobsNames.MainGroup,
                ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));
            
            var pdfCount = await _alfrescoHttpClient.GetNodeChildren(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, 1, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, JobsNames.DailyFingerPrintPath, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(nodeType='{AlfrescoNames.ContentModel.Content}')", ParameterType.QueryString)));

            var transactionHistory = await _transactionHistoryRepository.GetTransactionHistoryByDate(currentDate.Date, currentDate.Date);
            var pdfCounter = pdfCount?.List?.Pagination?.TotalItems.HasValue == true ? pdfCount.List.Pagination.TotalItems.Value : 1;
            
            TransactionHistoryPdf transactionHistoryObj = new TransactionHistoryPdf
            {
                Header = string.Format(PdfTranslations.PageHeader, _transactionHistoryConfiguration?.Originator, currentDate.ToString("dd.MM.yyyy")),
                Name = string.Format(PdfTranslations.FirstPage.Name, currentDate.ToString("dd.MM.yyyy")),
                Originator = string.Format(PdfTranslations.FirstPage.Originator, _transactionHistoryConfiguration?.Originator),
                Address = string.Format(PdfTranslations.FirstPage.Address, _transactionHistoryConfiguration?.Address),
                SerialNumber = string.Format(PdfTranslations.FirstPage.SerialNumber, ++pdfCounter),
                NumberOfPages = PdfTranslations.FirstPage.NumberOfPages,
                Rows = new TableRows
                {
                    Pid = PdfTranslations.Cells.Pid,
                    TypeOfObject = PdfTranslations.Cells.TypeOfObject,
                    TypeOfChanges = PdfTranslations.Cells.TypeOfChanges,
                    Descriptions = PdfTranslations.Cells.Descriptions,
                    Author = PdfTranslations.Cells.Author,
                    Date = PdfTranslations.Cells.Date
                },
                Columns = new List<TableColumns>()
            };
            
            foreach (var item in transactionHistory)
            {
                TransactionHistoryParameters eventParams = null;

                if (item.EventParameters != null)
                    eventParams = JsonConvert.DeserializeObject<TransactionHistoryParameters>(item.EventParameters);
                
                string requestGroup = mainGroup?.List?.Entries?.FirstOrDefault(u => u.Entry.Id == item.UserGroupId)?.Entry?.DisplayName ?? item.UserGroupId;

                if (eventParams != null)
                    transactionHistoryObj.Columns.Add(new TableColumns
                    {
                        Pid = item.Pid,
                        TypeOfObject = item.FkNodeTypeCodeNavigation.Code,
                        TypeOfChanges = item.FkEventTypeCodeNavigation.Code,
                        Descriptions = eventParams.Message,
                        Author = string.Join("; ", item.UserId, requestGroup),
                        Date = item.OccuredAt.ToString("dd.MM.yyyy hh:mm:ss")
                    });
                else
                    transactionHistoryObj.Columns.Add(new TableColumns
                    {
                        Pid = item.Pid,
                        TypeOfObject = item.FkNodeTypeCodeNavigation.Code,
                        TypeOfChanges = "",
                        Descriptions = item.FkEventTypeCodeNavigation.Code,
                        Author = string.Join(";", item.UserId, requestGroup),
                        Date = item.OccuredAt.ToString("dd.MM.yyyy hh:mm:ss")
                    });
            }            
            
            return transactionHistoryObj;
        }

        #endregion
    }
}              