using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Helpers;
using Benchy.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using static System.Threading.Tasks.Task;

namespace Benchy.Services
{
    public class ProducerService : IHostedService
    {
        private readonly IValueProvider _valueProvider;
        private readonly ILogger<ProducerService> _logger;
        private readonly Configuration.Configuration _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task _task;

        public ProducerService(
            IValueProvider valueProvider,
            ILogger<ProducerService> logger,
            IOptions<Configuration.Configuration> configuration,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _valueProvider = valueProvider;
            _configuration = configuration.Value;

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(appLifetime.ApplicationStopping);

            Environment.ExitCode = 0;

            _cancellationTokenSource.Token.Register(() =>
            {
                _logger.LogInformation($"Shutting down {nameof(ProducerService)}..");
                appLifetime.StopApplication();
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {nameof(ProducerService)}..");

            if (_task != null)
            {
                throw new InvalidOperationException();
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    Run();
                }
                catch (Exception e)
                {
                    _logger.LogCritical($"There was an error in running the Benchy {nameof(ProducerService)}: {e}");
                }
                finally
                {
                    _cancellationTokenSource.Cancel();
                }
            }

            _logger.LogInformation($"Started {nameof(ProducerService)}..");

            return CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stopping {nameof(ProducerService)}..");

            _cancellationTokenSource.Cancel();

            var runningTask = Interlocked.Exchange(ref _task, null);
            if (runningTask != null)
            {
                await runningTask;
            }

            _logger.LogInformation($"Stopped {nameof(ProducerService)}..");
        }

        private void Run()
        {
            const string queueName = "requests";
            const string exchange = "";
            const string routingKey = "task_queue";

            var factory = new ConnectionFactory() {HostName = "localhost"};

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var totalRequests = _configuration.Stages.Select(stage => stage.Requests).Sum();

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            foreach (var _ in Enumerable.Range(0, totalRequests))
            {
                var request = new Request
                {
                    Url = _configuration.Urls[_valueProvider.GetRandomInt(_configuration.Urls.Length)],
                    Headers = _configuration.Headers
                };

                _logger.LogInformation("Publishing to queue..");

                channel.BasicPublish(
                    exchange: exchange,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(request.ToString()));
            }
        }
    }
}