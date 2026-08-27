using Lca.Core.Security;

using Microsoft.AspNetCore.Authorization;

namespace Lca.Api.Security;

public sealed class TenantRequiredHandler(ITenantContext tenantContext)
    : AuthorizationHandler<TenantRequiredRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequiredRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && tenantContext.IsAvailable)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
