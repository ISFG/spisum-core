using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EntityFrameworkPaginateCore;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Data.Interfaces;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.Translations.Infrastructure;
using Newtonsoft.Json;

namespace ISFG.SpisUm.ClientSide.Services
{

    public class AuditLogService : IAuditLogService
    {
        #region Fields

        private readonly IAlfrescoModelComparer _alfrescoModelComparer;
        private readonly IIdentityUser _identityUser;

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly IMapper _mapper;
        private readonly ITransactionHistoryRepository _transactionHistoryRepository;
        private readonly ITranslateService _translateService;

        #endregion

        #region Constructors

        public AuditLogService(
            IIdentityUser identityUser, 
            ITransactionHistoryRepository transactionHistoryRepository,
            IAlfrescoModelComparer alfrescoModelComparer, 
            IMapper mapper,
            ITranslateService translationService
        )
        {
            _identityUser = identityUser;
            _transactionHistoryRepository = transactionHistoryRepository;
            _alfrescoModelComparer = alfrescoModelComparer;
            _mapper = mapper;
            _translateService = translationService;
        }

        #endregion

        #region Implementation of IAuditLogService

        public async Task<TransactionHistoryPage<TransactionHistory>> GetEvents(TransactionHistoryQuery transactionHistoryQuery)
        {
            if (transactionHistoryQuery.From != null) transactionHistoryQuery.From = transactionHistoryQuery.From.Value.ToUniversalTime();
            if (transactionHistoryQuery.To != null) transactionHistoryQuery.To = transactionHistoryQuery.To.Value.ToUniversalTime();            
            
            var sorts = new Sorts<Data.Models.TransactionHistory>();
            sorts.Add(true, x => x.OccuredAt, true);
            
            var filters = new Filters<Data.Models.TransactionHistory>();
            filters.Add(!string.IsNullOrEmpty(transactionHistoryQuery.NodeId), x => x.NodeId == transactionHistoryQuery.NodeId);
            filters.Add(transactionHistoryQuery.NodeIds != null && transactionHistoryQuery.NodeIds.Any(), x => transactionHistoryQuery.NodeIds.Contains(x.NodeId));
            filters.Add(transactionHistoryQuery.From != null, x => x.OccuredAt > transactionHistoryQuery.From);
            filters.Add(transactionHistoryQuery.To != null, x => x.OccuredAt < transactionHistoryQuery.To);

            var result = await _transactionHistoryRepository.GetTransactionHistory(transactionHistoryQuery.CurrentPage, transactionHistoryQuery.PageSize, sorts, filters);

            return _mapper.Map<TransactionHistoryPage<TransactionHistory>>(result);
        }

        public async Task Record(string nodeId, string nodeType, string pid, string nodeTypeCode, string eventType, string message = null)
        {
            await LocalRecord(nodeId, nodeType, pid, nodeTypeCode, eventType, 
                message != null ? JsonConvert.SerializeObject(new TransactionHistoryParameters(message), _jsonSettings) : null);
        }

        public async Task Record(string nodeId, string nodeType, string pid, string nodeTypeCode, string eventType, Dictionary<string, object> a, Dictionary<string, object> b, string message = null)
        {
            var difference = _alfrescoModelComparer.CompareProperties(a, b);

            if (!string.IsNullOrWhiteSpace(message))
                await difference.ForEachAsync(async x =>
                {
                    var translation = await _translateService.Translate(x.Key, nodeType , "title");

                    if (translation != null)
                        message = message.Replace(x.Key, translation);

                    // CodeList translation
                    var translationCodeListOldValue = await _translateService.Translate(x.Key, "codelist", x.OldValue?.ToString());
                    var translationCodeListNewValue = await _translateService.Translate(x.Key, "codelist", x.NewValue?.ToString());

                    if (translationCodeListOldValue != null)
                        message = message.Replace(x.OldValue?.ToString(), translationCodeListOldValue);

                    if (translationCodeListNewValue != null)
                        message = message.Replace(x.NewValue?.ToString(), translationCodeListNewValue);
                });

            await LocalRecord(nodeId, nodeType, pid, nodeTypeCode, eventType, 
                JsonConvert.SerializeObject(
                    difference.Any() ? 
                        message != null ? 
                            new TransactionHistoryParameters(message, difference) : 
                            new TransactionHistoryParameters(difference) : 
                        message != null ? 
                            new TransactionHistoryParameters(message):
                            null, _jsonSettings));
        }

        #endregion

        #region Private Methods

        private async Task LocalRecord(string nodeId, string nodeType, string pid, string nodeTypeCode, string eventType, string eventParameters)
        {
            await _transactionHistoryRepository.CreateEvent(nodeId, nodeType, pid, nodeTypeCode, eventType,_identityUser.Id, _identityUser.RequestGroup, "SpisUm", eventParameters);
        }

        #endregion
    }
}