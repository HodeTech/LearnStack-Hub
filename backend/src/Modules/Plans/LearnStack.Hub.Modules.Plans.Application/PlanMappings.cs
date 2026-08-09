using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Domain;

namespace LearnStack.Hub.Modules.Plans.Application;

/// <summary>Maps the <see cref="Plan"/> aggregate to its contract DTOs. Tier / cycle render lowercase to match the wire shape.</summary>
internal static class PlanMappings
{
    public static PlanDto ToDto(this Plan plan) => new(
        plan.Id.Value,
        plan.Name,
        plan.Tier.ToWire(),
        new Dictionary<string, bool>(plan.Features, StringComparer.Ordinal),
        new Dictionary<string, long>(plan.Limits, StringComparer.Ordinal),
        plan.BasePriceUsd,
        plan.BillingCycle.ToWire(),
        plan.Currency,
        plan.IsActive);

    public static PlanSummaryDto ToSummaryDto(this Plan plan) => new(
        plan.Id.Value,
        plan.Name,
        plan.Tier.ToWire(),
        plan.BasePriceUsd,
        plan.BillingCycle.ToWire(),
        plan.IsActive);

    public static string ToWire(this PlanTier tier) => tier.ToString().ToLowerInvariant();

    public static string ToWire(this BillingCycle cycle) => cycle.ToString().ToLowerInvariant();
}
