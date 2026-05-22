using LearnStack.Hub.SharedKernel.Observability;
using LearnStack.Hub.SharedKernel.Secrets;
using Microsoft.Extensions.Logging;

namespace LearnStack.Hub.Infrastructure.ErrorTracking;

/// <summary>
/// <see cref="IErrorTrackingProvider"/> for the online deployment modes
/// (SaaS / Dedicated / SelfHostedOnline). Composition-root default when a DSN
/// is configured.
/// </summary>
/// <remarks>
/// P02c-1 ships the <strong>shell</strong>: the Sentry SDK is not yet a Hub
/// dependency (it lands in a later packet inside this Infrastructure assembly so
/// modules never reference <c>Sentry.SentrySdk</c> directly). The shell reads
/// the DSN through <see cref="ISecretProvider"/> and logs the capture intent so
/// the composition-root branch + the secret seam are exercised now; the real
/// <c>SentrySdk.CaptureException</c> call replaces the log line when the SDK is
/// wired.
/// </remarks>
public sealed class SentryErrorTracker : IErrorTrackingProvider
{
    private readonly ILogger<SentryErrorTracker> _logger;
    private readonly bool _dsnConfigured;

    public SentryErrorTracker(ISecretProvider secrets, ILogger<SentryErrorTracker> logger)
    {
        ArgumentNullException.ThrowIfNull(secrets);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dsnConfigured = !string.IsNullOrWhiteSpace(secrets.GetSecret("ErrorTracking:Sentry:Dsn"));
    }

    public ValueTask CaptureAsync(
        Exception exception,
        CapturedContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(context);

        // TODO(later packet): SentrySdk.CaptureException(exception, scope => { ... tags from context ... });
        LogCaptureIntent(_logger, exception.GetType().FullName ?? "<unknown>", _dsnConfigured, exception);
        return ValueTask.CompletedTask;
    }

    private static readonly Action<ILogger, string, bool, Exception?> LogCaptureIntent =
        LoggerMessage.Define<string, bool>(
            LogLevel.Error,
            new EventId(1, nameof(LogCaptureIntent)),
            "SentryErrorTracker shell would capture {ExceptionType} (dsnConfigured={DsnConfigured}); real SDK wiring deferred.");
}
