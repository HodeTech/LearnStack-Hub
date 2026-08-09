using System.Diagnostics;
using LearnStack.Hub.SharedKernel.Errors;
using LearnStack.Hub.SharedKernel.Observability;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LearnStack.Hub.Api.Common;

/// <summary>
/// L1 exception handler — the single catch site at the HTTP boundary (mirror of
/// LearnStack core's <c>LearnStackExceptionHandler</c>). Builds the Problem
/// Details body, records the span error, and dispatches to
/// <see cref="IErrorTrackingProvider"/> only when
/// <see cref="ShouldCapture(Exception)"/> returns <c>true</c>.
/// </summary>
/// <remarks>
/// <c>internal sealed</c> — modules do not instantiate it; the framework's
/// <c>AddExceptionHandler&lt;T&gt;()</c> is the only entry. The captured
/// context is operator-scoped (no tenant context); the operator id itself
/// lands with the Operators module in P02c-4, so it is null here.
/// </remarks>
internal sealed class HubExceptionHandler(
    IErrorTrackingProvider errorTracker,
    ILogger<HubExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var problem = ProblemDetailsFactory.For(exception, httpContext);
        var capture = ShouldCapture(exception);
        var isProviderClientError = exception is ProviderException { IsClientError: true };
        var isCancellation = exception is OperationCanceledException;

        if (!isCancellation)
        {
            if (!isProviderClientError)
            {
                Activity.Current?.AddException(exception);
            }

            Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
        }

        if (capture)
        {
            // The error tracker must never abort the L1 handler — swallow + log
            // any capture failure; the Problem Details response below always runs.
            try
            {
                var capturedContext = BuildCapturedContext(httpContext);
                await errorTracker.CaptureAsync(exception, capturedContext, cancellationToken)
                    .ConfigureAwait(false);
                LogCaptured(logger, exception.GetType().FullName ?? "<unknown>", exception);
            }
#pragma warning disable CA1031 // L1 must complete; provider failure cannot escape.
            catch (Exception captureFailure)
#pragma warning restore CA1031
            {
                LogCaptureFailed(logger, exception.GetType().FullName ?? "<unknown>", captureFailure);
            }
        }
        else
        {
            LogSkipped(logger, exception.GetType().FullName ?? "<unknown>", null);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        if (isCancellation || cancellationToken.IsCancellationRequested)
        {
            return true;
        }

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
                problem,
                problem.GetType(),
                options: null,
                contentType: "application/problem+json",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// The error-capture boundary: skip cancellation (client disconnect) and
    /// provider 4xx (caller's mistake); capture everything else.
    /// </summary>
    internal static bool ShouldCapture(Exception exception) => exception switch
    {
        OperationCanceledException => false,
        ProviderException pex when pex.IsClientError => false,
        _ => true,
    };

    private static CapturedContext BuildCapturedContext(HttpContext httpContext)
    {
        var correlationId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return new CapturedContext(
            CorrelationId: correlationId,
            RequestPath: httpContext.Request.Path.Value,
            RequestMethod: httpContext.Request.Method,
            OperatorId: null, // resolved OperatorContext lands in P02c-4.
            TenantId: null,
            ModuleName: null,
            AdditionalTags: null);
    }

    private static readonly Action<ILogger, string, Exception?> LogCaptured =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(LogCaptured)),
            "L1 exception handler captured {ExceptionType} to IErrorTrackingProvider.");

    private static readonly Action<ILogger, string, Exception?> LogSkipped =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, nameof(LogSkipped)),
            "L1 exception handler skipped capture for {ExceptionType} per the error-capture boundary.");

    private static readonly Action<ILogger, string, Exception?> LogCaptureFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(LogCaptureFailed)),
            "L1 exception handler failed to capture {ExceptionType}; Problem Details response still written.");
}
