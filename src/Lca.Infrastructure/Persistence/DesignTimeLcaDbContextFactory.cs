using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Lca.Core.Security;
using Lca.Core.Tenancy;

namespace Lca.Infrastructure.Persistence;

public sealed class DesignTimeLcaDbContextFactory : IDesignTimeDbContextFactory<LcaDbContext>
{
    public LcaDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("LCA_MIGRATION_CONNECTION")
            ?? "Server=localhost,1433;Database=Lca;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
        DbContextOptions<LcaDbContext> options = new DbContextOptionsBuilder<LcaDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new LcaDbContext(options, UnavailableTenantContext.Instance);
    }

    private sealed class UnavailableTenantContext : ITenantContext
    {
        public static UnavailableTenantContext Instance { get; } = new();

        public bool IsAvailable => false;

        public TenantId? TenantId => null;
    }
}
