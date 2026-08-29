namespace Lca.Infrastructure.Configuration;

public sealed class TenantDatabaseOptions
{
    public const string SectionName = "TenantDatabases";

    public Dictionary<string, string> BusinessConnectionByTenant { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
