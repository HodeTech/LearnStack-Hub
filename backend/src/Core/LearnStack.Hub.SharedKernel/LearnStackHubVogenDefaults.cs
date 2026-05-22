using Vogen;

namespace LearnStack.Hub.SharedKernel;

/// <summary>
/// Canonical <c>Conversions</c> mask for every Hub-declared
/// <c>[ValueObject&lt;T&gt;]</c> (mirror of LearnStack core's
/// <c>LearnStackVogenDefaults</c> per ADR-0023). Lives at the SharedKernel
/// root namespace because the mask covers both aggregate-root IDs
/// (<c>LearnStackTenantId</c>, <c>PlanId</c>, …) and the cross-cutting
/// <c>OperatorId</c>.
/// </summary>
public static class LearnStackHubVogenDefaults
{
    /// <summary>
    /// Conversion set every aggregate-root ID opts into: EF Core value
    /// converter, System.Text.Json converter, and the TypeConverter (which
    /// carries ASP.NET Core route-parameter binding).
    /// </summary>
    public const Conversions IdMask =
        Conversions.EfCoreValueConverter
        | Conversions.SystemTextJson
        | Conversions.TypeConverter;
}
