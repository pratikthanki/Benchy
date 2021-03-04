using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Helpers;
using Benchy.Reporters;
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
        private readonly IReporter _reporter;
        private readonly IHttpClient _httpClient;
        private readonly ICalculationHandler _calculationHandler;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task _task;

        public BenchmarkService(
            ILogger<BenchmarkService> logger,
            IOptions<Configuration.Configuration> configuration,
            IHostApplicationLifetime appLifetime,
            ICalculationHandler calculationHandler,
            IValueProvider valueProvider,
            IHttpClient httpClient,
            IReporter reporter)
        {
            _logger = logger;
            _calculationHandler = calculationHandler;
            _valueProvider = valueProvider;
            _httpClient = httpClient;
            _reporter = reporter;
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

            _calculationHandler.LogStart();

            try
            {
                for (var stage = 0; stage < _configuration.Stages.Count; stage++)
                {
                    await ProcessStage(stage, cancellationToken);

                    await Task.Delay(_configuration.SecondsDelayBetweenStages * 1000, cancellationToken);
                }

                _calculationHandler.CreateSummary();

                await _reporter.Write(_calculationHandler.SummaryReport);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"There was an error in running Benchy: {e}");
                _calculationHandler.SummaryReport.Status = TaskStatus.Failed;
            }
            finally
            {
                _calculationHandler.LogEnd();

                _cancellationTokenSource.Cancel();
            }

            _calculationHandler.SummaryReport.Status = TaskStatus.Success;
        }

        private string GetRandomUrl()
        {
            return _configuration.Urls[_valueProvider.GetRandomInt(_configuration.Urls.Length)];
        }

        private int GetRandomUserCount(int concurrentUsers)
        {
            return _valueProvider.GetRandomInt(concurrentUsers) + 1;
        }

        private async Task ProcessStage(int stageId, CancellationToken cancellationToken)
        {
            var stage = _configuration.Stages[stageId];
            var totalRequests = stage.Requests;

            _logger.LogInformation($"Running requests: {totalRequests}");

            do
            {
                var count = Math.Min(GetRandomUserCount(stage.VirtualUsers), totalRequests);

                // A set of request tasks 
                var requests = Enumerable
                    .Range(0, count)
                    .Select(_ => _httpClient.RecordRequestAsync(GetRandomUrl(), stageId, cancellationToken))
                    .ToList();

                _logger.LogInformation($"Running total requests: {count}");

                await Task.WhenAll(requests);

                requests.ForEach(async request => { _calculationHandler.AddRequestReport(await request); });

                totalRequests -= count;

                _logger.LogInformation($"Requests remaining: {totalRequests}");

            } while (totalRequests > 0);
        }
    }
}