namespace LearnStack.Hub.Modules.Subscriptions.Domain;

/// <summary>
/// Subscription lifecycle status. Persisted as snake_case... no — stored as the
/// enum name with a <c>ck_subscriptions_status</c> check constraint. The
/// PastDue/Cure dunning path is Phase 09b; the value exists so the enum is
/// complete.
/// </summary>
public enum SubscriptionStatus
{
    Trial,
    Active,
    PastDue,
    Canceled,
    Expired,
}
