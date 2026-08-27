using System.Security.Claims;

using Lca.Core.Security;

namespace Lca.Api.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId => IsAuthenticated
        ? Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;

    public IReadOnlyCollection<string> Permissions => IsAuthenticated
        ? Principal?.FindAll(TrustedClaimTypes.Permission).Select(static claim => claim.Value).ToArray() ?? []
        : [];
}
