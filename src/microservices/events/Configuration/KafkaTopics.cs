namespace CinemaAbyss.EventsService.Configuration;

// Topic names ("Kafka:Topics" section of appsettings.json)
public sealed class KafkaTopics
{
    public string Movie { get; set; } = "";

    public string User { get; set; } = "";

    public string Payment { get; set; } = "";

    public string[] All => new[] { Movie, User, Payment };
}
