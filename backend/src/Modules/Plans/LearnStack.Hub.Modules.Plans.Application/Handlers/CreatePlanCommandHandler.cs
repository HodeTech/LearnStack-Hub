using LearnStack.Hub.Modules.Plans.Application.Abstractions;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.Plans.Application.Handlers;

public sealed class CreatePlanCommandHandler(
    IPlanRepository repository,
    IClock clock,
    IGuidFactory guids)
    : IRequestHandler<CreatePlanCommand, Result<PlanDto>>
{
    public async Task<Result<PlanDto>> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tier = Enum.Parse<PlanTier>(request.Tier, ignoreCase: true);
        var billingCycle = Enum.Parse<BillingCycle>(request.BillingCycle, ignoreCase: true);

        var plan = Plan.Create(
            PlanId.From(guids.NewUuidV7()),
            request.Name,
            tier,
            request.Features,
            request.Limits,
            request.BasePriceUsd,
            billingCycle,
            request.Currency.ToUpperInvariant(),
            clock,
            HubSystemActors.SystemOperator);

        await repository.AddAsync(plan, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PlanDto>.Ok(plan.ToDto());
    }
}
