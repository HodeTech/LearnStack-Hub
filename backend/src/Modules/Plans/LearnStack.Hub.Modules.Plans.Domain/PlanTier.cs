namespace LearnStack.Hub.Modules.Plans.Domain;

/// <summary>
/// Plan tier (Architecture 24 § 8). Persisted as snake_case text with a
/// <c>ck_plans_tier</c> check constraint.
/// </summary>
public enum PlanTier
{
    Starter,
    Growth,
    Scale,
    Enterprise,
    Custom,
}
