using System.Reflection;

using Lca.Api.Configuration;
using Lca.Api.Contracts;
using Lca.Api.Infrastructure;
using Lca.Api.Security;
using Lca.Core.Security;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddSingleton<IAuthorizationHandler, TenantRequiredHandler>();
builder.Services
    .AddAuthentication(UnavailableAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, UnavailableAuthenticationHandler>(
        UnavailableAuthenticationHandler.SchemeName,
        static _ => { });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.TenantRequired, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new TenantRequiredRequirement());
    });

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
