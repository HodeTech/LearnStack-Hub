using System.Diagnostics.CodeAnalysis;
using LearnStack.Hub.SharedKernel.Localization;

namespace LearnStack.Hub.SharedKernel.Results;

/// <summary>
/// Result-pattern wrapper. Returned by every MediatR command/query handler
/// (mirror of LearnStack core ADR-0032 § Error Model). Construction is
/// funnelled through the <see cref="Ok"/> / <see cref="Fail"/> factories;
/// the primary constructor is <c>internal</c> so callers cannot bypass the
/// success-must-carry-value rule via positional record syntax.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Result+Error factory pattern — canonical shape across FluentResults / Ardalis.Result lineage.")]
public sealed record Result<T> : IResultBase
{
    internal Result(bool isSuccess, T? value, Error? error, LocalizedMessage? successMessage = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        SuccessMessage = successMessage;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public Error? Error { get; }

    public LocalizedMessage? SuccessMessage { get; }

    /// <summary>
    /// Constructs a success result. Throws when <paramref name="value"/> is
    /// <c>null</c>: a success result must carry a value — if a payload-less
    /// success shape is needed, model it as <c>Result&lt;Unit&gt;</c>.
    /// </summary>
    public static Result<T> Ok(T value, LocalizedMessage? message = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(
                nameof(value),
                "Result<T>.Ok cannot wrap a null value. Use Result<Unit> for payload-less success.");
        }

        return new Result<T>(isSuccess: true, value: value, error: null, successMessage: message);
    }

    public static Result<T> Fail(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(isSuccess: false, value: default, error: error);
    }
}
