using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Helpers;
using Benchy.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benchy
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
                    var endpoints = Enumerable
                        .Range(0, stage.Requests)
                        .Select(_ => _configuration.Urls[_valueProvider.GetRandomInt()]);

                    var numOfThreads = stage.VirtualUsers;
                    var waitHandles = new WaitHandle[numOfThreads];

                    for (var i = 0; i < numOfThreads; i++)
                    {
                        var j = i;

                        var handle = new EventWaitHandle(false, EventResetMode.ManualReset);
                        var thread = new Thread(async () =>
                        {
                            await _httpService.GetAsync(GetRandomUrl(), cancellationToken);
                            handle.Set();
                        });

                        waitHandles[j] = handle;
                        thread.Start();
                    }

                    WaitHandle.WaitAll(waitHandles);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical($"There was an error in running Benchy: {e}");
            }
            finally
            {
                _summaryReport.TestStart = DateTimeOffset.UtcNow;

                _cancellationTokenSource.Cancel();
            }

            _summaryReport.IsSuccess = Environment.ExitCode == 0;
        }

        private string GetRandomUrl() => _configuration.Urls[_valueProvider.GetRandomInt()];
    }
}