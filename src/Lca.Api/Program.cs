using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

using Lca.Api.Configuration;
using Lca.Api.Contracts;
using Lca.Api.Infrastructure;
using Lca.Api.Security;
using Lca.Core.Security;
using Lca.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<IAuthorizationHandler, TenantRequiredHandler>();
builder.Services.AddLcaInfrastructure(builder.Configuration);

JwtOptions jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options => builder.Environment.IsDevelopment() || options.IsConfigured,
        "JWT issuer, audience, and a signing key of at least 32 characters are required outside Development.")
    .ValidateOnStart();

if (jwtOptions.IsConfigured)
{
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
        });
}
else
{
    builder.Services.AddAuthentication(UnavailableAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, UnavailableAuthenticationHandler>(
            UnavailableAuthenticationHandler.SchemeName,
            static _ => { });
}

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.TenantRequired, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new TenantRequiredRequirement());
    })
    .AddPolicy(Policies.CatalogRead, policy => AddTenantPermission(policy, Permissions.CatalogRead))
    .AddPolicy(Policies.ProductDraftCreate, policy => AddTenantPermission(policy, Permissions.ProductDraftCreate))
    .AddPolicy(Policies.ApprovalQueueRead, policy => AddTenantPermission(policy, Permissions.ApprovalQueueRead))
    .AddPolicy(Policies.ApprovalQueueApprove, policy => AddTenantPermission(policy, Permissions.ApprovalQueueApprove));

static void AddTenantPermission(AuthorizationPolicyBuilder policy, string permission)
{
    policy.RequireAuthenticatedUser();
    policy.RequireClaim("sub");
    policy.AddRequirements(new TenantRequiredRequirement());
    policy.RequireClaim(TrustedClaimTypes.Permission, permission);
}

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusContext =>
{
    await Results.Problem(statusCode: statusContext.HttpContext.Response.StatusCode)
        .ExecuteAsync(statusContext.HttpContext);
});
app.UseCors();
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
        .RequireAuthorization(Policies.TenantRequired);
}

app.Run();

public partial class Program;
