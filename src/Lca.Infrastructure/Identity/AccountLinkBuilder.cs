using Lca.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

namespace Lca.Infrastructure.Identity;

public sealed class AccountLinkBuilder(IOptions<AccountRecoveryOptions> options)
{
    private readonly Uri baseUri = new(options.Value.PublicFrontendBaseUrl.TrimEnd('/') + "/");

    public string InitialSetup(string userId, string token) => Build("password-setup", userId, token);
    public string TenantReset(string userId, string token) => Build("reset-password", userId, token);
    public string PlatformReset(string userId, string token) => Build("platform/reset-password", userId, token);

    private string Build(string path, string userId, string token) => new Uri(
        baseUri,
        $"{path}?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}").AbsoluteUri;
}
