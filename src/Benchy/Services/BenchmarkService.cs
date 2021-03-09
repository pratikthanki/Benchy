using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Configuration;
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
        private readonly IRequestClient _requestClient;
        private readonly ICalculationHandler _calculationHandler;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task _task;

        public BenchmarkService(
            ILogger<BenchmarkService> logger,
            IOptions<Configuration.Configuration> configuration,
            IHostApplicationLifetime appLifetime,
            ICalculationHandler calculationHandler,
            IValueProvider valueProvider,
            IRequestClient requestClient,
            IReporter reporter)
        {
            _logger = logger;
            _calculationHandler = calculationHandler;
            _valueProvider = valueProvider;
            _requestClient = requestClient;
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

            try
            {
                _calculationHandler.LogTestStart();

                foreach (var stage in _configuration.Stages)
                {
                    await ProcessStage(stage, cancellationToken);

                    await Task.Delay(_configuration.SecondsDelayBetweenStages * 1000, cancellationToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"There was an error in running Benchy: {e}");
                _calculationHandler.SetStatus(TaskStatus.Failed);
            }
            finally
            {
                _calculationHandler.LogTestEnd();
                _cancellationTokenSource.Cancel();
            }

            _calculationHandler.SetStatus(TaskStatus.Success);
            _calculationHandler.CreateSummaryReport();

            await _reporter.Write(_calculationHandler.GetSummaryReport());

            // TODO: make this better when printing results to stdout
            if (_configuration.ConsoleLog)
            {
                var report = _calculationHandler.GetSummaryReport();
                foreach (var StageSummary in report.StageSummary)
                {
                    _logger.LogInformation($"Results for stage: {StageSummary.Stage}; Url: {StageSummary.Url}");
                    _logger.LogInformation($"{StageSummary}");
                }
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

        private async Task ProcessStage(Stage stage, CancellationToken cancellationToken)
        {
            var totalRequests = stage.Requests;

            _logger.LogInformation($"Running requests: {totalRequests}");

            do
            {
                var count = Math.Min(GetRandomUserCount(stage.VirtualUsers), totalRequests);

                // A set of request tasks 
                var requests = Enumerable
                    .Range(0, count)
                    .Select(_ => _requestClient.RecordRequestAsync(
                        GetRandomUrl(),
                        stage,
                        _configuration.Headers,
                        cancellationToken))
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