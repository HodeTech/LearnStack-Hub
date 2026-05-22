using LearnStack.Hub.Modules.Entitlements.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Subscriptions.Application.Abstractions;
using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;
using LearnStack.Hub.Modules.Subscriptions.Domain;
using LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.Subscriptions.Application.Handlers;

/// <summary>
/// Creates the trial subscription and triggers the first entitlement recompute
/// (generation 1). Cross-module reads go through Application.Contracts queries
/// (Plans / TenantLifecycle); the recompute is a contract command into the
/// Entitlements module — all within the one outer transaction.
/// </summary>
public sealed class StartTrialCommandHandler(
    ISubscriptionRepository repository,
    IMediator mediator,
    IClock clock,
    IGuidFactory guids)
    : IRequestHandler<StartTrialCommand, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(StartTrialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = LearnStackTenantId.From(request.TenantId);

        var tenant = await mediator.Send(new GetTenantQuery(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return Result<SubscriptionDto>.Fail(tenant.Error!);
        }

        if (await repository.ExistsForTenantAsync(tenantId, cancellationToken).ConfigureAwait(false))
        {
            return Fail("lockey_business_rule_violation");
        }

        var plan = await mediator.Send(new GetPlanQuery(request.PlanId), cancellationToken).ConfigureAwait(false);
        if (plan.IsFailure)
        {
            return Result<SubscriptionDto>.Fail(plan.Error!);
        }

        if (!plan.Value!.IsActive)
        {
            return Fail("lockey_business_rule_violation");
        }

        var trialStart = clock.UtcNow;
        var trialEnd = trialStart.AddDays(request.TrialDays);

        var subscription = HubSubscription.StartTrial(
            HubSubscriptionId.From(guids.NewUuidV7()),
            tenantId,
            request.PlanId,
            trialStart,
            trialEnd,
            clock,
            HubSystemActors.SystemOperator);

        await repository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var recompute = await mediator.Send(new RecomputeEntitlementCommand(request.TenantId), cancellationToken)
            .ConfigureAwait(false);
        if (recompute.IsFailure)
        {
            return Result<SubscriptionDto>.Fail(recompute.Error!);
        }

        return Result<SubscriptionDto>.Ok(subscription.ToDto());
    }

    private static Result<SubscriptionDto> Fail(string lockey) =>
        Result<SubscriptionDto>.Fail(new Error(new LocalizedMessage(lockey)));
}
