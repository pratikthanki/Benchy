using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Configuration;
using Benchy.Helpers;
using Benchy.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskStatus = Benchy.Models.TaskStatus;

namespace Benchy.Services
{
    public class BenchmarkService : IHostedService
    {
        private readonly ILogger<BenchmarkService> _logger;
        private readonly IHttpService _httpService;
        private readonly IValueProvider _valueProvider;
        private readonly ITimeHandler _timeHandler;
        private readonly Configuration.Configuration _configuration;
        private readonly ICalculationService _calculationService;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task _task;

        public BenchmarkService(
            ILogger<BenchmarkService> logger,
            IHttpService httpService,
            IValueProvider valueProvider,
            IOptions<Configuration.Configuration> configuration,
            IHostApplicationLifetime appLifetime, 
            ITimeHandler timeHandler, 
            ICalculationService calculationService)
        {
            _logger = logger;
            _httpService = httpService;
            _valueProvider = valueProvider;
            _timeHandler = timeHandler;
            _calculationService = calculationService;
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
                    await ProcessStage(stage, cancellationToken);
                }
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

            _calculationService.SummaryReport.Status =
                Environment.ExitCode == 0 ? TaskStatus.Success : TaskStatus.Failed;

            _calculationService.CreateSummary();
        }

        private async Task ProcessStage(Stage stage, CancellationToken cancellationToken)
        {
            var totalRequests = stage.Requests;

            // A set of request tasks 
            var requestTasks = Enumerable
                .Range(0, stage.Requests)
                .AsParallel()
                .Select(_ => RecordRequestAsync(_valueProvider.GetRandomUrl(), cancellationToken))
                .ToList();
            
            do
            {
                var concurrentUsers = _valueProvider.GetRandomUserCount(stage.VirtualUsers);
                var requests = requestTasks.Take(concurrentUsers).ToList();

                foreach (var request in requests)
                {
                    requestTasks.Remove(request);
                }

                await Task.WhenAll(requests);

                foreach (var request in requests)
                {
                    _calculationService.RequestReports.Add(await request);
                }

                totalRequests -= concurrentUsers;
                
            } while (totalRequests > 0);
        }

        private async Task<RequestReport> RecordRequestAsync(string url, CancellationToken cancellationToken)
        {
            var requestTask = _httpService.GetAsync(_valueProvider.GetRandomUrl(), cancellationToken);

            _timeHandler.Start();

            var request = await requestTask;

            _timeHandler.Stop();

            return new RequestReport()
            {
                Id = new Guid(),
                Url = url,
                StatusCode = request.StatusCode,
                DurationMs = _timeHandler.ElapsedMilliseconds()
            };
        }
    }
}