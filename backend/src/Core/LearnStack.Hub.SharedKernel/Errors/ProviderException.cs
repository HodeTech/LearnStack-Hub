using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;

namespace LearnStack.Hub.SharedKernel.Errors;

/// <summary>
/// Wraps an upstream provider failure surfaced at an adapter boundary
/// (Stripe / Iyzico / Let's Encrypt — adapters land in later packets). Each
/// adapter translates SDK exception types into a <see cref="ProviderException"/>
/// so SDK types never leave the adapter assembly.
/// </summary>
/// <remarks>
/// <see cref="IsClientError"/> splits the error-capture boundary: <c>true</c>
/// for 4xx upstream (no capture), <c>false</c> for 5xx / timeouts (captured).
/// It does not drive the HTTP status returned to the client — that comes from
/// the carried <see cref="Error"/>'s code.
/// </remarks>
public class ProviderException : HubException
{
    private static readonly Error DefaultError = new(
        new LocalizedMessage("lockey_dependency_unavailable"));

    public ProviderException(
        string providerName,
        string message,
        bool isClientError,
        Exception? innerException = null)
        : base(DefaultError, message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ProviderName = providerName;
        IsClientError = isClientError;
    }

    public ProviderException(
        Error error,
        string providerName,
        string message,
        bool isClientError,
        Exception? innerException = null)
        : base(error, message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ProviderName = providerName;
        IsClientError = isClientError;
    }

    /// <summary>Stable provider identifier (e.g. <c>"stripe"</c>). Must not leak to end users.</summary>
    public string ProviderName { get; }

    /// <summary><c>true</c> when the upstream response is a 4xx-equivalent; the L1 handler skips capture for client errors.</summary>
    public bool IsClientError { get; }
}
