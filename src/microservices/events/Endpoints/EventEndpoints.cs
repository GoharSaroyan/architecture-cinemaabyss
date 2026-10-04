using CinemaAbyss.EventsService.Configuration;
using CinemaAbyss.EventsService.Kafka;
using CinemaAbyss.EventsService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CinemaAbyss.EventsService.Endpoints;

// HTTP API of the events service (see api-specification.yaml)
public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        app.MapGet("/api/events/health", () => Results.Json(new Dictionary<string, bool> { ["status"] = true }));

        app.MapPost("/api/events/movie", (MovieEvent evt, EventProducer producer, IOptions<KafkaSettings> settings) =>
        {
            if (string.IsNullOrEmpty(evt.Title) || string.IsNullOrEmpty(evt.Action))
            {
                return Task.FromResult(BadRequest("movie_id, title and action are required"));
            }

            var message = new Event
            {
                Id = $"movie-{evt.MovieId}-{evt.Action}",
                Type = "movie",
                Timestamp = DateTimeOffset.UtcNow,
                Payload = evt,
            };

            return Publish(producer, settings.Value.Topics.Movie, evt.MovieId.ToString(), message);
        });

        app.MapPost("/api/events/user", (UserEvent evt, EventProducer producer, IOptions<KafkaSettings> settings) =>
        {
            if (string.IsNullOrEmpty(evt.Action) || evt.Timestamp is null)
            {
                return Task.FromResult(BadRequest("user_id, action and timestamp are required"));
            }

            var message = new Event
            {
                Id = $"user-{evt.UserId}-{evt.Action}",
                Type = "user",
                Timestamp = evt.Timestamp.Value,
                Payload = evt,
            };

            return Publish(producer, settings.Value.Topics.User, evt.UserId.ToString(), message);
        });

        app.MapPost("/api/events/payment", (PaymentEvent evt, EventProducer producer, IOptions<KafkaSettings> settings) =>
        {
            if (string.IsNullOrEmpty(evt.Status) || evt.Timestamp is null)
            {
                return Task.FromResult(BadRequest("payment_id, user_id, amount, status and timestamp are required"));
            }

            var message = new Event
            {
                Id = $"payment-{evt.PaymentId}-{evt.Status}",
                Type = "payment",
                Timestamp = evt.Timestamp.Value,
                Payload = evt,
            };

            return Publish(producer, settings.Value.Topics.Payment, evt.PaymentId.ToString(), message);
        });
    }

    private static async Task<IResult> Publish(EventProducer producer, string topic, string key, Event message)
    {
        try
        {
            var result = await producer.PublishAsync(topic, key, message);

            return Results.Json(new EventResponse
            {
                Status = "success",
                Partition = result.Partition.Value,
                Offset = result.Offset.Value,
                Event = message,
            }, statusCode: StatusCodes.Status201Created);
        }
        catch (ProduceException<string, string> ex)
        {
            return Results.Json(new ErrorResponse { Error = ex.Error.Reason },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static IResult BadRequest(string error) =>
        Results.Json(new ErrorResponse { Error = error }, statusCode: StatusCodes.Status400BadRequest);
}
