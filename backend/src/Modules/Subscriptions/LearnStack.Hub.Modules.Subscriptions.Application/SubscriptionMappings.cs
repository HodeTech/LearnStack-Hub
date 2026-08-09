using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;
using LearnStack.Hub.Modules.Subscriptions.Domain;

namespace LearnStack.Hub.Modules.Subscriptions.Application;

internal static class SubscriptionMappings
{
    public static SubscriptionDto ToDto(this HubSubscription s) => new(
        s.TenantId.Value,
        s.PlanId,
        s.Status.ToString(),
        s.TrialStart,
        s.TrialEnd,
        s.CurrentPeriodStart,
        s.CurrentPeriodEnd,
        s.CancelAtPeriodEnd,
        s.PaymentProvider);

    public static SubscriptionSummaryDto ToSummaryDto(this HubSubscription s) => new(
        s.TenantId.Value,
        s.PlanId,
        s.Status.ToString(),
        s.CurrentPeriodEnd);
}
