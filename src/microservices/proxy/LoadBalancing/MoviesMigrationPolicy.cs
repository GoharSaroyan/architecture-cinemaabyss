using CinemaAbyss.ProxyService.Configuration;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

namespace CinemaAbyss.ProxyService.LoadBalancing;

// YARP load balancing policy used by the "movies-migration" cluster.
// Picks the movies service for MOVIES_MIGRATION_PERCENT % of requests when the feature flag is on.
// When the flag is off, all traffic goes to the monolith.
public sealed class MoviesMigrationPolicy : ILoadBalancingPolicy
{
    public const string PolicyName = "MoviesMigration";
    private const string MonolithDestination = "monolith";
    private const string MoviesDestination = "movies";

    private readonly MigrationSettings _settings;
    private readonly ILogger<MoviesMigrationPolicy> _logger;

    public MoviesMigrationPolicy(MigrationSettings settings, ILogger<MoviesMigrationPolicy> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public string Name => PolicyName;

    public DestinationState? PickDestination(
        HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> availableDestinations)
    {
        var toMovies = _settings.Enabled && Random.Shared.Next(100) < _settings.MoviesPercent;
        var targetId = toMovies ? MoviesDestination : MonolithDestination;

        var destination = availableDestinations.FirstOrDefault(d => d.DestinationId == targetId);

        _logger.LogInformation("{Method} {Path}{Query} -> {Destination}",
            context.Request.Method, context.Request.Path, context.Request.QueryString, destination?.DestinationId ?? "none");

        return destination;
    }
}
