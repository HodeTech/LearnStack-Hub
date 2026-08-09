namespace LearnStack.Hub.SharedKernel.FeatureFlags;

/// <summary>
/// Typed numeric-limit key (mirror of LearnStack core's <c>LimitKey</c> per
/// ADR-0021 Amendment 1). The wire string carries the <c>limits.</c> prefix;
/// the projected value is a <c>long</c> where <c>-1</c> = unlimited and
/// <c>0</c> = not available.
/// </summary>
public readonly record struct LimitKey(string Value)
{
    public override string ToString() => Value;
}
