using System.Text.Json.Serialization;

namespace CinemaAbyss.EventsService.Models;

// MovieEvent schema from api-specification.yaml
public sealed class MovieEvent
{
    [JsonPropertyName("movie_id")]
    public int MovieId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UserId { get; set; }

    [JsonPropertyName("rating")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Rating { get; set; }

    [JsonPropertyName("genres")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Genres { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}
