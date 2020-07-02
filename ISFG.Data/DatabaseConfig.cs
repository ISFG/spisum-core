using ISFG.Data.Database;
using ISFG.Data.Interfaces;
using ISFG.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.Data
{
    public static class DatabaseConfig
    {
        #region Static Methods

        public static void AddDatabase(this IServiceCollection services, IDataConfiguration configuration)
        {
            services.AddSingleton(configuration);
            
            services.AddDbContext<SpisumContext>(options =>
            {
                options.UseNpgsql(configuration.Connection);
                //options.EnableDetailedErrors();
            });

            services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();
            services.AddScoped<ISystemLoginRepository, SystemLoginRepository>();
        }

        #endregion
    }
}