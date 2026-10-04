using System.Text.Json;
using CinemaAbyss.EventsService.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CinemaAbyss.EventsService.Kafka;

// Writes events to Kafka topics
public sealed class EventProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<EventProducer> _logger;

    public EventProducer(IOptions<KafkaSettings> settings, ILogger<EventProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            Acks = Acks.All,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task<DeliveryResult<string, string>> PublishAsync<T>(
        string topic, string key, T payload, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(payload),
        };

        var result = await _producer.ProduceAsync(topic, message, cancellationToken);

        _logger.LogInformation("Produced event to {Topic} [partition {Partition}, offset {Offset}]: key={Key} value={Value}",
            result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Key, result.Message.Value);

        return result;
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
