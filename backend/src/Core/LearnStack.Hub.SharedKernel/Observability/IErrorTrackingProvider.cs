namespace LearnStack.Hub.SharedKernel.Observability;

/// <summary>
/// Sanctioned entry point for error capture. The L1 <c>HubExceptionHandler</c>
/// is the only production caller; modules never import <c>Sentry.SentrySdk</c>
/// directly. The composition root selects the implementation by
/// <c>DeploymentMode</c>: <c>NoOpErrorTracker</c> for Development,
/// <c>SentryErrorTracker</c> for SaaS / Dedicated / SelfHostedOnline,
/// <c>LocalFileErrorTracker</c> for SelfHostedAirGapped.
/// </summary>
public interface IErrorTrackingProvider
{
    ValueTask CaptureAsync(
        Exception exception,
        CapturedContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of cross-cutting tags every error capture flows with. Hub is
/// operator-scoped, not tenant-scoped: the actor is an <c>OperatorId</c>, and
/// there is no tenant <c>UserId</c> / organization context (the operator-id
/// span enricher itself lands with the Operators module in P02c-4). When a
/// capture concerns a specific tenant the optional <see cref="TenantId"/>
/// carries it (Hub legitimately administers tenants by id — that is not RLS).
/// </summary>
public sealed record CapturedContext(
    string? CorrelationId,
    string? RequestPath,
    string? RequestMethod,
    Guid? OperatorId,
    Guid? TenantId,
    string? ModuleName,
    IReadOnlyDictionary<string, string>? AdditionalTags = null);
