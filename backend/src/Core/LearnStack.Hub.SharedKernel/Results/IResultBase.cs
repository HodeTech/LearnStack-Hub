using System.Diagnostics.CodeAnalysis;
using LearnStack.Hub.SharedKernel.Localization;

namespace LearnStack.Hub.SharedKernel.Results;

/// <summary>
/// Non-generic surface every <see cref="Result{T}"/> exposes. Pipeline
/// behaviors operate on this contract so they can construct the correct
/// concrete <c>Result&lt;TResponse&gt;</c> shape via
/// <see cref="Result.FailFor{TResponse}(Error)"/>.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Result+Error pattern — C#-only codebase; no VB consumer affected.")]
public interface IResultBase
{
    bool IsSuccess { get; }

    bool IsFailure { get; }

    LocalizedMessage? SuccessMessage { get; }

    Error? Error { get; }
}
