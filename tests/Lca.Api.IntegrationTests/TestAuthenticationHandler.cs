using System.Security.Claims;
using System.Text.Encodings.Web;
using Lca.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lca.Api.IntegrationTests;

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        ];

        if (Request.Headers.TryGetValue("X-Test-Tenant", out var tenantId))
        {
            claims.Add(new Claim(TrustedClaimTypes.TenantId, tenantId.ToString()));
        }

        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));
        AuthenticationTicket ticket = new(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
