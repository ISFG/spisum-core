using System;
using ISFG.SpisUm.Jobs.Interfaces;

namespace ISFG.SpisUm.Jobs.Models
{
    public class ScheduleConfig<T> : IScheduleConfig<T>
    {
        #region Implementation of IScheduleConfig<T>

        public string CronExpression { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }

        #endregion
    }
}