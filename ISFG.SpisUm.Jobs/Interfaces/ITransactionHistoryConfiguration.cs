namespace ISFG.SpisUm.Jobs.Interfaces
{
    public interface ITransactionHistoryConfiguration
    {
        #region Properties

        string CronExpression { get; }
        string Name { get; }
        string Originator { get; }
        string Address { get; }

        #endregion
    }
}