using Lca.Core.Security;
using Lca.Core.Tenancy;

namespace Lca.Api.Security;

public sealed class HttpTenantContext : ITenantContext
{
    private TenantId? tenantId;

    public bool IsAvailable => tenantId is not null;

    public TenantId? TenantId => tenantId;

    public void Initialize(TenantId validatedTenantId)
    {
        if (tenantId is not null && tenantId != validatedTenantId)
        {
            throw new InvalidOperationException("Tenant context has already been initialized.");
        }

        tenantId = validatedTenantId;
    }
}
