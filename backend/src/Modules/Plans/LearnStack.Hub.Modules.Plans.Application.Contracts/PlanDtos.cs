namespace LearnStack.Hub.Modules.Plans.Application.Contracts;

/// <summary>Full plan projection returned by <see cref="GetPlanQuery"/> / <see cref="CreatePlanCommand"/>.</summary>
public sealed record PlanDto(
    Guid Id,
    string Name,
    string Tier,
    IReadOnlyDictionary<string, bool> Features,
    IReadOnlyDictionary<string, long> Limits,
    decimal BasePriceUsd,
    string BillingCycle,
    string Currency,
    bool IsActive);

/// <summary>Compact plan projection for list endpoints.</summary>
public sealed record PlanSummaryDto(
    Guid Id,
    string Name,
    string Tier,
    decimal BasePriceUsd,
    string BillingCycle,
    bool IsActive);
