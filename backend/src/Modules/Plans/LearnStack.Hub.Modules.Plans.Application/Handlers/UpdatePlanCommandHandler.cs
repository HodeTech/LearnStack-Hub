using LearnStack.Hub.Modules.Entitlements.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Application.Abstractions;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.Plans.Application.Handlers;

public sealed class UpdatePlanCommandHandler(
    IPlanRepository repository,
    IMediator mediator,
    IClock clock)
    : IRequestHandler<UpdatePlanCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await repository.GetByIdAsync(PlanId.From(request.PlanId), cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_not_found")));
        }

        var billingCycle = Enum.Parse<BillingCycle>(request.BillingCycle, ignoreCase: true);

        var update = plan.Update(
            request.Name,
            request.Features,
            request.Limits,
            request.BasePriceUsd,
            billingCycle,
            request.Currency.ToUpperInvariant(),
            clock,
            HubSystemActors.SystemOperator);
        if (update.IsFailure)
        {
            return update;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Fan out an entitlement recompute to every subscription bound to this
        // plan. P02c-1 runs an in-process loop (tenant volume is tiny); a
        // background job (Hangfire) replaces it when volume warrants (Phase 09b/11).
        var boundTenants = await mediator.Send(new GetSubscriptionsByPlanQuery(request.PlanId), cancellationToken)
            .ConfigureAwait(false);
        if (boundTenants.IsFailure)
        {
            return Result<Unit>.Fail(boundTenants.Error!);
        }

        foreach (var tenantId in boundTenants.Value!)
        {
            var recompute = await mediator.Send(new RecomputeEntitlementCommand(tenantId), cancellationToken)
                .ConfigureAwait(false);
            if (recompute.IsFailure)
            {
                return Result<Unit>.Fail(recompute.Error!);
            }
        }

        return Result<Unit>.Ok(Unit.Value);
    }
}
