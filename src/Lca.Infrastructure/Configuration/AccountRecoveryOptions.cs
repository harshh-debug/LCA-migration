namespace Lca.Infrastructure.Configuration;

public sealed class AccountRecoveryOptions
{
    public const string SectionName = "AccountRecovery";

    public string PublicFrontendBaseUrl { get; init; } = "http://localhost:3000";
    public int ForgotRequestsPerIp { get; init; } = 5;
    public int ForgotWindowMinutes { get; init; } = 15;
    public int TokenAttemptsPerIp { get; init; } = 10;
    public int TokenAttemptWindowMinutes { get; init; } = 15;
    public int MinimumEmailIntervalMinutes { get; init; } = 5;
    public int MaximumEmailsPerHour { get; init; } = 3;
    public int InitialSetupTokenHours { get; init; } = 24;
    public int PasswordResetTokenMinutes { get; init; } = 60;
    public string DevelopmentMailDirectory { get; init; } = "/tmp/lca-development-mail";

    public bool IsValid => Uri.TryCreate(PublicFrontendBaseUrl, UriKind.Absolute, out Uri? uri)
        && uri.Scheme is "http" or "https"
        && ForgotRequestsPerIp > 0
        && ForgotWindowMinutes > 0
        && TokenAttemptsPerIp > 0
        && TokenAttemptWindowMinutes > 0
        && MinimumEmailIntervalMinutes > 0
        && MaximumEmailsPerHour > 0
        && InitialSetupTokenHours > 0
        && PasswordResetTokenMinutes > 0
        && !string.IsNullOrWhiteSpace(DevelopmentMailDirectory);
}
