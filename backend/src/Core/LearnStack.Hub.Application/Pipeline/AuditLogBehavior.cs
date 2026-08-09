using System.Runtime.ExceptionServices;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LearnStack.Hub.Application.Pipeline;

/// <summary>
/// Step 3 of the 6-step Hub pipeline — <strong>shell</strong>. Wraps the inner
/// pipeline with try/catch and rethrows via <see cref="ExceptionDispatchInfo"/>
/// to preserve the original stack trace. The operator-audit write lands in
/// P02c-4 (the Audit module + Operators module); the shell preserves the
/// catch/rethrow contract + pipeline order so P02c-4 can light up the write
/// without churn.
/// </summary>
public sealed class AuditLogBehavior<TRequest, TResponse>(
    ILogger<AuditLogBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            var response = await next().ConfigureAwait(false);

            // TODO(P02c-4): on success, resolve the audit-state capture for the
            // request type and write the success-class operator-audit entry
            // through the Audit module's writer.

            return response;
        }
#pragma warning disable CA1031 // Audit-then-rethrow contract binds the broad catch here.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            // TODO(P02c-4): write the failure-class operator-audit entry. The
            // shell logs the audit intent so failure visibility is not silently
            // lost while the Audit module is pending.
            LogAuditIntent(logger, typeof(TRequest).Name, ex);

            ExceptionDispatchInfo.Capture(ex).Throw();
            throw; // unreachable; the line above is the rethrow.
        }
    }

    private static readonly Action<ILogger, string, Exception?> LogAuditIntent =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogAuditIntent)),
            "AuditLogBehavior shell captured exception during {RequestName}; operator-audit write deferred until P02c-4.");
}
