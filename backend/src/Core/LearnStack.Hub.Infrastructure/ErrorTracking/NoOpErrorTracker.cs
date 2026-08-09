using LearnStack.Hub.SharedKernel.Observability;

namespace LearnStack.Hub.Infrastructure.ErrorTracking;

/// <summary>
/// <see cref="IErrorTrackingProvider"/> that discards captures. Composition-root
/// default for <c>DeploymentMode.Development</c> — exceptions still surface via
/// Serilog + the OTel span; no external sink is involved.
/// </summary>
public sealed class NoOpErrorTracker : IErrorTrackingProvider
{
    public ValueTask CaptureAsync(
        Exception exception,
        CapturedContext context,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
