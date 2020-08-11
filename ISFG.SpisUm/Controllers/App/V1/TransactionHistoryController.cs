using System.Threading.Tasks;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models.TransactionHistory;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/transactionhistory")]
    public class TransactionHistoryController : ControllerBase
    {
        #region Fields

        private readonly IAuditLogService _auditLogService;

        #endregion

        #region Constructors

        public TransactionHistoryController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get transaction history
        /// </summary>
        [HttpGet("transaction-history")]
        public async Task<TransactionHistoryPage<TransactionHistory>> GetTransactionHistory([FromQuery] TransactionHistoryQuery transactionHistoryQuery)
        {
            return await _auditLogService.GetEvents(transactionHistoryQuery);
        }

        #endregion
    }
}