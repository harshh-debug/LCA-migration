using System.Text;

using Lca.Core.Security;
using Lca.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

namespace Lca.Infrastructure.Identity;

public sealed class DevelopmentAccountEmailSender(IOptions<AccountRecoveryOptions> options) : IAccountEmailSender
{
    private readonly string mailDirectory = options.Value.DevelopmentMailDirectory;

    public Task SendInitialPasswordSetupAsync(string recipientEmail, string setupUrl, CancellationToken cancellationToken) =>
        WriteAsync(recipientEmail, "Set up your LCA account", setupUrl, cancellationToken);

    public Task SendTenantPasswordResetAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken) =>
        WriteAsync(recipientEmail, "Reset your LCA password", resetUrl, cancellationToken);

    public Task SendPlatformPasswordResetAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken) =>
        WriteAsync(recipientEmail, "Reset your LCA Platform Owner password", resetUrl, cancellationToken);

    private async Task WriteAsync(string recipientEmail, string subject, string actionUrl, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(mailDirectory);
        string fileName = $"account-email-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.eml";
        string body = $"To: {recipientEmail}\nSubject: {subject}\n\nOpen this link to continue:\n{actionUrl}\n";
        await File.WriteAllTextAsync(Path.Combine(mailDirectory, fileName), body, Encoding.UTF8, cancellationToken);
    }
}

public sealed class UnavailableAccountEmailSender : IAccountEmailSender
{
    private static InvalidOperationException Error() => new("A production transactional email provider is not configured.");
    public Task SendInitialPasswordSetupAsync(string recipientEmail, string setupUrl, CancellationToken cancellationToken) => Task.FromException(Error());
    public Task SendTenantPasswordResetAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken) => Task.FromException(Error());
    public Task SendPlatformPasswordResetAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken) => Task.FromException(Error());
}
