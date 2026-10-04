using System.Text.Json.Serialization;

namespace CinemaAbyss.EventsService.Models;

// PaymentEvent schema from api-specification.yaml
public sealed class PaymentEvent
{
    [JsonPropertyName("payment_id")]
    public int PaymentId { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    [JsonPropertyName("method_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MethodType { get; set; }
}
