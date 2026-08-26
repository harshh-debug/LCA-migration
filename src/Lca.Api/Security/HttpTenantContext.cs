using System.Security.Claims;
using Lca.Application.Security;
using Lca.Domain.Tenancy;

namespace Lca.Api.Security;

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAvailable => TenantId is not null;

    public TenantId? TenantId
    {
        get
        {
            if (Principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            string? value = Principal.FindFirstValue(TrustedClaimTypes.TenantId);
            return string.IsNullOrWhiteSpace(value) ? null : new TenantId(value);
        }
    }
}

