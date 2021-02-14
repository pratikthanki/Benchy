using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benchy
{
    public class BenchmarkService : IHostedService
    {
        private readonly ILogger<BenchmarkService> _logger;
        private readonly IWebClient _webClient;
        private readonly IOptions<Configuration> _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private Task _task;

        public BenchmarkService(
            ILogger<BenchmarkService> logger,
            IWebClient webClient,
            IOptions<Configuration> configuration,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _webClient = webClient;
            _configuration = configuration;

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(appLifetime.ApplicationStopping);

            Environment.ExitCode = 0;

            _cancellationTokenSource.Token.Register(() =>
            {
                _logger.LogInformation($"Shutting down {nameof(BenchmarkService)}..");
                appLifetime.StopApplication();
            });

            _cancellationToken = _cancellationTokenSource.Token;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {nameof(BenchmarkService)}..");

            if (_task != null)
                throw new InvalidOperationException();

            if (!_cancellationTokenSource.IsCancellationRequested)
                _task = Task.Run(RunSafe, cancellationToken);

            _logger.LogInformation($"Starting {nameof(BenchmarkService)}..");

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stopping {nameof(BenchmarkService)}..");

            _cancellationTokenSource.Cancel();
            var runningTask = Interlocked.Exchange(ref _task, null);
            if (runningTask != null)
                await runningTask;

            _logger.LogInformation($"Stopped {nameof(BenchmarkService)}..");
        }

        private async Task RunSafe()
        {
            foreach (var stage in _configuration.Value.Stages)
            {
                var numOfThreads = stage.VirtualUsers;
                var waitHandles = new WaitHandle[numOfThreads];

                for (var i = 0; i < numOfThreads; i++)
                {
                    var j = i;

                    var handle = new EventWaitHandle(false, EventResetMode.ManualReset);
                    var thread = new Thread(() =>
                    {
                        _webClient.GetAsync(_configuration.Value.GetRandomUrl(), _cancellationToken);
                        handle.Set();
                    });

                    waitHandles[j] = handle;
                    thread.Start();
                }

                WaitHandle.WaitAll(waitHandles);
            }
        }
    }
}