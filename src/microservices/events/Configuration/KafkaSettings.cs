namespace CinemaAbyss.EventsService.Configuration;

// Kafka connection settings ("Kafka" section of appsettings.json)
public sealed class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "";

    public string GroupId { get; set; } = "";

    public KafkaTopics Topics { get; set; } = new();
}
