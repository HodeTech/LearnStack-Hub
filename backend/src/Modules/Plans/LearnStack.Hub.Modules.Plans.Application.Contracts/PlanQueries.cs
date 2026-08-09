using LearnStack.Hub.SharedKernel.Pagination;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Plans.Application.Contracts;

/// <summary>Reads a single plan by id.</summary>
public sealed record GetPlanQuery(Guid PlanId) : IRequest<Result<PlanDto>>;

/// <summary>Lists plans (cursor-paginated). <c>ActiveOnly</c> filters out deactivated plans.</summary>
public sealed record ListPlansQuery(bool? ActiveOnly, string? Cursor, int Limit)
    : IRequest<Result<Page<PlanSummaryDto>>>;
