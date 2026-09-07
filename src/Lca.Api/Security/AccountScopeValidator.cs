using Lca.Core.Security;
using Lca.Core.Tenancy;
using Lca.Infrastructure.Identity;
using Lca.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lca.Api.Security;

public sealed class AccountScopeValidator(LcaDbContext context, UserManager<ApplicationUser> userManager)
{
    public async Task<bool> IsValidPlatformAccountAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (!user.IsActive || user.RequiresPasswordSetup || user.AccountType != AccountType.Platform)
        {
            return false;
        }

        if (!await userManager.IsInRoleAsync(user, PlatformRoles.PlatformAdmin))
        {
            return false;
        }

        return !await context.TenantMemberships
            .AsNoTracking()
            .AnyAsync(membership => membership.UserId == user.Id, cancellationToken);
    }

    public async Task<long?> ResolveTenantAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!user.IsActive || user.RequiresPasswordSetup || user.AccountType != AccountType.Tenant
            || await userManager.IsInRoleAsync(user, PlatformRoles.PlatformAdmin))
        {
            return null;
        }

        long[] tenantIds = await context.TenantMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == user.Id
                && membership.Status == TenantMembershipStatus.Active
                && membership.Tenant != null
                && membership.Tenant.Status == TenantStatus.Active)
            .Select(membership => membership.TenantId)
            .Take(2)
            .ToArrayAsync(cancellationToken);

        return tenantIds.Length == 1 ? tenantIds[0] : null;
    }
}
