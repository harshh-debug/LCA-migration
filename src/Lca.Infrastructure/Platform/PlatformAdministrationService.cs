using System.Text.Json;
using System.Globalization;

using Lca.Core.Platform;
using Lca.Core.Security;
using Lca.Core.Tenancy;
using Lca.Infrastructure.Identity;
using Lca.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lca.Infrastructure.Platform;

public sealed partial class PlatformAdministrationService(
    LcaDbContext context,
    UserManager<ApplicationUser> userManager,
    IAccountEmailSender emailSender,
    IAccountRecoveryThrottle throttle,
    AccountLinkBuilder linkBuilder,
    TimeProvider timeProvider,
    ILogger<PlatformAdministrationService> logger) : IPlatformAdministrationService
{
    public async Task<PlatformDashboard> GetDashboardAsync(CancellationToken cancellationToken)
    {
        Tenant[] tenants = await context.Tenants.AsNoTracking().OrderBy(value => value.Id).ToArrayAsync(cancellationToken);
        long[] ids = tenants.Select(value => value.Id).ToArray();
        Dictionary<long, int> users = await context.TenantMemberships.AsNoTracking()
            .Where(value => ids.Contains(value.TenantId))
            .GroupBy(value => value.TenantId).ToDictionaryAsync(value => value.Key, value => value.Count(), cancellationToken);
        Dictionary<long, int> products = await context.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(value => ids.Contains(value.TenantId))
            .GroupBy(value => value.TenantId).ToDictionaryAsync(value => value.Key, value => value.Count(), cancellationToken);
        Dictionary<long, int> customers = await context.Customers.IgnoreQueryFilters().AsNoTracking()
            .Where(value => ids.Contains(value.TenantId))
            .GroupBy(value => value.TenantId).ToDictionaryAsync(value => value.Key, value => value.Count(), cancellationToken);

        int effectiveUsers = await context.TenantMemberships.AsNoTracking()
            .Where(value => value.Status == TenantMembershipStatus.Active
                && value.Tenant != null && value.Tenant.Status == TenantStatus.Active)
            .Join(context.Users.Where(value => value.AccountType == AccountType.Tenant && value.IsActive && !value.RequiresPasswordSetup),
                membership => membership.UserId, user => user.Id, (_, _) => 1)
            .CountAsync(cancellationToken);

        PlatformAuditEntry[] recentEntries = await context.PlatformAuditEntries.AsNoTracking()
            .OrderByDescending(value => value.OccurredAtUtc).Take(10).ToArrayAsync(cancellationToken);
        PlatformAuditRecord[] recentActivity = recentEntries.Select(ToAuditRecord).ToArray();

        TenantUsageSummary[] usage = tenants.Select(value => new TenantUsageSummary(
            value.Id, value.Name, users.GetValueOrDefault(value.Id), products.GetValueOrDefault(value.Id), customers.GetValueOrDefault(value.Id))).ToArray();
        TenantSummary[] recent = tenants.OrderByDescending(value => value.CreatedAtUtc).Take(5)
            .Select(value => ToTenantSummary(value, users, products, customers)).ToArray();

        return new PlatformDashboard(
            tenants.Length,
            tenants.Count(value => value.Status == TenantStatus.Active),
            tenants.Count(value => value.Status == TenantStatus.Inactive),
            users.Values.Sum(),
            effectiveUsers,
            products.Values.Sum(),
            customers.Values.Sum(),
            usage,
            recent,
            recentActivity,
            timeProvider.GetUtcNow());
    }

    public async Task<PlatformPage<TenantSummary>> GetTenantsAsync(
        string? search,
        TenantStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = context.Tenants.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(value => value.Name.Contains(term) || value.Slug.Contains(term));
        }
        if (status.HasValue) query = query.Where(value => value.Status == status.Value);

        int total = await query.CountAsync(cancellationToken);
        Tenant[] tenants = await query.OrderBy(value => value.Name).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        long[] ids = tenants.Select(value => value.Id).ToArray();
        Dictionary<long, int> users = await context.TenantMemberships.AsNoTracking().Where(value => ids.Contains(value.TenantId))
            .GroupBy(value => value.TenantId).ToDictionaryAsync(value => value.Key, value => value.Count(), cancellationToken);
        Dictionary<long, int> products = await context.Products.IgnoreQueryFilters().AsNoTracking().Where(value => ids.Contains(value.TenantId))
            .GroupBy(value => value.TenantId).ToDictionaryAsync(value => value.Key, value => value.Count(), cancellationToken);
        Dictionary<long, int> customers = await context.Customers.IgnoreQueryFilters().AsNoTracking().Where(value => ids.Contains(value.TenantId))
            .GroupBy(value => value.TenantId).ToDictionaryAsync(value => value.Key, value => value.Count(), cancellationToken);
        return new PlatformPage<TenantSummary>(tenants.Select(value => ToTenantSummary(value, users, products, customers)).ToArray(), page, pageSize, total);
    }

    public async Task<TenantSummary?> GetTenantAsync(long tenantId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await context.Tenants.AsNoTracking().SingleOrDefaultAsync(value => value.Id == tenantId, cancellationToken);
        return tenant is null ? null : await BuildTenantSummaryAsync(tenant, cancellationToken);
    }

    public async Task<TenantSummary> CreateTenantAsync(string name, string slug, string actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        string normalizedSlug = NormalizeSlug(slug);
        if (await context.Tenants.AnyAsync(value => value.Slug == normalizedSlug, cancellationToken))
            throw new PlatformAdministrationConflictException("A Tenant with this slug already exists.");
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Tenant tenant = new() { Name = name.Trim(), Slug = normalizedSlug, Status = TenantStatus.Active, CreatedAtUtc = now, UpdatedAtUtc = now };
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);
        context.PlatformAuditEntries.Add(Audit(actorUserId, "TenantCreated", "Tenant", tenant.Id.ToString(CultureInfo.InvariantCulture), tenant.Id, correlationId, new { tenant.Name, tenant.Slug }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await BuildTenantSummaryAsync(tenant, cancellationToken);
    }

    public async Task<TenantSummary?> UpdateTenantAsync(long tenantId, string name, string slug, string actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await context.Tenants.SingleOrDefaultAsync(value => value.Id == tenantId, cancellationToken);
        if (tenant is null) return null;
        string normalizedSlug = NormalizeSlug(slug);
        if (await context.Tenants.AnyAsync(value => value.Id != tenantId && value.Slug == normalizedSlug, cancellationToken))
            throw new PlatformAdministrationConflictException("A Tenant with this slug already exists.");
        var before = new { tenant.Name, tenant.Slug };
        tenant.Name = name.Trim(); tenant.Slug = normalizedSlug; tenant.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        context.PlatformAuditEntries.Add(Audit(actorUserId, "TenantUpdated", "Tenant", tenant.Id.ToString(CultureInfo.InvariantCulture), tenant.Id, correlationId, new { before, after = new { tenant.Name, tenant.Slug } }));
        await context.SaveChangesAsync(cancellationToken);
        return await BuildTenantSummaryAsync(tenant, cancellationToken);
    }

    public async Task<TenantSummary?> SetTenantStatusAsync(long tenantId, TenantStatus status, string actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await context.Tenants.SingleOrDefaultAsync(value => value.Id == tenantId, cancellationToken);
        if (tenant is null) return null;
        TenantStatus previous = tenant.Status;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        tenant.Status = status; tenant.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (previous != status && status == TenantStatus.Inactive)
        {
            string[] userIds = await context.TenantMemberships.AsNoTracking()
                .Where(value => value.TenantId == tenantId)
                .Select(value => value.UserId)
                .ToArrayAsync(cancellationToken);
            ApplicationUser[] users = await context.Users
                .Where(value => userIds.Contains(value.Id) && value.AccountType == AccountType.Tenant)
                .ToArrayAsync(cancellationToken);
            foreach (ApplicationUser user in users)
            {
                EnsureIdentitySucceeded(await userManager.UpdateSecurityStampAsync(user));
            }
        }
        context.PlatformAuditEntries.Add(Audit(actorUserId, "TenantStatusChanged", "Tenant", tenant.Id.ToString(CultureInfo.InvariantCulture), tenant.Id, correlationId, new { previous, status }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await BuildTenantSummaryAsync(tenant, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TenantUserSummary>?> GetTenantUsersAsync(long tenantId, CancellationToken cancellationToken)
    {
        if (!await context.Tenants.AnyAsync(value => value.Id == tenantId, cancellationToken)) return null;
        TenantUserJoin[] values = await (
            from user in context.Users.AsNoTracking()
            join membership in context.TenantMemberships.AsNoTracking() on user.Id equals membership.UserId
            where user.AccountType == AccountType.Tenant && membership.TenantId == tenantId
            orderby user.Email
            select new TenantUserJoin(user, membership))
            .ToArrayAsync(cancellationToken);
        return values.Select(value => ToTenantUser(value.User, value.Membership)).ToArray();
    }

    public async Task<TenantUserSummary?> GetTenantUserAsync(string userId, CancellationToken cancellationToken)
    {
        TenantUserJoin? value = await (
            from user in context.Users.AsNoTracking()
            join membership in context.TenantMemberships.AsNoTracking() on user.Id equals membership.UserId
            where user.AccountType == AccountType.Tenant && user.Id == userId
            select new TenantUserJoin(user, membership))
            .SingleOrDefaultAsync(cancellationToken);
        return value is null ? null : ToTenantUser(value.User, value.Membership);
    }

    public async Task<ProvisionedTenantUser> ProvisionTenantUserAsync(
        long tenantId, string email, string actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await context.Tenants.SingleOrDefaultAsync(value => value.Id == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Tenant was not found.");
        if (tenant.Status != TenantStatus.Active)
            throw new PlatformAdministrationConflictException("A user cannot be provisioned for an inactive Tenant.");
        string canonicalEmail = email.Trim();
        if (await userManager.FindByNameAsync(canonicalEmail) is not null || await userManager.FindByEmailAsync(canonicalEmail) is not null)
            throw new PlatformAdministrationConflictException("An account with this email already exists.");

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        ApplicationUser user = new() { UserName = canonicalEmail, Email = canonicalEmail, EmailConfirmed = false, AccountType = AccountType.Tenant, IsActive = true, RequiresPasswordSetup = true };
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            IdentityResult create = await userManager.CreateAsync(user);
            EnsureIdentitySucceeded(create);
            TenantMembership membership = new() { UserId = user.Id, TenantId = tenantId, Status = TenantMembershipStatus.Active, CreatedAtUtc = now, UpdatedAtUtc = now };
            context.TenantMemberships.Add(membership);
            context.PlatformAuditEntries.Add(Audit(actorUserId, "TenantUserProvisioned", "ApplicationUser", user.Id, tenantId, correlationId, new { Email = canonicalEmail }));
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        AccountEmailDeliveryResult delivery = await SendInitialSetupEmailAsync(user, tenantId, actorUserId, correlationId, false, cancellationToken);
        return new ProvisionedTenantUser(new TenantUserSummary(user.Id, canonicalEmail, true, false, true, tenantId, TenantMembershipStatus.Active), delivery);
    }

    public async Task<TenantUserSummary?> SetTenantUserStatusAsync(string userId, bool isActive, string actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        TenantMembership? membership = user is null ? null : await context.TenantMemberships.SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);
        if (user is null || user.AccountType != AccountType.Tenant || membership is null) return null;
        bool previous = user.IsActive;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        user.IsActive = isActive;
        EnsureIdentitySucceeded(await userManager.UpdateAsync(user));
        if (previous && !isActive)
        {
            EnsureIdentitySucceeded(await userManager.UpdateSecurityStampAsync(user));
        }
        context.PlatformAuditEntries.Add(Audit(actorUserId, "TenantUserStatusChanged", "ApplicationUser", user.Id, membership.TenantId, correlationId, new { previous, isActive }));
        await context.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return ToTenantUser(user, membership);
    }

    public async Task<TenantUserSummary?> SetMembershipStatusAsync(long tenantId, string userId, TenantMembershipStatus status, string actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        TenantMembership? membership = await context.TenantMemberships.SingleOrDefaultAsync(value => value.UserId == userId && value.TenantId == tenantId, cancellationToken);
        if (user is null || user.AccountType != AccountType.Tenant || membership is null) return null;
        TenantMembershipStatus previous = membership.Status;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        membership.Status = status; membership.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (previous != status && status == TenantMembershipStatus.Inactive)
        {
            EnsureIdentitySucceeded(await userManager.UpdateSecurityStampAsync(user));
        }
        context.PlatformAuditEntries.Add(Audit(actorUserId, "TenantMembershipStatusChanged", "TenantMembership", user.Id, tenantId, correlationId, new { previous, status }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToTenantUser(user, membership);
    }

    public async Task<AccountEmailDeliveryResult?> ResendInitialSetupEmailAsync(string userId, string actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        TenantMembership? membership = user is null ? null : await context.TenantMemberships.Include(value => value.Tenant).SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);
        if (user is null || user.AccountType != AccountType.Tenant || membership is null) return null;
        if (!user.RequiresPasswordSetup) throw new PlatformAdministrationConflictException("Initial password setup has already been completed.");
        if (!user.IsActive || membership.Status != TenantMembershipStatus.Active || membership.Tenant?.Status != TenantStatus.Active)
            throw new PlatformAdministrationConflictException("The account, membership, and Tenant must be active before setup email can be sent.");
        return await SendInitialSetupEmailAsync(user, membership.TenantId, actorUserId, correlationId, true, cancellationToken);
    }

    public async Task<PlatformPage<PlatformAuditRecord>> GetAuditAsync(long? tenantId, string? action, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<PlatformAuditEntry> query = context.PlatformAuditEntries.AsNoTracking();
        if (tenantId.HasValue) query = query.Where(value => value.TenantId == tenantId.Value);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(value => value.Action == action.Trim());
        if (fromUtc.HasValue) query = query.Where(value => value.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(value => value.OccurredAtUtc <= toUtc.Value);
        int count = await query.CountAsync(cancellationToken);
        PlatformAuditEntry[] entries = await query.OrderByDescending(value => value.OccurredAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        PlatformAuditRecord[] items = entries.Select(ToAuditRecord).ToArray();
        return new PlatformPage<PlatformAuditRecord>(items, page, pageSize, count);
    }

    private async Task<AccountEmailDeliveryResult> SendInitialSetupEmailAsync(ApplicationUser user, long tenantId, string actor, string correlationId, bool rotateStamp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email)) return AccountEmailDeliveryResult.DeliveryFailed;
        string normalized = userManager.NormalizeEmail(user.Email);
        if (!throttle.TryAcquire(normalized, timeProvider.GetUtcNow())) return AccountEmailDeliveryResult.Throttled;
        await TryAuditAsync(Audit(actor, rotateStamp ? "InitialSetupEmailResendRequested" : "InitialSetupEmailRequested", "ApplicationUser", user.Id, tenantId, correlationId, null), cancellationToken);
        string token;
        try
        {
            if (rotateStamp) EnsureIdentitySucceeded(await userManager.UpdateSecurityStampAsync(user));
            token = await userManager.GenerateUserTokenAsync(user, InitialPasswordSetupTokenProvider.ProviderName, InitialPasswordSetupTokenProvider.Purpose);
        }
        catch (Exception exception)
        {
            LogSetupInitiationFailure(logger, user.Id, exception);
            await TryAuditAsync(Audit(actor, "InitialSetupTokenGenerationFailed", "ApplicationUser", user.Id, tenantId, correlationId, null), cancellationToken);
            return AccountEmailDeliveryResult.TokenGenerationFailed;
        }
        try
        {
            await emailSender.SendInitialPasswordSetupAsync(user.Email, linkBuilder.InitialSetup(user.Id, token), cancellationToken);
        }
        catch (Exception exception)
        {
            LogSetupDeliveryFailure(logger, user.Id, exception);
            await TryAuditAsync(Audit(actor, "InitialSetupEmailDeliveryFailed", "ApplicationUser", user.Id, tenantId, correlationId, null), cancellationToken);
            return AccountEmailDeliveryResult.DeliveryFailed;
        }
        await TryAuditAsync(Audit(actor, rotateStamp ? "InitialSetupEmailResent" : "InitialSetupEmailSent", "ApplicationUser", user.Id, tenantId, correlationId, null), cancellationToken);
        return AccountEmailDeliveryResult.Sent;
    }

    private async Task TryAuditAsync(PlatformAuditEntry entry, CancellationToken cancellationToken)
    {
        try { context.PlatformAuditEntries.Add(entry); await context.SaveChangesAsync(cancellationToken); }
        catch (Exception exception) { context.Entry(entry).State = EntityState.Detached; LogAuditPersistenceFailure(logger, entry.Action, entry.TargetId, exception); }
    }

    private async Task<TenantSummary> BuildTenantSummaryAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        int users = await context.TenantMemberships.AsNoTracking().CountAsync(value => value.TenantId == tenant.Id, cancellationToken);
        int products = await context.Products.IgnoreQueryFilters().AsNoTracking().CountAsync(value => value.TenantId == tenant.Id, cancellationToken);
        int customers = await context.Customers.IgnoreQueryFilters().AsNoTracking().CountAsync(value => value.TenantId == tenant.Id, cancellationToken);
        return new TenantSummary(tenant.Id, tenant.Name, tenant.Slug, tenant.Status, users, products, customers, tenant.CreatedAtUtc, tenant.UpdatedAtUtc);
    }

    private static TenantSummary ToTenantSummary(Tenant value, IReadOnlyDictionary<long, int> users, IReadOnlyDictionary<long, int> products, IReadOnlyDictionary<long, int> customers) =>
        new(value.Id, value.Name, value.Slug, value.Status, users.GetValueOrDefault(value.Id), products.GetValueOrDefault(value.Id), customers.GetValueOrDefault(value.Id), value.CreatedAtUtc, value.UpdatedAtUtc);
    private static TenantUserSummary ToTenantUser(ApplicationUser user, TenantMembership membership) =>
        new(user.Id, user.Email ?? user.UserName ?? string.Empty, user.IsActive, user.EmailConfirmed, user.RequiresPasswordSetup, membership.TenantId, membership.Status);
    private static PlatformAuditRecord ToAuditRecord(PlatformAuditEntry value) =>
        new(value.Id, value.ActorUserId, value.Action, value.TargetType, value.TargetId, value.TenantId, value.OccurredAtUtc, value.Details);
    private PlatformAuditEntry Audit(string actor, string action, string targetType, string targetId, long? tenantId, string correlationId, object? details) =>
        new() { ActorUserId = actor, Action = action, TargetType = targetType, TargetId = targetId, TenantId = tenantId, OccurredAtUtc = timeProvider.GetUtcNow().UtcDateTime, CorrelationId = correlationId, Details = details is null ? null : JsonSerializer.Serialize(details) };
    private static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();
    private static void EnsureIdentitySucceeded(IdentityResult result)
    {
        if (result.Succeeded) return;
        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            throw new PlatformAdministrationConflictException("An account with this email already exists.");
        throw new PlatformAdministrationConflictException(string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    private sealed record TenantUserJoin(ApplicationUser User, TenantMembership Membership);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Warning, Message = "Initial setup token generation failed for user {UserId}")]
    private static partial void LogSetupInitiationFailure(ILogger logger, string userId, Exception exception);
    [LoggerMessage(EventId = 2202, Level = LogLevel.Warning, Message = "Initial setup email delivery failed for user {UserId}")]
    private static partial void LogSetupDeliveryFailure(ILogger logger, string userId, Exception exception);
    [LoggerMessage(EventId = 2203, Level = LogLevel.Error, Message = "Platform audit persistence failed for action {Action} and target {TargetId}")]
    private static partial void LogAuditPersistenceFailure(ILogger logger, string action, string targetId, Exception exception);
}
