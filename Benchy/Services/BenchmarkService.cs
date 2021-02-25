using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Configuration;
using Benchy.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskStatus = Benchy.Models.TaskStatus;

namespace Benchy.Services
{
    public class BenchmarkService : IHostedService
    {
        private readonly ILogger<BenchmarkService> _logger;
        private readonly Configuration.Configuration _configuration;
        private readonly IValueProvider _valueProvider;
        private readonly IHttpService _httpService;
        private readonly ICalculationService _calculationService;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task _task;

        public BenchmarkService(
            ILogger<BenchmarkService> logger,
            IOptions<Configuration.Configuration> configuration,
            IHostApplicationLifetime appLifetime,
            ICalculationService calculationService,
            IValueProvider valueProvider, IHttpService httpService)
        {
            _logger = logger;
            _calculationService = calculationService;
            _valueProvider = valueProvider;
            _httpService = httpService;
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

            _calculationService.SummaryReport.TestStart = DateTimeOffset.UtcNow;

            try
            {
                foreach (var stage in _configuration.Stages)
                {
                    await ProcessStage(stage, _calculationService, cancellationToken);

                    await Task.Delay(_configuration.SecondsDelayBetweenStages * 1000, cancellationToken);
                }

                _calculationService.CreateSummary();
                _calculationService.SummaryReport.Status = TaskStatus.Success;
            }
            catch (Exception e)
            {
                _logger.LogCritical($"There was an error in running Benchy: {e}");
                _calculationService.SummaryReport.Status = TaskStatus.Failed;
            }
            finally
            {
                _calculationService.SummaryReport.TestEnd = DateTimeOffset.UtcNow;
                _cancellationTokenSource.Cancel();
            }
        }
        
        private string GetRandomUrl()
        {
            return _configuration.Urls[_valueProvider.GetRandomInt(_configuration.Urls.Length)];
        }

        private int GetRandomUserCount(int concurrentUsers)
        {
            return _valueProvider.GetRandomInt(concurrentUsers) + 1;
        }

        private async Task ProcessStage(
            Stage stage,
            ICalculationService calculationService,
            CancellationToken cancellationToken)
        {
            var totalRequests = stage.Requests;

            _logger.LogInformation($"Running requests: {totalRequests}");

            do
            {
                var concurrentUsers = GetRandomUserCount(stage.VirtualUsers);

                // A set of request tasks 
                var requests = Enumerable
                    .Range(0, concurrentUsers)
                    .Select(_ => _httpService.RecordRequestAsync(GetRandomUrl(), cancellationToken))
                    .ToList();

                _logger.LogInformation($"Running total requests: {concurrentUsers}");

                await Task.WhenAll(requests);

                foreach (var request in requests)
                {
                    calculationService.RequestReports.Add(await request);
                }

                totalRequests -= concurrentUsers;

                _logger.LogInformation($"Requests remaining: {totalRequests}");

            } while (totalRequests > 0);
        }
    }
}