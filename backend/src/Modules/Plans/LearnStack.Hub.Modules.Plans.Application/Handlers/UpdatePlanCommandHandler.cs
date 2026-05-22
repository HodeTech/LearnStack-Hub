using LearnStack.Hub.Modules.Plans.Application.Abstractions;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.Plans.Application.Handlers;

public sealed class UpdatePlanCommandHandler(
    IPlanRepository repository,
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

        // TODO(P02c-1d): fan out an entitlement recompute to every subscription
        // bound to this plan — ask Subscriptions for the tenant ids
        // (GetSubscriptionsByPlanQuery) then send RecomputeEntitlementCommand
        // per tenant. Wired once the Subscriptions + Entitlements contracts land.
        return Result<Unit>.Ok(Unit.Value);
    }
}
