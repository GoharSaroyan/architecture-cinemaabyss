using CinemaAbyss.EventsService.Configuration;
using CinemaAbyss.EventsService.Endpoints;
using CinemaAbyss.EventsService.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Kafka settings are defined in appsettings.json ("Kafka" section)
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(KafkaSettings.SectionName));

builder.Services.AddSingleton<EventProducer>();
builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();

app.MapEventEndpoints();

var port = Environment.GetEnvironmentVariable("PORT");
if (string.IsNullOrEmpty(port))
{
    port = "8082";
}

Console.WriteLine($"Starting events service on port {port}");

app.Run($"http://0.0.0.0:{port}");
