using System;
using ISFG.SpisUm.Jobs.Configuration;
using ISFG.SpisUm.Jobs.Interfaces;
using ISFG.SpisUm.Jobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.SpisUm.Jobs.Extension
{
    public static class CronJobExt
    {
        #region Static Methods

        public static IServiceCollection AddCronJob<T>(this IServiceCollection services, Action<IScheduleConfig<T>> options) where T : CronJobService
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options), @"Please provide Schedule Configurations.");

            var config = new ScheduleConfig<T>();
            options.Invoke(config);
            
            if (string.IsNullOrWhiteSpace(config.CronExpression))
                throw new ArgumentNullException(nameof(ScheduleConfig<T>.CronExpression), @"Empty Cron Expression is not allowed.");

            services.AddSingleton<IScheduleConfig<T>>(config);
            services.AddHostedService<T>();
            
            return services;
        }

        #endregion
    }
}