using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ISFG.SpisUm.Jobs
{
    public class Program
    {
        #region Static Methods

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += AppUnhandledException;
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", true, true)
                    .AddEnvironmentVariables();
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                logging.AddSerilog();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(hostContext.Configuration)
                    .CreateLogger();
            })
            .ConfigureAppConfiguration(hostBuilder =>
            {
                string logPath = Environment.GetEnvironmentVariable("ASPNETCORE_LOG");
                if (logPath == null)
                    Environment.SetEnvironmentVariable("ASPNETCORE_LOG",
                        Path.Combine(AppContext.BaseDirectory, "Logs", "log-jobs.txt"));
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                    webBuilder.UseStartup<Startup>();
            });

        private static void AppUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Log.Logger == null || !(e.ExceptionObject is Exception ex)) 
                return;

            Log.Logger?.Error(ex, "SpisUm Jobs crashed");

            if(e.IsTerminating)
                Log.CloseAndFlush();
        }

        #endregion
    }
}
