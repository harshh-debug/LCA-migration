using Lca.Infrastructure.Catalog;
using Lca.Infrastructure.Governance;
using Lca.Infrastructure.Persistence;

using Microsoft.AspNetCore.Diagnostics;

namespace Lca.Api.Infrastructure;

public sealed partial class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode = exception switch
        {
            InvalidProductDraftException => StatusCodes.Status400BadRequest,
            ApprovalConflictException => StatusCodes.Status409Conflict,
            TenantDatabaseNotConfiguredException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError,
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception);
            return false;
        }

        LogRejectedRequest(logger, exception, statusCode);
        await Results.Problem(
                statusCode: statusCode,
                title: statusCode switch
                {
                    StatusCodes.Status400BadRequest => "Invalid product draft",
                    StatusCodes.Status409Conflict => "Approval conflict",
                    _ => "Tenant database unavailable",
                },
                detail: exception.Message)
            .ExecuteAsync(httpContext);
        return true;
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Unhandled API exception")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "API request rejected with status code {StatusCode}")]
    private static partial void LogRejectedRequest(ILogger logger, Exception exception, int statusCode);
}
