using System.Text.Json.Serialization;

namespace CinemaAbyss.EventsService.Models;

// Event schema from api-specification.yaml: the message written to Kafka
public sealed class Event
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("payload")]
    public object Payload { get; set; } = new();
}
