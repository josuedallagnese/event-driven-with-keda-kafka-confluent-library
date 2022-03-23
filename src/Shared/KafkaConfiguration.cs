using System;
using Microsoft.Extensions.Configuration;

namespace Shared
{
    public class KafkaConfiguration
    {
        public string BootstrapServers { get; set; }
        public string Topic { get; set; }
        public string ReplyTopic { get; set; }
        public string ConsumerGroup { get; }

        public KafkaConfiguration(IConfiguration configuration)
        {
            BootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers");

            if (string.IsNullOrWhiteSpace(BootstrapServers))
                throw new ArgumentNullException($"Invalid Kafka:BootstrapServers configuration value");

            Topic = configuration.GetValue<string>("Kafka:Topic");

            if (string.IsNullOrWhiteSpace(Topic))
                throw new ArgumentNullException($"Invalid Kafka:Topic configuration value");

            ReplyTopic = configuration.GetValue<string>("Kafka:ReplyTopic");

            if (string.IsNullOrWhiteSpace(ReplyTopic))
                throw new ArgumentNullException($"Invalid Kafka:ReplyTopic configuration value");

            ConsumerGroup = configuration.GetValue<string>("Kafka:ConsumerGroup");

            if (string.IsNullOrWhiteSpace(ConsumerGroup))
                throw new ArgumentNullException($"Invalid Kafka:ConsumerGroup configuration value");
        }
    }
}
