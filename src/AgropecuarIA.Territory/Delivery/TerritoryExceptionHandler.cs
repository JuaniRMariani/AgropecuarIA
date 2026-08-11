using AgropecuarIA.Territory.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AgropecuarIA.Territory.Delivery;

public sealed class TerritoryExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<TerritoryExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, string, string, Exception?> RejectedRequest =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, "TerritoryRequestRejected"),
            "Territory request was rejected with code {ErrorCode} and correlation {CorrelationId}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not TerritoryOperationException territoryException)
        {
            return false;
        }

        RejectedRequest(
            logger,
            territoryException.Code,
            httpContext.TraceIdentifier,
            null);
        httpContext.Response.StatusCode = territoryException.StatusCode;
        ProblemDetails problem = new()
        {
            Status = territoryException.StatusCode,
            Title = territoryException.Title,
            Type = $"https://agropecuaria.local/problems/{territoryException.Code}",
        };
        problem.Extensions["code"] = territoryException.Code;
        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;
        if (territoryException.Retryable)
        {
            problem.Extensions["retryable"] = true;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }
}
