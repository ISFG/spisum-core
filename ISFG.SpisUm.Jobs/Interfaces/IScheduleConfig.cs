using System;

namespace ISFG.SpisUm.Jobs.Interfaces
{
    public interface IScheduleConfig<T>
    {
        #region Properties

        string CronExpression { get; set; }
        TimeZoneInfo TimeZoneInfo { get; set; }

        #endregion
    }
}