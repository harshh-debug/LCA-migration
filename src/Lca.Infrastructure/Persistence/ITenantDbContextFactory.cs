namespace Lca.Infrastructure.Persistence;

internal interface ITenantDbContextFactory
{
    LcaDbContext Create(string tenantId);
}
