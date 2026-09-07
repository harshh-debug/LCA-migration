using System.Reflection;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using Lca.Api.Configuration;
using Lca.Api.Contracts;
using Lca.Api.Infrastructure;
using Lca.Api.Security;
using Lca.Core.Security;
using Lca.Core.Tenancy;
using Lca.Infrastructure;
using Lca.Infrastructure.Configuration;
using Lca.Infrastructure.Identity;
using Lca.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

if (args is ["--health-check"])
{
    using HttpClient healthClient = new();
    try
    {
        using HttpResponseMessage response = await healthClient.GetAsync("http://127.0.0.1:8080/health/live");
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        Environment.ExitCode = 1;
    }

    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services
    .AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .Validate(
        options => !builder.Environment.IsStaging() || options.AllowedOrigins.Length > 0,
        "At least one explicit CORS origin is required in Staging.")
    .ValidateOnStart();

CorsOptions corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (corsOptions.AllowedOrigins.Length > 0)
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
}));
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();
    options.AddOperationTransformer<BearerSecurityOperationTransformer>();
});
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sqlserver", tags: ["ready"]);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<HttpTenantContext>();
builder.Services.AddScoped<ITenantContext>(services => services.GetRequiredService<HttpTenantContext>());
builder.Services.AddScoped<IAuthorizationHandler, TenantRequiredHandler>();
builder.Services.AddLcaInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddScoped<AccountScopeValidator>();
builder.Services.AddScoped<JwtTokenIssuer>();
builder.Services.AddScoped<ApplicationAuthenticationService>();
builder.Services.AddScoped<InitialAccountSeeder>();
builder.Services.AddOptions<InitialAccountsOptions>()
    .Bind(builder.Configuration.GetSection(InitialAccountsOptions.SectionName));

AccountRecoveryOptions recoveryOptions = builder.Configuration
    .GetSection(AccountRecoveryOptions.SectionName)
    .Get<AccountRecoveryOptions>() ?? new AccountRecoveryOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("account-recovery-forgot", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = recoveryOptions.ForgotRequestsPerIp,
            Window = TimeSpan.FromMinutes(recoveryOptions.ForgotWindowMinutes),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.AddPolicy("account-recovery-token", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = recoveryOptions.TokenAttemptsPerIp,
            Window = TimeSpan.FromMinutes(recoveryOptions.TokenAttemptWindowMinutes),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
});

JwtOptions jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => options.IsConfigured,
        "JWT issuer, audience, a signing key of at least 32 characters, and a valid token lifetime are required.")
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ValidateAccountScopeAsync,
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.TenantAccess, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("sub");
        policy.RequireClaim(TrustedClaimTypes.AccountType, "tenant");
        policy.AddRequirements(new TenantRequiredRequirement());
    })
    .AddPolicy(Policies.PlatformAdmin, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("sub");
        policy.RequireClaim(TrustedClaimTypes.AccountType, "platform");
        policy.RequireRole(PlatformRoles.PlatformAdmin);
    });

static async Task ValidateAccountScopeAsync(TokenValidatedContext tokenContext)
{
    ClaimsPrincipal principal = tokenContext.Principal!;
    string? userId = principal.FindFirstValue("sub");
    string? accountType = principal.FindFirstValue(TrustedClaimTypes.AccountType);
    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accountType))
    {
        tokenContext.Fail("The token account scope is invalid.");
        return;
    }

    UserManager<ApplicationUser> userManager = tokenContext.HttpContext.RequestServices
        .GetRequiredService<UserManager<ApplicationUser>>();
    AccountScopeValidator validator = tokenContext.HttpContext.RequestServices
        .GetRequiredService<AccountScopeValidator>();
    ApplicationUser? user = await userManager.FindByIdAsync(userId);
    string? tokenSecurityStamp = principal.FindFirstValue(TrustedClaimTypes.SecurityStamp);
    if (user is null
        || string.IsNullOrWhiteSpace(tokenSecurityStamp)
        || !string.Equals(tokenSecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
    {
        tokenContext.Fail("The token user is invalid.");
        return;
    }

    string? tenantClaim = principal.FindFirstValue(TrustedClaimTypes.TenantId);
    if (string.Equals(accountType, "platform", StringComparison.Ordinal))
    {
        if (tenantClaim is not null
            || !await validator.IsValidPlatformAccountAsync(user, tokenContext.HttpContext.RequestAborted))
        {
            tokenContext.Fail("The platform account scope is invalid.");
        }

        return;
    }

    if (!string.Equals(accountType, "tenant", StringComparison.Ordinal)
        || !long.TryParse(tenantClaim, NumberStyles.None, CultureInfo.InvariantCulture, out long claimedTenantId))
    {
        tokenContext.Fail("The tenant account scope is invalid.");
        return;
    }

    long? resolvedTenantId = await validator.ResolveTenantAsync(user, tokenContext.HttpContext.RequestAborted);
    if (resolvedTenantId != claimedTenantId)
    {
        tokenContext.Fail("The tenant membership is invalid.");
        return;
    }

    tokenContext.HttpContext.RequestServices
        .GetRequiredService<HttpTenantContext>()
        .Initialize(new TenantId(claimedTenantId));
}

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<InitialAccountSeeder>().SeedAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusContext =>
{
    await Results.Problem(statusCode: statusContext.HttpContext.Response.StatusCode)
        .ExecuteAsync(statusContext.HttpContext);
});
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = static _ => false,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = static registration => registration.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
});

RouteGroupBuilder system = app.MapGroup("/api/v1/system").WithTags("System");
system.MapGet("/status", (HttpContext context, TimeProvider timeProvider) =>
{
    AssemblyName assembly = typeof(Program).Assembly.GetName();
    return TypedResults.Ok(new SystemStatusResponse(
        Service: assembly.Name ?? "Lca.Api",
        Status: "ok",
        Version: assembly.Version?.ToString() ?? "unknown",
        TimestampUtc: timeProvider.GetUtcNow(),
        CorrelationId: context.TraceIdentifier));
})
.WithName("GetSystemStatus")
.Produces<SystemStatusResponse>();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/api/v1/test/tenant", (ITenantContext tenantContext) =>
            TypedResults.Ok(new { tenantId = tenantContext.TenantId?.Value }))
        .RequireAuthorization(Policies.TenantAccess);
}

app.Run();

public partial class Program;
