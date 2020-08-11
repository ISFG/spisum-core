using ISFG.SpisUm.Jobs.Configuration;

namespace ISFG.SpisUm.Jobs.Interfaces
{
    public interface ITransactionHistoryConfiguration
    {
        #region Properties

        ScheduleConfiguration Schedule { get; }
        string Name { get; }
        string Originator { get; }
        string Address { get; }

        #endregion
    }
}