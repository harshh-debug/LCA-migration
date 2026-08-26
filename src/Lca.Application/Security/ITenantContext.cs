using Lca.Domain.Tenancy;

namespace Lca.Application.Security;

public interface ITenantContext
{
    bool IsAvailable { get; }

    TenantId? TenantId { get; }
}

