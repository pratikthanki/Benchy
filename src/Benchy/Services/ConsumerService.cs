using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benchy.Helpers;
using Benchy.Reporters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using static System.Threading.Tasks.Task;

namespace Benchy.Services
{
    public class ConsumerService : IHostedService
    {
        private readonly ILogger<ConsumerService> _logger;
        private readonly Configuration.Configuration _configuration;
        private readonly IValueProvider _valueProvider;
        private readonly IReporter _reporter;
        private readonly IRequestClient _requestClient;
        private readonly ICalculationHandler _calculationHandler;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task _task;

        public ConsumerService(
            ILogger<ConsumerService> logger,
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
                _logger.LogInformation($"Shutting down {nameof(ConsumerService)}..");
                appLifetime.StopApplication();
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {nameof(ConsumerService)}..");

            if (_task != null)
            {
                throw new InvalidOperationException();
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    _task = Task.Run(Run, cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogCritical($"There was an error in running the Benchy {nameof(ConsumerService)}: {e}");
                }
                finally
                {
                    _cancellationTokenSource.Cancel();
                }
            }

            _logger.LogInformation($"Started {nameof(ConsumerService)}..");

            return CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stopping {nameof(ConsumerService)}..");

            _cancellationTokenSource.Cancel();

            var runningTask = Interlocked.Exchange(ref _task, null);
            if (runningTask != null)
            {
                await runningTask;
            }

            _logger.LogInformation($"Stopped {nameof(ConsumerService)}..");
        }

        private static void Run()
        {
            var factory = new ConnectionFactory() {HostName = "localhost"};

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: "task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);

                int dots = message.Split('.').Length - 1;
                Thread.Sleep(dots * 1000);

                Console.WriteLine(" [x] Done");

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            channel.BasicConsume(queue: "task_queue", autoAck: false, consumer: consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}