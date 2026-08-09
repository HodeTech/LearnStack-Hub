using System.Text.Json;
using LearnStack.Hub.SharedKernel.Observability;
using LearnStack.Hub.SharedKernel.Secrets;
using Microsoft.Extensions.Logging;

namespace LearnStack.Hub.Infrastructure.ErrorTracking;

/// <summary>
/// <see cref="IErrorTrackingProvider"/> for <c>DeploymentMode.SelfHostedAirGapped</c>:
/// writes a redacted JSON capture line to a local newline-delimited file (no
/// network egress). Tag values whose key matches <see cref="SensitiveTokenCatalog"/>
/// are redacted before serialisation so credentials never reach the file.
/// </summary>
public sealed class LocalFileErrorTracker : IErrorTrackingProvider, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly string _filePath;
    private readonly ILogger<LocalFileErrorTracker> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public LocalFileErrorTracker(string filePath, ILogger<LocalFileErrorTracker> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask CaptureAsync(
        Exception exception,
        CapturedContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(context);

        var record = new
        {
            timestamp = DateTimeOffset.UtcNow,
            exceptionType = exception.GetType().FullName,
            message = exception.Message,
            stackTrace = exception.StackTrace,
            correlationId = context.CorrelationId,
            requestPath = context.RequestPath,
            requestMethod = context.RequestMethod,
            operatorId = context.OperatorId,
            tenantId = context.TenantId,
            moduleName = context.ModuleName,
            tags = Redact(context.AdditionalTags),
        };

        var line = JsonSerializer.Serialize(record, SerializerOptions);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.AppendAllTextAsync(_filePath, line + Environment.NewLine, cancellationToken)
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031 // The tracker is the last line of defense; an I/O failure must not escape.
        catch (Exception ioFailure)
#pragma warning restore CA1031
        {
            LogWriteFailed(_logger, _filePath, ioFailure);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static readonly Action<ILogger, string, Exception?> LogWriteFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(LogWriteFailed)),
            "LocalFileErrorTracker failed to append a capture to {FilePath}.");

    private static Dictionary<string, string>? Redact(IReadOnlyDictionary<string, string>? tags)
    {
        if (tags is not { Count: > 0 })
        {
            return null;
        }

        var redacted = new Dictionary<string, string>(tags.Count, StringComparer.Ordinal);
        foreach (var (key, value) in tags)
        {
            redacted[key] = SensitiveTokenCatalog.IsSensitive(key)
                ? SensitiveTokenCatalog.RedactedValue
                : value;
        }

        return redacted;
    }

    public void Dispose() => _writeLock.Dispose();
}
