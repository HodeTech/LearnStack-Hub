using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Plans.Application.Contracts;

/// <summary>Creates a plan. <c>Tier</c> / <c>BillingCycle</c> are wire strings the validator + handler parse.</summary>
public sealed record CreatePlanCommand(
    string Name,
    string Tier,
    IReadOnlyDictionary<string, bool> Features,
    IReadOnlyDictionary<string, long> Limits,
    decimal BasePriceUsd,
    string BillingCycle,
    string Currency) : IRequest<Result<PlanDto>>;

/// <summary>Updates a plan's mutable definition; triggers the entitlement recompute fan-out.</summary>
public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Name,
    IReadOnlyDictionary<string, bool> Features,
    IReadOnlyDictionary<string, long> Limits,
    decimal BasePriceUsd,
    string BillingCycle,
    string Currency) : IRequest<Result<Unit>>;

/// <summary>Deactivates a plan so it can no longer be newly subscribed.</summary>
public sealed record DeactivatePlanCommand(Guid PlanId) : IRequest<Result<Unit>>;
