using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Lca.Api.Configuration;
using Lca.Core.Security;
using Lca.Infrastructure.Identity;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lca.Api.Security;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions jwtOptions = options.Value;

    public IssuedAccessToken IssuePlatformToken(ApplicationUser user) => Issue(
        user,
        [new Claim("role", PlatformRoles.PlatformAdmin)]);

    public IssuedAccessToken IssueTenantToken(ApplicationUser user, long tenantId) => Issue(
        user,
        [new Claim(TrustedClaimTypes.TenantId, tenantId.ToString(CultureInfo.InvariantCulture))]);

    private IssuedAccessToken Issue(ApplicationUser user, IReadOnlyCollection<Claim> scopeClaims)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset expires = now.AddMinutes(jwtOptions.AccessTokenMinutes);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(TrustedClaimTypes.AccountType, user.AccountType.ToString().ToLowerInvariant()),
            new(TrustedClaimTypes.SecurityStamp, user.SecurityStamp ?? string.Empty),
            .. scopeClaims,
        ];
        SigningCredentials credentials = new(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            jwtOptions.Issuer,
            jwtOptions.Audience,
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            credentials);

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAtUtc);
