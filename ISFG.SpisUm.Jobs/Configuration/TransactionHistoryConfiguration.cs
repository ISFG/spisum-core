using ISFG.Common.Attributes;
using ISFG.SpisUm.Jobs.Interfaces;

namespace ISFG.SpisUm.Jobs.Configuration
{
    [Settings("TransactionHistory")]
    public class TransactionHistoryConfiguration : ITransactionHistoryConfiguration
    {
        #region Implementation of ITransactionHistoryConfiguration

        public string Address { get; set; }
        public string CronExpression { get; set; }
        public string Name { get; set; }
        public string Originator { get; set; }

        #endregion
    }
}