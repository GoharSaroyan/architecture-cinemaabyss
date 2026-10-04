using CinemaAbyss.ProxyService.Configuration;
using CinemaAbyss.ProxyService.LoadBalancing;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;

var builder = WebApplication.CreateBuilder(args);

// Routes and clusters are defined in appsettings.json ("ReverseProxy" section).
// Destination addresses are written there as {{ENV_VAR}} and resolved from the environment (docker-compose.yml).
builder.Services.AddSingleton(MigrationSettings.FromEnvironment());
builder.Services.AddSingleton<ILoadBalancingPolicy, MoviesMigrationPolicy>();

builder.Services.AddSingleton<IProxyConfigFilter, EnvironmentAddressFilter>();

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
