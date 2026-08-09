using FluentValidation;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Application.Pipeline;

/// <summary>
/// Step 1 of the 6-step Hub pipeline. Aggregates FluentValidation failures into
/// <see cref="Error.Details"/> and returns
/// <c>Result.FailFor&lt;TResponse&gt;(validation_failed, …)</c>; <strong>never
/// throws</strong> <see cref="ValidationException"/>.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase
{
    private const string ValidationFailedKey = "lockey_validation_failed";

    private readonly IValidator<TRequest>[] _validators = validators.ToArray();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Length == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var details = failures
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<LocalizedMessage>)g
                    .Select(f => new LocalizedMessage(NormaliseKey(f.ErrorCode ?? f.ErrorMessage)))
                    .ToArray(),
                StringComparer.Ordinal);

        var error = new Error(
            new LocalizedMessage(ValidationFailedKey),
            details);

        return Result.FailFor<TResponse>(error);
    }

    /// <summary>
    /// FluentValidation error codes are typically rule names ("NotEmpty"); the
    /// API contract requires the <see cref="LocalizedMessage.RequiredPrefix"/>.
    /// If the validator's <c>ErrorCode</c> already starts with <c>lockey_</c>
    /// we trust it; otherwise we coerce by lower-casing and prefixing.
    /// </summary>
    private static string NormaliseKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "lockey_validation_failed";
        }

        return raw.StartsWith(LocalizedMessage.RequiredPrefix, StringComparison.Ordinal)
            ? raw
            : LocalizedMessage.RequiredPrefix + raw.ToLowerInvariant();
    }
}
