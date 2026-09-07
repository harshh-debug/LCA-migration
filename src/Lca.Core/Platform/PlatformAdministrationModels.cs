using Lca.Core.Tenancy;

namespace Lca.Core.Platform;

public sealed record PlatformDashboard(
    int TotalTenants,
    int ActiveTenants,
    int InactiveTenants,
    int TotalTenantUsers,
    int EffectiveActiveTenantUsers,
    int TotalProducts,
    int TotalCustomers,
    IReadOnlyCollection<TenantUsageSummary> TenantUsage,
    IReadOnlyCollection<TenantSummary> RecentTenants,
    IReadOnlyCollection<PlatformAuditRecord> RecentActivity,
    DateTimeOffset GeneratedAtUtc);

public sealed record TenantUsageSummary(long TenantId, string Name, int TenantUsers, int Products, int Customers);

public sealed record TenantSummary(
    long Id,
    string Name,
    string Slug,
    TenantStatus Status,
    int TenantUsers,
    int Products,
    int Customers,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record TenantUserSummary(
    string Id,
    string Email,
    bool IsActive,
    bool EmailConfirmed,
    bool RequiresPasswordSetup,
    long TenantId,
    TenantMembershipStatus MembershipStatus);

public sealed record PlatformAuditRecord(
    long Id,
    string ActorUserId,
    string Action,
    string TargetType,
    string TargetId,
    long? TenantId,
    DateTime OccurredAtUtc,
    string? Details);

public sealed record PlatformPage<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

public sealed record ProvisionedTenantUser(TenantUserSummary User, AccountEmailDeliveryResult SetupEmail);

public enum AccountEmailDeliveryResult
{
    Sent,
    Throttled,
    TokenGenerationFailed,
    DeliveryFailed,
}

public interface IPlatformAdministrationService
{
    Task<PlatformDashboard> GetDashboardAsync(CancellationToken cancellationToken);
    Task<PlatformPage<TenantSummary>> GetTenantsAsync(string? search, TenantStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<TenantSummary?> GetTenantAsync(long tenantId, CancellationToken cancellationToken);
    Task<TenantSummary> CreateTenantAsync(string name, string slug, string actorUserId, string correlationId, CancellationToken cancellationToken);
    Task<TenantSummary?> UpdateTenantAsync(long tenantId, string name, string slug, string actorUserId, string correlationId, CancellationToken cancellationToken);
    Task<TenantSummary?> SetTenantStatusAsync(long tenantId, TenantStatus status, string actorUserId, string correlationId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TenantUserSummary>?> GetTenantUsersAsync(long tenantId, CancellationToken cancellationToken);
    Task<TenantUserSummary?> GetTenantUserAsync(string userId, CancellationToken cancellationToken);
    Task<ProvisionedTenantUser> ProvisionTenantUserAsync(long tenantId, string email, string actorUserId, string correlationId, CancellationToken cancellationToken);
    Task<TenantUserSummary?> SetTenantUserStatusAsync(string userId, bool isActive, string actorUserId, string correlationId, CancellationToken cancellationToken);
    Task<TenantUserSummary?> SetMembershipStatusAsync(long tenantId, string userId, TenantMembershipStatus status, string actorUserId, string correlationId, CancellationToken cancellationToken);
    Task<AccountEmailDeliveryResult?> ResendInitialSetupEmailAsync(string userId, string actorUserId, string correlationId, CancellationToken cancellationToken);
    Task<PlatformPage<PlatformAuditRecord>> GetAuditAsync(long? tenantId, string? action, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class PlatformAdministrationConflictException(string message) : InvalidOperationException(message);
