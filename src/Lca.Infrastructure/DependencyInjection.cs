using Lca.Core.Catalog;
using Lca.Infrastructure.Catalog;
using Lca.Infrastructure.Configuration;
using Lca.Infrastructure.Governance;
using Lca.Infrastructure.Persistence;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lca.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLcaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DatabaseConnectionOptions>()
            .Bind(configuration.GetSection(DatabaseConnectionOptions.SectionName));
        services.AddOptions<TenantDatabaseOptions>()
            .Bind(configuration.GetSection(TenantDatabaseOptions.SectionName));
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IApprovalQueueService, ApprovalQueueService>();
        return services;
    }
}
