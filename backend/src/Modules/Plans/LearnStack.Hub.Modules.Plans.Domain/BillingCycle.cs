namespace LearnStack.Hub.Modules.Plans.Domain;

/// <summary>Billing cadence for a plan. Persisted as snake_case text with a <c>ck_plans_billing_cycle</c> check.</summary>
public enum BillingCycle
{
    Monthly,
    Annual,
}
