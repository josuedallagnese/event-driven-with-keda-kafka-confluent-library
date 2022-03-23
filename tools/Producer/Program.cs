using System.Net;
using System.Text.Json;
using Confluent.Kafka;
using ConsoleTables;
using Microsoft.Extensions.Configuration;
using Producer;
using Shared;
using Shared.Messages;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var hubConfiguration = new KafkaConfiguration(configuration);

var producerConfig = new ProducerConfig
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

using var producer = new ProducerBuilder<string, string>(producerConfig)
    .Build();

var replyConsumer = new ReplyConsumer(hubConfiguration);

replyConsumer.Start();

while (true)
{
    Console.Clear();

    Console.WriteLine("[1] - Send a new message");
    Console.WriteLine("[2] - List messages and reply messages");
    Console.WriteLine("[3] - Resend failed messages");
    Console.WriteLine("[4] - Resend failed messages and force successful");
    Console.WriteLine();
    Console.WriteLine("[9] - Exit");
    Console.WriteLine();

    _ = int.TryParse(Console.ReadLine().ToLower(), out var task);

    if (task == 9)
        break;

    if (task < 0 || task > 4)
    {
        Console.WriteLine("Invalid Task! Press any key to continue");
        Console.ReadKey();
        continue;
    }

    Console.WriteLine();

    if (task == 1)
    {
        var newMessage = Message.Generate();

        Console.WriteLine("Sending ....");
        Console.WriteLine();

        var kafkaMessage = new Message<string, string>()
        {
            Key = newMessage.Id,
            Value = JsonSerializer.Serialize(newMessage)
        };

        var deliveryResult = await producer.ProduceAsync(hubConfiguration.Topic, kafkaMessage);

        Console.WriteLine($"Delivery result: {deliveryResult.Status}");

        if (deliveryResult.Status == PersistenceStatus.Persisted)
        {
            MemoryStore.Save(newMessage);

            var table = new ConsoleTable("Id", "Customer", "Simulate Error");
            table.AddRow(newMessage.Id, newMessage.Customer, newMessage.SimulateError);
            table.Write();
        }
    }

    if (task == 2)
    {
        var table = new ConsoleTable("Id", "Customer", "Simulate Error");

        foreach (var message in MemoryStore.GetMessages())
            table.AddRow(message.Id, message.Customer, message.SimulateError);

        table.Write();

        Console.WriteLine();

        var replyTable = new ConsoleTable("MessageId", "Error", "Attempts", "Detail");

        foreach (var reply in MemoryStore.GetReplies())
            replyTable.AddRow(reply.MessageId, reply.Error, reply.Attempts, reply.Detail);

        replyTable.Write();
    }

    if (task == 3 || task == 4)
    {
        var messages = MemoryStore.GetMessages();
        var repliesWithError = MemoryStore.GetReplies().Where(w => w.Error);

        var messagesToSend = new List<Message<string, string>>();

        foreach (var reply in repliesWithError)
        {
            var message = messages.SingleOrDefault(s => s.Id == reply.MessageId);

            if (message == null)
                continue;

            if (task == 4)
                message.SimulateError = false;

            var kafkaMessage = new Message<string, string>()
            {
                Key = message.Id,
                Value = JsonSerializer.Serialize(message)
            };

            messagesToSend.Add(kafkaMessage);

            MemoryStore.Save(message);
        }

        Console.WriteLine("Sending ....");
        Console.WriteLine();

        if (messagesToSend.Any())
        {
            foreach (var item in messagesToSend)
            {
                var deliveryResult = await producer.ProduceAsync(hubConfiguration.Topic, item);

                Console.WriteLine($"Delivery result: {deliveryResult.Status}");
            }
        }
        else
            Console.WriteLine("No messages found");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to continue");
    Console.ReadKey();
}

replyConsumer.Stop();
