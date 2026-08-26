using System.Text.RegularExpressions;

namespace Lca.Api.Infrastructure;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string candidate = context.Request.Headers[HeaderName].ToString();
        string correlationId = !string.IsNullOrWhiteSpace(candidate)
            && candidate.Length <= 128
            && SafeCorrelationId().IsMatch(candidate)
                ? candidate
                : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
        });

        await next(context);
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeCorrelationId();
}

