using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;

namespace LearnStack.Hub.SharedKernel.Errors;

/// <summary>
/// Programmer-error exception. Raised <em>only</em> when an aggregate's
/// invariant is bypassed by a programming mistake — never for expected
/// business-rule violations (those return
/// <c>Result.Fail(business_rule_violation, …)</c>, mirror of LearnStack core
/// ADR-0032 § Sub-decision 4).
/// </summary>
public sealed class DomainException : HubException
{
    // A DomainException reaching the L1 handler is a *bug* — an invariant was
    // bypassed, not an expected outcome. Its default code maps to a 500
    // (internal error), NOT business_rule_violation (409) which is reserved
    // for the Result.Fail path.
    private static readonly Error DefaultError = new(
        new LocalizedMessage("lockey_internal_error"));

    public DomainException(string message, Exception? innerException = null)
        : base(DefaultError, message, innerException)
    {
    }

    public DomainException(Error error, string message, Exception? innerException = null)
        : base(error, message, innerException)
    {
    }
}
