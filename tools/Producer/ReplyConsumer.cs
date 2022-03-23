using System.Net;
using System.Text.Json;
using Confluent.Kafka;
using Shared;

namespace Producer
{
    public class ReplyConsumer
    {
        private readonly ConsumerConfig _consumerConfig;
        private readonly KafkaConfiguration _hubConfiguration;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ReplyConsumer(KafkaConfiguration hubConfiguration)
        {
            _consumerConfig = new ConsumerConfig()
            {
                BootstrapServers = hubConfiguration.BootstrapServers,
                ClientId = Dns.GetHostName(),
                GroupId = hubConfiguration.ConsumerGroup
            };

            _hubConfiguration = hubConfiguration;

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            Task.Run(() =>
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig)
                    .Build();

                consumer.Subscribe(_hubConfiguration.ReplyTopic);

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(_cancellationTokenSource.Token);

                        var replyMessage = JsonSerializer.Deserialize<Shared.Messages.ReplyMessage>(result.Message.Value);

                        MemoryStore.Save(replyMessage);
                    }
                    catch (Exception)
                    {
                    }
                }

                consumer.Close();
            });
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
