using LearnStack.Hub.SharedKernel.Results;

namespace LearnStack.Hub.SharedKernel.Errors;

/// <summary>
/// Base class for every exception Hub itself raises (the Hub analogue of
/// LearnStack core's <c>LearnStackException</c>). Exceptions are reserved for
/// <em>unexpected</em> failures (bugs, transient infrastructure faults,
/// contract violations); expected outcomes return <see cref="Result{T}"/>.
/// Carrying the structured <see cref="Results.Error"/> at the exception site
/// lets the L1 <c>HubExceptionHandler</c> map straight to RFC 7807 Problem
/// Details.
/// </summary>
public abstract class HubException : Exception
{
    protected HubException(Error error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    /// <summary>The stable <see cref="Results.Error"/> the L1 handler projects to the Problem Details body.</summary>
    public Error Error { get; }
}
