using LearnStack.Hub.Modules.Entitlements.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Subscriptions.Application.Abstractions;
using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.Subscriptions.Application.Handlers;

// Every successful transition that changes the bound plan, the status, or the
// current period triggers an entitlement recompute (the primary P02c-1
// cross-module flow). Recompute is a contract command into Entitlements; it
// rides the same outer transaction, so a recompute failure rolls the lot back.

public sealed class ActivateSubscriptionCommandHandler(
    ISubscriptionRepository repository,
    IMediator mediator,
    IClock clock)
    : IRequestHandler<ActivateSubscriptionCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subscription = await repository.GetByTenantAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_not_found")));
        }

        var result = subscription.Activate(request.PeriodStart, request.PeriodEnd, clock, HubSystemActors.SystemOperator);
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await RecomputeHelper.RecomputeAsync(mediator, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ChangePlanCommandHandler(
    ISubscriptionRepository repository,
    IMediator mediator,
    IClock clock)
    : IRequestHandler<ChangePlanCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ChangePlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subscription = await repository.GetByTenantAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_not_found")));
        }

        var plan = await mediator.Send(new GetPlanQuery(request.NewPlanId), cancellationToken).ConfigureAwait(false);
        if (plan.IsFailure)
        {
            return Result<Unit>.Fail(plan.Error!);
        }

        if (!plan.Value!.IsActive)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_business_rule_violation")));
        }

        // P02c-1 keeps the existing billing period (proration is Phase 09b).
        var result = subscription.ChangePlan(
            request.NewPlanId,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            clock,
            HubSystemActors.SystemOperator);
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await RecomputeHelper.RecomputeAsync(mediator, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class CancelSubscriptionCommandHandler(
    ISubscriptionRepository repository,
    IMediator mediator,
    IClock clock)
    : IRequestHandler<CancelSubscriptionCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subscription = await repository.GetByTenantAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_not_found")));
        }

        var result = subscription.Cancel(request.AtPeriodEnd, clock, HubSystemActors.SystemOperator);
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await RecomputeHelper.RecomputeAsync(mediator, request.TenantId, cancellationToken).ConfigureAwait(false);
    }
}

internal static class RecomputeHelper
{
    public static async Task<Result<Unit>> RecomputeAsync(IMediator mediator, Guid tenantId, CancellationToken cancellationToken)
    {
        var recompute = await mediator.Send(new RecomputeEntitlementCommand(tenantId), cancellationToken)
            .ConfigureAwait(false);
        return recompute.IsFailure ? Result<Unit>.Fail(recompute.Error!) : Result<Unit>.Ok(Unit.Value);
    }
}
