using System.Text.RegularExpressions;
using Yarp.ReverseProxy.Configuration;

namespace CinemaAbyss.ProxyService.Configuration;

// YARP config filter: replaces destination addresses written as "{{ENV_VAR}}" in appsettings.json
// with the value of that environment variable (MONOLITH_URL, MOVIES_SERVICE_URL, EVENTS_SERVICE_URL from docker-compose.yml).
public sealed partial class EnvironmentAddressFilter : IProxyConfigFilter
{
    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        if (cluster.Destinations is null)
        {
            return new ValueTask<ClusterConfig>(cluster);
        }

        var destinations = cluster.Destinations.ToDictionary(
            d => d.Key,
            d => d.Value with { Address = Resolve(d.Value.Address) });

        return new ValueTask<ClusterConfig>(cluster with { Destinations = destinations });
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel) =>
        new(route);

    private static string Resolve(string address)
    {
        var match = PlaceholderRegex().Match(address);
        if (!match.Success)
        {
            return address;
        }

        var name = match.Groups[1].Value;
        var value = Environment.GetEnvironmentVariable(name);

        return string.IsNullOrEmpty(value)
            ? throw new InvalidOperationException($"Environment variable {name} is not set")
            : value;
    }

    [GeneratedRegex(@"^\{\{(\w+)\}\}$")]
    private static partial Regex PlaceholderRegex();
}
