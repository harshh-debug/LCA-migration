using Lca.Core.Catalog;
using Lca.Core.Governance;

using Microsoft.EntityFrameworkCore;

namespace Lca.Infrastructure.Persistence;

public sealed class LcaDbContext(DbContextOptions<LcaDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<AIImage> AIImages => Set<AIImage>();

    public DbSet<ApprovalQueueItem> ApprovalQueue => Set<ApprovalQueueItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(InfrastructureAssembly.Marker.Assembly);
}
