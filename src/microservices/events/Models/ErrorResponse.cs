using System.Text.Json.Serialization;

namespace CinemaAbyss.EventsService.Models;

// Error schema from api-specification.yaml
public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = "";
}
