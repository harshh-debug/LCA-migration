using Lca.Infrastructure.Configuration;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lca.Infrastructure.Identity;

public sealed class InitialPasswordSetupTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<AccountRecoveryOptions> recoveryOptions,
    ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(
        dataProtectionProvider,
        Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions
        {
            Name = ProviderName,
            TokenLifespan = TimeSpan.FromHours(recoveryOptions.Value.InitialSetupTokenHours),
        }),
        logger)
{
    public const string ProviderName = "InitialPasswordSetup";
    public const string Purpose = "initial-password-setup";
}
