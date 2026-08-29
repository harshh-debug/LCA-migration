namespace Lca.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Issuer)
        && !string.IsNullOrWhiteSpace(Audience)
        && SigningKey.Length >= 32;
}
