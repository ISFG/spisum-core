using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ISFG.SpisUm
{
    public static class Program
    {
        #region Static Methods

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += AppUnhandledException;
            
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppEnvironmentVariables()
                .ConfigureAppConfiguration(ConfigConfiguration)
                .ConfigureLogging(LogConfiguration)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

        private static void ConfigConfiguration(HostBuilderContext context, IConfigurationBuilder config)
        {
            config.SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
                .AddEnvironmentVariables();
        }

        private static void LogConfiguration(HostBuilderContext hostContext, ILoggingBuilder logging)
        {
            logging.AddSerilog();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostContext.Configuration)
                .CreateLogger();
        }

        private static IHostBuilder ConfigureAppEnvironmentVariables(this IHostBuilder hostBuilder)
        {
            string logPath = Environment.GetEnvironmentVariable("ASPNETCORE_LOG");
            if (logPath == null)
                Environment.SetEnvironmentVariable("ASPNETCORE_LOG",
                    Path.Combine(AppContext.BaseDirectory, "Logs", "log.txt"));

            return hostBuilder;
        }

        private static void AppUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Log.Logger == null || !(e.ExceptionObject is Exception ex)) 
                return;

            Log.Logger?.Error(ex, "SpisUm crashed");

            if(e.IsTerminating)
                Log.CloseAndFlush();
        }

        #endregion
    }
}