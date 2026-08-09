using LearnStack.Hub.Modules.Entitlements.Application.Contracts;
using LearnStack.Hub.Modules.Entitlements.Domain;

namespace LearnStack.Hub.Modules.Entitlements.Application;

/// <summary>Maps the <see cref="Entitlement"/> aggregate to the wire-contract <see cref="EntitlementProjectionDto"/>.</summary>
internal static class EntitlementMappings
{
    public static EntitlementProjectionDto ToProjectionDto(this Entitlement e) => new()
    {
        TenantId = e.Id.Value,
        Tier = e.Tier,
        Features = new Dictionary<string, bool>(e.Features, StringComparer.Ordinal),
        Limits = new Dictionary<string, long>(e.Limits, StringComparer.Ordinal),
        Compliance = new ComplianceSectionDto
        {
            Caps = e.ComplianceCaps.ToDictionary(
                kvp => kvp.Key,
                kvp => new ComplianceCapDto
                {
                    Allowed = kvp.Value.Allowed,
                    Forced = kvp.Value.Forced,
                    Value = kvp.Value.Value,
                },
                StringComparer.Ordinal),
        },
        ExpiresAt = e.ExpiresAt,
        GraceUntil = e.GraceUntil,
        Generation = e.Generation,
    };
}
