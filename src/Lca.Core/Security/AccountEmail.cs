namespace Lca.Core.Security;

public interface IAccountEmailSender
{
    Task SendInitialPasswordSetupAsync(string recipientEmail, string setupUrl, CancellationToken cancellationToken);
    Task SendTenantPasswordResetAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken);
    Task SendPlatformPasswordResetAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken);
}

public interface IAccountRecoveryService
{
    Task RequestTenantPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task RequestPlatformPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task<AccountRecoveryResult> CompleteInitialPasswordSetupAsync(string userId, string token, string newPassword, CancellationToken cancellationToken);
    Task<AccountRecoveryResult> ResetTenantPasswordAsync(string userId, string token, string newPassword, CancellationToken cancellationToken);
    Task<AccountRecoveryResult> ResetPlatformPasswordAsync(string userId, string token, string newPassword, CancellationToken cancellationToken);
}

public sealed record AccountRecoveryResult(bool Succeeded, bool IsConflict, IReadOnlyCollection<string> Errors)
{
    public static AccountRecoveryResult Success { get; } = new(true, false, []);
    public static AccountRecoveryResult Invalid { get; } = new(false, false, ["The password link is invalid or has expired."]);
    public static AccountRecoveryResult AlreadyCompleted { get; } = new(false, true, ["Initial password setup has already been completed."]);
}
