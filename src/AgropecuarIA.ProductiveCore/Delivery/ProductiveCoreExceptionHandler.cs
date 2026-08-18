using AgropecuarIA.ProductiveCore.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AgropecuarIA.ProductiveCore.Delivery;

public sealed class ProductiveCoreExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProductiveCoreExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, string, string, Exception?> RejectedRequest =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, "ProductiveCoreRequestRejected"),
            "Productive Core request was rejected with code {ErrorCode} and correlation {CorrelationId}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ProductiveCoreOperationException operationException)
        {
            return false;
        }

        RejectedRequest(logger, operationException.Code, httpContext.TraceIdentifier, null);
        httpContext.Response.StatusCode = operationException.StatusCode;
        if (operationException.RetryAfterSeconds is not null)
        {
            httpContext.Response.Headers.RetryAfter =
                operationException.RetryAfterSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        ProblemDetails problem = new()
        {
            Status = operationException.StatusCode,
            Title = operationException.Title,
            Type = $"https://agropecuaria.local/problems/{operationException.Code}",
        };
        problem.Extensions["code"] = operationException.Code;
        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;
        if (operationException.Retryable)
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
