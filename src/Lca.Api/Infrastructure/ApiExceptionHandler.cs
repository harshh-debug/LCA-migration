using Lca.Infrastructure.Persistence;
using Lca.Core.Catalog;
using Lca.Core.Customers;
using Lca.Core.Platform;

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
            TenantIsolationException => StatusCodes.Status403Forbidden,
            CatalogConflictException => StatusCodes.Status409Conflict,
            CustomerConflictException => StatusCodes.Status409Conflict,
            PlatformAdministrationConflictException => StatusCodes.Status409Conflict,
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
                    StatusCodes.Status409Conflict => "Operation conflicts with existing data",
                    _ => "Tenant access denied",
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
