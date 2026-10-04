namespace CinemaAbyss.ProxyService.Configuration;

// Feature flag settings
public sealed record MigrationSettings(bool Enabled, int MoviesPercent)
{
    public static MigrationSettings FromEnvironment()
    {
        var enabled = string.Equals(
            Environment.GetEnvironmentVariable("GRADUAL_MIGRATION"), "true", StringComparison.OrdinalIgnoreCase);

        var percent = int.TryParse(Environment.GetEnvironmentVariable("MOVIES_MIGRATION_PERCENT"), out var p) ? p : 0;

        return new MigrationSettings(enabled, Math.Clamp(percent, 0, 100));
    }
}
