using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lca.Infrastructure.Persistence;

public sealed class DesignTimeLcaDbContextFactory : IDesignTimeDbContextFactory<LcaDbContext>
{
    public LcaDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("LCA_MIGRATION_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=LcaMigrationDesign;Trusted_Connection=True;";
        DbContextOptions<LcaDbContext> options = new DbContextOptionsBuilder<LcaDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new LcaDbContext(options);
    }
}
