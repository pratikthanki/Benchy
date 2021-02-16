using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Benchy
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddCommandLine(args);
                    config.AddJsonFile("appsettings.json");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddHostedService<BenchmarkService>()
                        .AddSingleton<IValueProvider, ValueProvider>()
                        .AddTransient<IWebClient, WebClient>()
                        .Configure<Configuration>(hostContext.Configuration);
                })
                .UseConsoleLifetime();
        }
    }
}