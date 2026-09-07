using Lca.Core.Security;
using Lca.Core.Tenancy;
using Lca.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lca.Infrastructure.Identity;

public sealed partial class AccountRecoveryService(
    UserManager<ApplicationUser> userManager,
    LcaDbContext context,
    IAccountEmailSender emailSender,
    IAccountRecoveryThrottle throttle,
    AccountLinkBuilder linkBuilder,
    TimeProvider timeProvider,
    ILogger<AccountRecoveryService> logger) : IAccountRecoveryService
{
    public Task RequestTenantPasswordResetAsync(string email, CancellationToken cancellationToken) =>
        RequestPasswordResetAsync(email, AccountType.Tenant, cancellationToken);

    public Task RequestPlatformPasswordResetAsync(string email, CancellationToken cancellationToken) =>
        RequestPasswordResetAsync(email, AccountType.Platform, cancellationToken);

    public async Task<AccountRecoveryResult> CompleteInitialPasswordSetupAsync(
        string userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        if (user is null
            || user.AccountType != AccountType.Tenant
            || !user.IsActive
            || !await HasValidTenantScopeAsync(user, cancellationToken))
        {
            return AccountRecoveryResult.Invalid;
        }

        if (!user.RequiresPasswordSetup)
        {
            return AccountRecoveryResult.AlreadyCompleted;
        }

        if (!await userManager.VerifyUserTokenAsync(
                user,
                InitialPasswordSetupTokenProvider.ProviderName,
                InitialPasswordSetupTokenProvider.Purpose,
                token)
            || await userManager.HasPasswordAsync(user))
        {
            return AccountRecoveryResult.Invalid;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        IdentityResult passwordResult = await userManager.AddPasswordAsync(user, newPassword);
        if (!passwordResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(passwordResult);
        }

        user.EmailConfirmed = true;
        user.RequiresPasswordSetup = false;
        IdentityResult updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(updateResult);
        }

        IdentityResult stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(stampResult);
        }

        await transaction.CommitAsync(cancellationToken);
        return AccountRecoveryResult.Success;
    }

    public Task<AccountRecoveryResult> ResetTenantPasswordAsync(
        string userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken) =>
        ResetPasswordAsync(userId, token, newPassword, AccountType.Tenant, cancellationToken);

    public Task<AccountRecoveryResult> ResetPlatformPasswordAsync(
        string userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken) =>
        ResetPasswordAsync(userId, token, newPassword, AccountType.Platform, cancellationToken);

    private async Task RequestPasswordResetAsync(
        string email,
        AccountType accountType,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = userManager.NormalizeEmail(email.Trim());
        ApplicationUser? user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null
            || user.AccountType != accountType
            || !user.IsActive
            || user.RequiresPasswordSetup
            || string.IsNullOrWhiteSpace(user.Email)
            || !await IsEligibleAsync(user, accountType, cancellationToken)
            || !throttle.TryAcquire(normalizedEmail, timeProvider.GetUtcNow()))
        {
            return;
        }

        try
        {
            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            string url = accountType == AccountType.Platform
                ? linkBuilder.PlatformReset(user.Id, token)
                : linkBuilder.TenantReset(user.Id, token);
            if (accountType == AccountType.Platform)
            {
                await emailSender.SendPlatformPasswordResetAsync(user.Email, url, cancellationToken);
            }
            else
            {
                await emailSender.SendTenantPasswordResetAsync(user.Email, url, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            LogRecoveryDeliveryFailure(logger, user.Id, accountType, exception);
        }
    }

    private async Task<AccountRecoveryResult> ResetPasswordAsync(
        string userId,
        string token,
        string newPassword,
        AccountType accountType,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        if (user is null
            || user.AccountType != accountType
            || !user.IsActive
            || user.RequiresPasswordSetup
            || !await IsEligibleAsync(user, accountType, cancellationToken))
        {
            return AccountRecoveryResult.Invalid;
        }

        IdentityResult result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return result.Errors.Any(error => error.Code is "InvalidToken")
                ? AccountRecoveryResult.Invalid
                : Failure(result);
        }

        IdentityResult stampResult = await userManager.UpdateSecurityStampAsync(user);
        return stampResult.Succeeded ? AccountRecoveryResult.Success : Failure(stampResult);
    }

    private Task<bool> IsEligibleAsync(ApplicationUser user, AccountType accountType, CancellationToken cancellationToken) =>
        accountType == AccountType.Platform
            ? HasValidPlatformScopeAsync(user, cancellationToken)
            : HasValidTenantScopeAsync(user, cancellationToken);

    private async Task<bool> HasValidPlatformScopeAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        await userManager.IsInRoleAsync(user, PlatformRoles.PlatformAdmin)
        && !await context.TenantMemberships.AsNoTracking()
            .AnyAsync(membership => membership.UserId == user.Id, cancellationToken);

    private async Task<bool> HasValidTenantScopeAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (await userManager.IsInRoleAsync(user, PlatformRoles.PlatformAdmin))
        {
            return false;
        }

        int count = await context.TenantMemberships.AsNoTracking()
            .Where(membership => membership.UserId == user.Id
                && membership.Status == TenantMembershipStatus.Active
                && membership.Tenant != null
                && membership.Tenant.Status == TenantStatus.Active)
            .Take(2)
            .CountAsync(cancellationToken);
        return count == 1;
    }

    private static AccountRecoveryResult Failure(IdentityResult result) =>
        new(false, false, result.Errors.Select(error => error.Description).ToArray());

    [LoggerMessage(EventId = 2101, Level = LogLevel.Warning,
        Message = "Account recovery email delivery failed for user {UserId} in scope {AccountType}")]
    private static partial void LogRecoveryDeliveryFailure(
        ILogger logger,
        string userId,
        AccountType accountType,
        Exception exception);
}
