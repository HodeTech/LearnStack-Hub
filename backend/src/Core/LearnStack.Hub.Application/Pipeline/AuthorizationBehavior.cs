using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Application.Pipeline;

/// <summary>
/// Step 4 of the 6-step Hub pipeline — <strong>shell</strong>. The operator
/// permission check (<c>{module}.{resource}.{action}</c> keys, operator scope
/// only) lands in P02c-4 with the Operators module. The shell passes every
/// request through and preserves the pipeline-order contract.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // TODO(P02c-4): resolve the request's required operator permission,
        // check it against the resolved OperatorContext, and return
        // Result.FailFor<TResponse>(forbidden) on deny.

        return next();
    }
}
