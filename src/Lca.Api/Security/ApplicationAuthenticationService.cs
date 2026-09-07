using Lca.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

namespace Lca.Api.Security;

public sealed class ApplicationAuthenticationService(
    UserManager<ApplicationUser> userManager,
    AccountScopeValidator scopeValidator,
    JwtTokenIssuer tokenIssuer)
{
    public async Task<IssuedAccessToken?> LoginPlatformAsync(
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await FindUserAsync(userNameOrEmail);
        if (user is null
            || !await userManager.CheckPasswordAsync(user, password)
            || !await scopeValidator.IsValidPlatformAccountAsync(user, cancellationToken))
        {
            return null;
        }

        return tokenIssuer.IssuePlatformToken(user);
    }

    public async Task<IssuedAccessToken?> LoginTenantAsync(
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await FindUserAsync(userNameOrEmail);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        long? tenantId = await scopeValidator.ResolveTenantAsync(user, cancellationToken);
        return tenantId.HasValue ? tokenIssuer.IssueTenantToken(user, tenantId.Value) : null;
    }

    private async Task<ApplicationUser?> FindUserAsync(string userNameOrEmail)
    {
        string identifier = userNameOrEmail.Trim();
        return await userManager.FindByNameAsync(identifier)
            ?? await userManager.FindByEmailAsync(identifier);
    }
}
