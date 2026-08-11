using AgropecuarIA.Identity;
using AgropecuarIA.Identity.Application;
using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AgropecuarIA.Api;

public sealed class IdentityExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<IdentityExceptionHandler> logger,
    IdentityTelemetry telemetry) : IExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> UnexpectedFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, "IdentityRequestFailed"),
            "Identity request failed with correlation {CorrelationId}");

    private static readonly Action<ILogger, string, string, Exception?> RejectedRequest =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2, "IdentityRequestRejected"),
            "Identity request was rejected with code {ErrorCode} and correlation {CorrelationId}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int statusCode, string title, string code, bool retryable, int? retryAfterSeconds) = exception switch
        {
            IdentityOperationException identityException =>
                (
                    identityException.StatusCode,
                    identityException.Title,
                    identityException.Code,
                    identityException.Retryable,
                    identityException.RetryAfterSeconds),
            AntiforgeryValidationException =>
                (StatusCodes.Status400BadRequest, "The antiforgery token is invalid.", "request.invalid_antiforgery", false, null),
            BadHttpRequestException =>
                (StatusCodes.Status400BadRequest, "The request body is invalid.", "request.invalid_body", false, null),
            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "server.unexpected", false, null),
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            UnexpectedFailure(logger, httpContext.TraceIdentifier, exception);
        }
        else
        {
            RejectedRequest(logger, code, httpContext.TraceIdentifier, null);
            telemetry.Record("request_rejected", code);
        }

        httpContext.Response.StatusCode = statusCode;
        if (retryAfterSeconds is not null)
        {
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(
                CultureInfo.InvariantCulture);
        }

        ProblemDetails problem = new()
        {
            Status = statusCode,
            Title = title,
            Type = $"https://agropecuaria.local/problems/{code}",
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;
        if (retryable)
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
