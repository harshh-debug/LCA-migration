using Lca.Core.Catalog;
using Lca.Core.Customers;
using Lca.Core.Platform;
using Lca.Core.Security;
using Lca.Infrastructure.Catalog;
using Lca.Infrastructure.Configuration;
using Lca.Infrastructure.Customers;
using Lca.Infrastructure.Identity;
using Lca.Infrastructure.Platform;
using Lca.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lca.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLcaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useDevelopmentEmailSink = false)
    {
        string? connectionString = configuration.GetConnectionString("LcaDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:LcaDatabase is required.");
        }
        services.AddDbContext<LcaDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(InfrastructureAssembly.Marker.Assembly.FullName)));
        AccountRecoveryOptions recoveryOptions = configuration.GetSection(AccountRecoveryOptions.SectionName)
            .Get<AccountRecoveryOptions>() ?? new AccountRecoveryOptions();
        if (!recoveryOptions.IsValid)
        {
            throw new InvalidOperationException("AccountRecovery configuration is invalid.");
        }
        services.AddOptions<AccountRecoveryOptions>()
            .Bind(configuration.GetSection(AccountRecoveryOptions.SectionName))
            .Validate(value => value.IsValid, "AccountRecovery configuration is invalid.")
            .ValidateOnStart();
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromMinutes(recoveryOptions.PasswordResetTokenMinutes));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LcaDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<InitialPasswordSetupTokenProvider>(InitialPasswordSetupTokenProvider.ProviderName);
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IProductWorkbookImportService, ProductWorkbookImportService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerWorkbookImportService, CustomerWorkbookImportService>();
        services.AddSingleton<IAccountRecoveryThrottle, InMemoryAccountRecoveryThrottle>();
        services.AddSingleton<AccountLinkBuilder>();
        services.AddScoped<IAccountRecoveryService, AccountRecoveryService>();
        services.AddScoped<IPlatformAdministrationService, PlatformAdministrationService>();
        services.AddSingleton<IAccountEmailSender>(_ => useDevelopmentEmailSink
            ? new DevelopmentAccountEmailSender(_.GetRequiredService<Microsoft.Extensions.Options.IOptions<AccountRecoveryOptions>>())
            : new UnavailableAccountEmailSender());
        return services;
    }
}
