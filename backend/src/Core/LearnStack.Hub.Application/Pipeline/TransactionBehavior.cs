using LearnStack.Hub.SharedKernel.Persistence;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Application.Pipeline;

/// <summary>
/// Step 5 of the 6-step Hub pipeline — <strong>live</strong>. Opens the
/// shared-connection unit-of-work transaction for the outermost command,
/// commits on a success-<see cref="IResultBase"/>, and rolls back on a
/// fail-result or any exception. Validation- and authorization-failed requests
/// short-circuit upstream and never reach here.
/// </summary>
/// <remarks>
/// Unlike LearnStack core's Packet-3 shell (which waited for per-module
/// DbContexts), Hub has real DbContexts in P02c-1 so this behavior is live.
/// Nested MediatR sends (e.g. the Subscriptions handler invoking the
/// Entitlements recompute) join the outer transaction: when a transaction is
/// already active the behavior delegates straight to the inner pipeline so only
/// the outermost command owns commit / rollback. Every module DbContext rides
/// the one transaction via <see cref="IUnitOfWork"/>.
/// </remarks>
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResultBase
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // Nested send: a transaction is already open one frame out. Join it —
        // the outermost behavior owns commit / rollback.
        if (unitOfWork.HasActiveTransaction)
        {
            return await next().ConfigureAwait(false);
        }

        await unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var response = await next().ConfigureAwait(false);

            if (response.IsSuccess)
            {
                await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await unitOfWork.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
