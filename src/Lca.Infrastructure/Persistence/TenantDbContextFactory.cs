using Lca.Infrastructure.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lca.Infrastructure.Persistence;

internal sealed class TenantDbContextFactory(
    IOptions<DatabaseConnectionOptions> connections,
    IOptions<TenantDatabaseOptions> tenantDatabases) : ITenantDbContextFactory
{
    public LcaDbContext Create(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)
            || !tenantDatabases.Value.BusinessConnectionByTenant.TryGetValue(tenantId, out string? connectionName))
        {
            throw new TenantDatabaseNotConfiguredException(tenantId);
        }

        string? connectionString = connections.Value.Find(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new TenantDatabaseNotConfiguredException(tenantId);
        }

        DbContextOptions<LcaDbContext> options = new DbContextOptionsBuilder<LcaDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(InfrastructureAssembly.Marker.Assembly.FullName))
            .Options;

        return new LcaDbContext(options);
    }
}

public sealed class TenantDatabaseNotConfiguredException(string tenantId)
    : InvalidOperationException($"No trusted business database mapping is configured for tenant '{tenantId}'.");
