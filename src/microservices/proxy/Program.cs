using CinemaAbyss.ProxyService.Configuration;
using CinemaAbyss.ProxyService.LoadBalancing;
using Yarp.ReverseProxy.LoadBalancing;

var builder = WebApplication.CreateBuilder(args);

// Routes, clusters and service addresses are defined in appsettings.json ("ReverseProxy" section).
builder.Services.AddSingleton(MigrationSettings.FromEnvironment());
builder.Services.AddSingleton<ILoadBalancingPolicy, MoviesMigrationPolicy>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Json(new Dictionary<string, bool> { ["status"] = true }));
app.MapReverseProxy();

var port = Environment.GetEnvironmentVariable("PORT");
if (string.IsNullOrEmpty(port))
{
    port = "8000";
}

var settings = app.Services.GetRequiredService<MigrationSettings>();
Console.WriteLine($"Starting proxy service on port {port}");
Console.WriteLine($"Gradual migration: {settings.Enabled}, movies migration percent: {settings.MoviesPercent}");

app.Run($"http://0.0.0.0:{port}");
