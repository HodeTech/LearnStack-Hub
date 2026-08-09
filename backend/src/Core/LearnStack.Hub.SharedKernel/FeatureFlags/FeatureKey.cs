namespace LearnStack.Hub.SharedKernel.FeatureFlags;

/// <summary>
/// Typed feature-flag key (mirror of LearnStack core's <c>FeatureKey</c> per
/// ADR-0021 Amendment 1). The wire string is dotted snake_case with <strong>no
/// <c>.enabled</c> suffix</strong> — every feature is implicitly boolean. The
/// projection serialises <c>Plan.features</c> as <c>Dictionary&lt;string, bool&gt;</c>
/// keyed on these strings; a drift from LearnStack core's registry breaks the
/// entitlement projection LearnStack consumes.
/// </summary>
public readonly record struct FeatureKey(string Value)
{
    public override string ToString() => Value;
}
