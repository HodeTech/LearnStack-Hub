using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Subscriptions.Application.Contracts;

/// <summary>Creates the initial trial subscription (usually called by CreateTenantCommand). Triggers the first recompute.</summary>
public sealed record StartTrialCommand(Guid TenantId, Guid PlanId, int TrialDays) : IRequest<Result<SubscriptionDto>>;

/// <summary><c>Trial | PastDue → Active</c> with a fresh period; triggers a recompute.</summary>
public sealed record ActivateSubscriptionCommand(
    Guid TenantId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd) : IRequest<Result<Unit>>;

/// <summary>Rebinds the subscription to a new plan (no proration in P02c-1); triggers a recompute.</summary>
public sealed record ChangePlanCommand(Guid TenantId, Guid NewPlanId) : IRequest<Result<Unit>>;

/// <summary>Cancels the subscription immediately or at period end; triggers a recompute.</summary>
public sealed record CancelSubscriptionCommand(Guid TenantId, bool AtPeriodEnd) : IRequest<Result<Unit>>;
