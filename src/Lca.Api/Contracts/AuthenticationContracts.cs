using System.ComponentModel.DataAnnotations;

namespace Lca.Api.Contracts;

public sealed class LoginRequest
{
    [Required]
    [MaxLength(256)]
    public required string UserNameOrEmail { get; init; }

    [Required]
    [MaxLength(256)]
    public required string Password { get; init; }
}

public sealed record AccessTokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);

public sealed record CurrentAccountResponse(string UserId, string AccountType, long? TenantId);

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }
}

public sealed class CompletePasswordRequest
{
    [Required, MaxLength(450)]
    public required string UserId { get; init; }

    [Required, MaxLength(4096)]
    public required string Token { get; init; }

    [Required, MaxLength(256)]
    public required string NewPassword { get; init; }
}

public sealed record ForgotPasswordResponse(string Message);
