using Lca.Api.Configuration;
using Lca.Core.Security;
using Lca.Core.Tenancy;
using Lca.Infrastructure.Identity;
using Lca.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lca.Api.Security;

public sealed class InitialAccountSeeder(
    IOptions<InitialAccountsOptions> options,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    LcaDbContext context,
    TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        InitialAccountsOptions settings = options.Value;
        if (!settings.HasAnyValue)
        {
            return;
        }

        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("All InitialAccounts values are required when development account provisioning is enabled.");
        }

        if (string.Equals(settings.PlatformEmail.Trim(), settings.TenantEmail.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Platform and tenant accounts must use distinct email addresses.");
        }

        ApplicationUser platformUser = await GetOrCreateUserAsync(
            settings.PlatformEmail,
            settings.PlatformPassword,
            AccountType.Platform);
        if (await context.TenantMemberships.AnyAsync(
                membership => membership.UserId == platformUser.Id,
                cancellationToken))
        {
            throw new InvalidOperationException("The platform account cannot have a tenant membership.");
        }

        if (!await roleManager.RoleExistsAsync(PlatformRoles.PlatformAdmin))
        {
            IdentityResult roleResult = await roleManager.CreateAsync(new IdentityRole(PlatformRoles.PlatformAdmin));
            EnsureSucceeded(roleResult, "create the PlatformAdmin role");
        }

        if (!await userManager.IsInRoleAsync(platformUser, PlatformRoles.PlatformAdmin))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(platformUser, PlatformRoles.PlatformAdmin),
                "assign the PlatformAdmin role");
        }

        ApplicationUser tenantUser = await GetOrCreateUserAsync(
            settings.TenantEmail,
            settings.TenantPassword,
            AccountType.Tenant);
        if (await userManager.IsInRoleAsync(tenantUser, PlatformRoles.PlatformAdmin))
        {
            throw new InvalidOperationException("The tenant account cannot have the PlatformAdmin role.");
        }

        Tenant tenant = await context.Tenants.SingleOrDefaultAsync(item => item.Id == 1, cancellationToken)
            ?? throw new InvalidOperationException("The initial LCA tenant has not been migrated.");
        if (tenant.Status != TenantStatus.Active)
        {
            throw new InvalidOperationException("The initial LCA tenant is not active.");
        }

        TenantMembership[] activeMemberships = await context.TenantMemberships
            .Where(membership => membership.UserId == tenantUser.Id
                && membership.Status == TenantMembershipStatus.Active)
            .ToArrayAsync(cancellationToken);
        if (activeMemberships.Length > 1 || activeMemberships.SingleOrDefault()?.TenantId is long activeTenantId && activeTenantId != 1)
        {
            throw new InvalidOperationException("The tenant account already has a conflicting active membership.");
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        TenantMembership? membership = await context.TenantMemberships.FindAsync(
            [tenantUser.Id, 1L],
            cancellationToken);
        if (membership is null)
        {
            context.TenantMemberships.Add(new TenantMembership
            {
                UserId = tenantUser.Id,
                TenantId = 1,
                Status = TenantMembershipStatus.Active,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }
        else if (membership.Status != TenantMembershipStatus.Active)
        {
            membership.Status = TenantMembershipStatus.Active;
            membership.UpdatedAtUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> GetOrCreateUserAsync(
        string email,
        string password,
        AccountType accountType)
    {
        string normalizedEmail = email.Trim();
        ApplicationUser? user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is not null)
        {
            if (user.AccountType != accountType)
            {
                throw new InvalidOperationException("An initial account exists with the wrong account type.");
            }

            return user;
        }

        user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            AccountType = accountType,
            IsActive = true,
        };
        EnsureSucceeded(await userManager.CreateAsync(user, password), $"create the {accountType} account");
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unable to {operation}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}
