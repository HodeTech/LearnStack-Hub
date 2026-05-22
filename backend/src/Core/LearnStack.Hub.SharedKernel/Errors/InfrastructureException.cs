using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;

namespace LearnStack.Hub.SharedKernel.Errors;

/// <summary>
/// Transient infrastructure fault (database connection, cache, outbox
/// dispatcher transport). Retryable. Captured to <c>IErrorTrackingProvider</c>
/// at the L1 handler.
/// </summary>
public class InfrastructureException : HubException
{
    private static readonly Error DefaultError = new(
        new LocalizedMessage("lockey_dependency_unavailable"));

    public InfrastructureException(string message, Exception? innerException = null)
        : base(DefaultError, message, innerException)
    {
    }

    public InfrastructureException(Error error, string message, Exception? innerException = null)
        : base(error, message, innerException)
    {
    }
}
