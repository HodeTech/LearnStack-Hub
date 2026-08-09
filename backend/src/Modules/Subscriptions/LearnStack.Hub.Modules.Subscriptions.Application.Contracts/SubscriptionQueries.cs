using LearnStack.Hub.SharedKernel.Pagination;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Subscriptions.Application.Contracts;

/// <summary>Reads the subscription for a tenant.</summary>
public sealed record GetSubscriptionQuery(Guid TenantId) : IRequest<Result<SubscriptionDto>>;

/// <summary>
/// Returns the tenant ids of every subscription bound to a plan — consumed by
/// the Plans module's fan-out recompute so Plans never reaches into the
/// Subscriptions Domain.
/// </summary>
public sealed record GetSubscriptionsByPlanQuery(Guid PlanId) : IRequest<Result<IReadOnlyList<Guid>>>;

/// <summary>Lists subscriptions (cursor-paginated), optionally filtered by status.</summary>
public sealed record ListSubscriptionsQuery(string? Status, string? Cursor, int Limit)
    : IRequest<Result<Page<SubscriptionSummaryDto>>>;
