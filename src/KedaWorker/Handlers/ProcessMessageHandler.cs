using System.Net;
using System.Text.Json;
using Confluent.Kafka;
using KedaWorker.Requests;
using MediatR;
using Shared;
using Shared.Messages;

namespace KedaWorker.Handlers
{
    public class ProcessMessageHandler : AsyncRequestHandler<ProcessMessageRequest>
    {
        private readonly ILogger _logger;
        private readonly KafkaConfiguration _hubConfiguration;
        private readonly ProducerConfig _replyProducerConfig;

        public ProcessMessageHandler(
            ILogger<ProcessMessageHandler> logger,
            KafkaConfiguration hubConfiguration)
        {
            _logger = logger;
            _hubConfiguration = hubConfiguration;

            _replyProducerConfig = new ProducerConfig
            {
                BootstrapServers = hubConfiguration.BootstrapServers,
                ClientId = Dns.GetHostName(),
                EnableIdempotence = true,
                MaxInFlight = 5,
                Acks = Acks.All,
                MessageSendMaxRetries = 5

                //Uncomment for librdkafka debugging information
                //Debug = "security,broker,protocol"
            };
        }

        protected override async Task Handle(ProcessMessageRequest request, CancellationToken cancellationToken)
        {
            Exception processedWithError = null;

            try
            {
                var message = JsonSerializer.Deserialize<Shared.Messages.Message>(request.Result.Message.Value);

                if (message.SimulateError)
                    throw new Exception($"This message is an error simulation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                processedWithError = ex;
            }
            finally
            {
                _logger.LogInformation($"[Partition - {request.Result.Partition.Value}] - Message {request.Result.Message.Key}.");

                await ReplyAsync(request.Result.Message.Key, new ReplyMessage()
                {
                    Detail = processedWithError?.Message,
                    Error = processedWithError != null,
                    MessageId = request.Result.Message.Key
                });
            }
        }

        private async Task ReplyAsync(string correlationId, ReplyMessage reply)
        {
            using var producer = new ProducerBuilder<string, string>(_replyProducerConfig)
                .Build();

            var kafkaMessage = new Message<string, string>()
            {
                Key = correlationId,
                Value = JsonSerializer.Serialize(reply)
            };

            await producer.ProduceAsync(_hubConfiguration.ReplyTopic, kafkaMessage);
        }
    }
}
