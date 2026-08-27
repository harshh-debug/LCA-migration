using Lca.Core.Tenancy;

namespace Lca.Core.Security;

public interface ITenantContext
{
    bool IsAvailable { get; }

    TenantId? TenantId { get; }
}
