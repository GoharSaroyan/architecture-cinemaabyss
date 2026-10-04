using System.Text.Json.Serialization;

namespace CinemaAbyss.EventsService.Models;

// EventResponse schema from api-specification.yaml
public sealed class EventResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("partition")]
    public int Partition { get; set; }

    [JsonPropertyName("offset")]
    public long Offset { get; set; }

    [JsonPropertyName("event")]
    public Event Event { get; set; } = new();
}
