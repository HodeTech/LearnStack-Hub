using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Application.Pipeline;

/// <summary>
/// Step 6 of the 6-step Hub pipeline — <strong>shell</strong>. Enrols
/// <c>IOutbox</c> messages in the current unit-of-work transaction; the outbox
/// processor publishes them via Dapr pub/sub on commit. The <c>IOutbox</c>
/// contract + the <c>learnstack.hub.entitlement</c> publish land in P02c-2; the
/// shell delegates to the inner pipeline so the order is correct now.
/// </summary>
public sealed class OutboxFlushBehavior<TRequest, TResponse>
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

        // TODO(P02c-2): on a success-Result, flush IOutbox messages collected
        // during the handler into outbox_messages within the active unit-of-work
        // transaction so Dapr pub/sub dispatches learnstack.hub.entitlement
        // after commit.

        return next();
    }
}
