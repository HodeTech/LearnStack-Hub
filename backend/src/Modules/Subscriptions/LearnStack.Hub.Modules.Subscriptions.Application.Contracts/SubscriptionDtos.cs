namespace LearnStack.Hub.Modules.Subscriptions.Application.Contracts;

/// <summary>Full subscription projection.</summary>
public sealed record SubscriptionDto(
    Guid TenantId,
    Guid PlanId,
    string Status,
    DateTimeOffset? TrialStart,
    DateTimeOffset? TrialEnd,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    string? PaymentProvider);

/// <summary>Compact subscription projection for list endpoints.</summary>
public sealed record SubscriptionSummaryDto(
    Guid TenantId,
    Guid PlanId,
    string Status,
    DateTimeOffset CurrentPeriodEnd);
