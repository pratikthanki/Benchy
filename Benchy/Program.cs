using System;
using System.Threading.Tasks;
using Benchy.Helpers;
using Benchy.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ColoredConsole;

namespace Benchy
{
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            try
            {
                await CreateHostBuilder(args).Build().RunAsync();
            }
            catch (Exception ex)
            {
                ColorConsole.WriteLine(ex.Message.White().OnRed());
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config
                        .AddCommandLine(args)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddHostedService<BenchmarkService>()
                        .AddSingleton<IValueProvider, ValueProvider>()
                        .AddTransient<IHttpService, HttpService>()
                        .AddTransient<ITimeHandler, TimeHandler>()
                        .AddTransient<ICalculationService, CalculationService>()
                        .Configure<Configuration.Configuration>(hostContext.Configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging
                        .ClearProviders()
                        .SetMinimumLevel(LogLevel.Warning)
                        .AddDebug()
                        .AddConsole();
                })
                .UseConsoleLifetime();
        }
    }
}