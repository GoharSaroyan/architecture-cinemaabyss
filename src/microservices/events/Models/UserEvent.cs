using System.Text.Json.Serialization;

namespace CinemaAbyss.EventsService.Models;

// UserEvent schema from api-specification.yaml
public sealed class UserEvent
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("username")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }
}
