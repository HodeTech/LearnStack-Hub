using LearnStack.Hub.Modules.Plans.Application.Abstractions;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Plans.Application.Handlers;

public sealed class GetPlanQueryHandler(IPlanRepository repository)
    : IRequestHandler<GetPlanQuery, Result<PlanDto>>
{
    public async Task<Result<PlanDto>> Handle(GetPlanQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await repository.GetByIdAsync(PlanId.From(request.PlanId), cancellationToken)
            .ConfigureAwait(false);

        return plan is null
            ? Result<PlanDto>.Fail(new Error(new LocalizedMessage("lockey_not_found")))
            : Result<PlanDto>.Ok(plan.ToDto());
    }
}
