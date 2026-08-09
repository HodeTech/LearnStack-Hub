using LearnStack.Hub.Modules.Plans.Application.Abstractions;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.Plans.Application.Handlers;

public sealed class DeactivatePlanCommandHandler(
    IPlanRepository repository,
    IClock clock)
    : IRequestHandler<DeactivatePlanCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeactivatePlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await repository.GetByIdAsync(PlanId.From(request.PlanId), cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_not_found")));
        }

        var result = plan.Deactivate(clock, HubSystemActors.SystemOperator);
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Unit>.Ok(Unit.Value);
    }
}
