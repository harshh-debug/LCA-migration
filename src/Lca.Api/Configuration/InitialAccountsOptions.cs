namespace Lca.Api.Configuration;

public sealed class InitialAccountsOptions
{
    public const string SectionName = "InitialAccounts";

    public string PlatformEmail { get; init; } = string.Empty;

    public string PlatformPassword { get; init; } = string.Empty;

    public string TenantEmail { get; init; } = string.Empty;

    public string TenantPassword { get; init; } = string.Empty;

    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(PlatformEmail)
        || !string.IsNullOrWhiteSpace(PlatformPassword)
        || !string.IsNullOrWhiteSpace(TenantEmail)
        || !string.IsNullOrWhiteSpace(TenantPassword);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PlatformEmail)
        && !string.IsNullOrWhiteSpace(PlatformPassword)
        && !string.IsNullOrWhiteSpace(TenantEmail)
        && !string.IsNullOrWhiteSpace(TenantPassword);
}
