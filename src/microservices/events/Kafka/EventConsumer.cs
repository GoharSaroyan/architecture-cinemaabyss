using CinemaAbyss.EventsService.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CinemaAbyss.EventsService.Kafka;

// Reads events from all topics in the background and writes them to the log
public sealed class EventConsumer : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<EventConsumer> _logger;

    public EventConsumer(IOptions<KafkaSettings> settings, ILogger<EventConsumer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => Consume(stoppingToken), stoppingToken);

    private void Consume(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = true,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_settings.Topics.All);

        _logger.LogInformation("Consumer subscribed to topics: {Topics}", string.Join(", ", _settings.Topics.All));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    _logger.LogInformation("Consumed event from {Topic} [partition {Partition}, offset {Offset}]: key={Key} value={Value}",
                        result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Key, result.Message.Value);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning("Consume error: {Reason}", ex.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Service is stopping
        }
        finally
        {
            consumer.Close();
        }
    }
}
