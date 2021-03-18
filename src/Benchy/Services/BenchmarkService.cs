using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Benchy.Configuration;
using Benchy.Helpers;
using Benchy.Models;
using Benchy.Reporters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Threading.Tasks.Task;
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
                _task = Run(() => RunSafe(_cancellationTokenSource.Token), cancellationToken);
            }

            _logger.LogInformation($"Started {nameof(BenchmarkService)}..");

            return CompletedTask;
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

                    await Delay(_configuration.SecondsDelayBetweenStages * 1000, cancellationToken);
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

            await _reporter.Write(_calculationHandler.GetSummaryReport());

            // TODO: make this better when printing results to stdout
            if (_configuration.ConsoleLog)
            {
                ConsoleLogReport();
            }
        }

        private void ConsoleLogReport()
        {
            var report = _calculationHandler.GetSummaryReport();
            foreach (var StageSummary in report.StageSummary)
            {
                _logger.LogInformation($"Results for stage: {StageSummary.Stage}; Url: {StageSummary.Url}");
                _logger.LogInformation($"{StageSummary}");
            }
        }

        private async Task<ChannelReader<ValueTask<RequestSummary>>> BuildUserRequestChannelReader(
            Stage stage,
            CancellationToken cancellationToken)
        {
            var totalRequests = stage.Requests;

            var requestChannel = Channel.CreateBounded<ValueTask<RequestSummary>>(
                new BoundedChannelOptions(totalRequests)
                {
                    SingleReader = false,
                    SingleWriter = true
                });

            _logger.LogInformation($"Building bounded request channel: {totalRequests}");

            // Separate producer thread
            await Run(() =>
            {
                Enumerable
                    .Range(0, totalRequests)
                    .ToList()
                    .ForEach(async _ =>
                    {
                        var request = _requestClient.RecordRequestAsync(
                            GetRandomUrl(),
                            stage,
                            _configuration.Headers,
                            cancellationToken);

                        await requestChannel.Writer.WriteAsync(request, cancellationToken);
                    });

                requestChannel.Writer.TryComplete();
            }, cancellationToken);

            return requestChannel.Reader;
        }

        private async Task ProcessStage(Stage stage, CancellationToken cancellationToken)
        {
            var reader = await BuildUserRequestChannelReader(stage, cancellationToken);
            var requests = new List<RequestSummary>();

            // Create a list of consumers
            var tasks = new List<ValueTask<RequestSummary>>();
            var count = 0;

            while (await reader.WaitToReadAsync(cancellationToken))
            {
                if (!reader.TryRead(out var request))
                {
                    break;
                }

                count++;
                if (count < stage.VirtualUsers)
                {
                    tasks.Add(request);
                    continue;
                }

                await WhenAll(tasks
                    .Where(t => !t.IsCompletedSuccessfully)
                    .Select(t => t.AsTask()));

                tasks.ForEach(task => _calculationHandler.AddRequestReport(task.Result));

                count = 0;
                tasks = new List<ValueTask<RequestSummary>>();
            }
        }

        private string GetRandomUrl()
        {
            return _configuration.Urls[_valueProvider.GetRandomInt(_configuration.Urls.Length)];
        }
    }
}