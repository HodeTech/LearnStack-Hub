using System.Reflection;
using LearnStack.Hub.SharedKernel.Localization;

namespace LearnStack.Hub.SharedKernel.Results;

/// <summary>
/// Static helpers for constructing <see cref="Result{T}"/> instances when the
/// concrete generic parameter is known only at runtime (e.g. inside the
/// MediatR <c>ValidationBehavior</c> that short-circuits a handler without
/// referencing the value type). Mirror of LearnStack core ADR-0032 § Sub-decision 3.
/// </summary>
public static class Result
{
    public static Result<T> Ok<T>(T value, LocalizedMessage? message = null) =>
        Result<T>.Ok(value, message);

    public static Result<T> Fail<T>(Error error) => Result<T>.Fail(error);

    /// <summary>
    /// Reflection-friendly failure factory used by MediatR pipeline behaviors
    /// whose <c>TResponse</c> is itself a <c>Result&lt;TValue&gt;</c>. Returns
    /// an instance of <typeparamref name="TResponse"/> by reflecting over the
    /// closed generic to invoke <c>Result&lt;TValue&gt;.Fail(error)</c>. The
    /// reflected <see cref="MethodInfo"/> is cached per closed
    /// <typeparamref name="TResponse"/> — initialised once, zero per-call cost.
    /// </summary>
    public static TResponse FailFor<TResponse>(Error error)
        where TResponse : IResultBase
    {
        ArgumentNullException.ThrowIfNull(error);

        var fail = FailForCache<TResponse>.FailMethod
            ?? throw new InvalidOperationException(
                $"Result.FailFor<TResponse> requires TResponse to be a closed Result<T>; got {typeof(TResponse).FullName}.");

        return (TResponse)fail.Invoke(obj: null, parameters: [error])!;
    }

    private static class FailForCache<TResponse>
        where TResponse : IResultBase
    {
        public static readonly MethodInfo? FailMethod = ResolveFailMethod();

        private static MethodInfo? ResolveFailMethod()
        {
            var responseType = typeof(TResponse);
            if (!responseType.IsGenericType
                || responseType.GetGenericTypeDefinition() != typeof(Result<>))
            {
                return null;
            }

            return responseType.GetMethod(
                nameof(Result<int>.Fail),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(Error)],
                modifiers: null);
        }
    }
}
