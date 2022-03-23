using System.Net;
using Confluent.Kafka;
using KedaWorker.Requests;
using MediatR;
using Shared;

namespace KedaWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly KafkaConfiguration _hubConfiguration;
        private readonly ConsumerConfig _consumerConfig;

        public Worker(
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory,
            KafkaConfiguration kafkaConfiguration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubConfiguration = kafkaConfiguration;

            _consumerConfig = new ConsumerConfig()
            {
                BootstrapServers = kafkaConfiguration.BootstrapServers,
                ClientId = Dns.GetHostName(),
                GroupId = kafkaConfiguration.ConsumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig)
                .Build();

            _logger.LogInformation($"Subscribing topic {_hubConfiguration.Topic}");

            consumer.Subscribe(_hubConfiguration.Topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Run(async () =>
                {
                    var result = consumer.Consume(stoppingToken);

                    await ProcessEventAsync(result);
                });
            }

            consumer.Close();
        }

        private async Task ProcessEventAsync(ConsumeResult<string, string> result)
        {
            using var scope = _scopeFactory.CreateScope();

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new ProcessMessageRequest(result));
        }
    }
}
