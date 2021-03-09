using System;
using System.Threading.Tasks;
using Benchy.Helpers;
using Benchy.Reporters;
using Benchy.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ColoredConsole;
using Microsoft.Extensions.Logging.Console;

namespace Benchy
{
    class Program
    {
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
                        .AddTransient<IRequestClient, RequestClient>()
                        .AddTransient<ITimeHandler, TimeHandler>()
                        .AddTransient<ICalculationHandler, CalculationHandler>()
                        .AddTransient<IReporter, JsonReporter>()
                        .Configure<Configuration.Configuration>(hostContext.Configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging
                        .ClearProviders()
                        .SetMinimumLevel(LogLevel.Warning)
                        .AddConsole()
                        .AddSimpleConsole(options =>
                        {
                            options.IncludeScopes = true;
                            options.SingleLine = true;
                            options.TimestampFormat = "HH:mm:ss ";
                            options.UseUtcTimestamp = true;
                            options.ColorBehavior = LoggerColorBehavior.Enabled;
                        });
                })
                .UseConsoleLifetime();
        }
    }
}