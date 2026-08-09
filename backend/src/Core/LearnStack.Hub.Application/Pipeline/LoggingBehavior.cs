using System.Diagnostics;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LearnStack.Hub.Application.Pipeline;

/// <summary>
/// Step 2 of the 6-step Hub pipeline. Opens an <see cref="ILogger.BeginScope"/>
/// carrying the request name + correlation id, starts a manual
/// <see cref="Activity"/> on the <c>learnstack.hub.mediatr</c>
/// <see cref="ActivitySource"/>, and measures handler latency.
/// </summary>
/// <remarks>
/// Hub is operator-scoped, not tenant-scoped, so the correlation scope carries
/// <c>operator.id</c> rather than <c>tenant.id</c>. The operator-context
/// accessor lands with the Operators module (P02c-4); until then the scope
/// carries the request name + the W3C correlation id from the ambient activity.
/// </remarks>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase
{
    private static readonly ActivitySource ActivitySource = new("learnstack.hub.mediatr");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;

        using var activity = ActivitySource.StartActivity(
            $"mediatr.{requestName}",
            ActivityKind.Internal);

        using var scope = logger.BeginScope(BuildScope(requestName));

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            if (response.IsSuccess)
            {
                LogSuccess(logger, requestName, stopwatch.ElapsedMilliseconds, null);
            }
            else
            {
                LogFailure(logger, requestName, response.Error?.Code ?? "<unknown>", stopwatch.ElapsedMilliseconds, null);
            }

            return response;
        }
        catch
        {
            stopwatch.Stop();
            // Re-throw so the AuditLogBehavior (step 3) owns the failure-audit
            // path and the L1 HubExceptionHandler logs the exception once.
            throw;
        }
    }

    private static Dictionary<string, object?> BuildScope(string requestName)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["RequestName"] = requestName,
            ["CorrelationId"] = Activity.Current?.Id,
        };
    }

    private static readonly Action<ILogger, string, long, Exception?> LogSuccess =
        LoggerMessage.Define<string, long>(
            LogLevel.Information,
            new EventId(1, nameof(LogSuccess)),
            "MediatR request {RequestName} completed successfully in {ElapsedMilliseconds} ms");

    private static readonly Action<ILogger, string, string, long, Exception?> LogFailure =
        LoggerMessage.Define<string, string, long>(
            LogLevel.Information,
            new EventId(2, nameof(LogFailure)),
            "MediatR request {RequestName} returned Result.Fail({ErrorCode}) in {ElapsedMilliseconds} ms");
}
