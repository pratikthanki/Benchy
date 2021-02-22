using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Helpers;
using Benchy.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benchy.Services
{
    public class BenchmarkService : IHostedService
    {
        private readonly ILogger<BenchmarkService> _logger;
        private readonly IHttpService _httpService;
        private readonly IValueProvider _valueProvider;
        private readonly Configuration.Configuration _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly SummaryReport _summaryReport;

        private Task _task;

        public BenchmarkService(
            ILogger<BenchmarkService> logger,
            IHttpService httpService,
            IValueProvider valueProvider,
            IOptions<Configuration.Configuration> configuration,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _httpService = httpService;
            _valueProvider = valueProvider;
            _summaryReport = new SummaryReport();
            _configuration = configuration.Value;

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(appLifetime.ApplicationStopping);

            Environment.ExitCode = 0;

            _cancellationTokenSource.Token.Register(() =>
            {
                _logger.LogInformation($"Shutting down {nameof(BenchmarkService)}..");
                appLifetime.StopApplication();
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {nameof(BenchmarkService)}..");

            if (_task != null)
            {
                throw new InvalidOperationException();
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _task = Task.Run(() => RunSafe(_cancellationTokenSource.Token), cancellationToken);
            }

            _logger.LogInformation($"Started {nameof(BenchmarkService)}..");

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stopping {nameof(BenchmarkService)}..");

            _cancellationTokenSource.Cancel();

            var runningTask = Interlocked.Exchange(ref _task, null);
            if (runningTask != null)
            {
                await runningTask;
            }

            _logger.LogInformation($"Stopped {nameof(BenchmarkService)}..");
        }

        private async Task RunSafe(CancellationToken cancellationToken)
        {
            // TODO: Would it be helpful to log throughout the benchmark so it is clear the application is alive?
            // _ = Task.Run(() => { }, cancellationToken);

            _summaryReport.TestStart = DateTimeOffset.UtcNow;

            try
            {
                foreach (var stage in _configuration.Stages)
                {
                    var totalRequests = stage.Requests;
                    
                    // A set of request tasks 
                    var requestTasks = Enumerable
                        .Range(0, stage.Requests)
                        .Select(_ => _httpService.GetAsync(_valueProvider.GetRandomUrl(), cancellationToken))
                        .ToList();

                    do
                    {
                        var concurrentUsers = _valueProvider.GetRandomUserCount(stage.VirtualUsers);

                        var requests = requestTasks.Take(concurrentUsers).ToList();

                        //Wait for all the requests to finish
                        var allTasks = Task.WhenAll(requests);
                        
                        try
                        {
                            await Task.WhenAll(allTasks);
                        }
                        catch (Exception)
                        {
                            // TODO
                        }

                        totalRequests -= concurrentUsers;

                    } while (totalRequests > 0);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"There was an error in running Benchy: {e}");
            }
            finally
            {
                _summaryReport.TestEnd = DateTimeOffset.UtcNow;

                _cancellationTokenSource.Cancel();
            }

            _summaryReport.IsSuccess = Environment.ExitCode == 0;
        }
    }
}