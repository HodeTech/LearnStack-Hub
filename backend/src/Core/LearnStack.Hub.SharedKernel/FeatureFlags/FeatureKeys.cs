using System.Collections.Immutable;

namespace LearnStack.Hub.SharedKernel.FeatureFlags;

/// <summary>
/// The Hub-side registry of known <see cref="FeatureKey"/>s. Hub is the
/// authoring side — the plan editor (P02c-4) writes these keys into
/// <c>Plan.features</c>, and the <c>Plan</c> validator rejects any key not in
/// <see cref="All"/>.
/// </summary>
/// <remarks>
/// <para>
/// Seeded from the entitlement projection wire-shape (Architecture 24 § 4 +
/// <c>docs/architecture/entitlement-projection.md</c>) — the projection JSON
/// is the load-bearing contract surface LearnStack core consumes — together
/// with the broader feature set in ADR-0021 Amendment 1.
/// </para>
/// <para>
/// <strong>Registry sync.</strong> Hub and LearnStack core each keep their own
/// copy of this registry, so they can drift. Where ADR-0021 Amendment 1 and
/// the projection example disagree (e.g. the limit-key prefix), the projection
/// wire-shape wins because it is the contract the projection serialiser emits.
/// A future cross-repo reconciliation (or a shared contracts package, Phase 11)
/// is the durable fix — tracked in <c>docs/roadmap/README.md</c>.
/// </para>
/// </remarks>
public static class FeatureKeys
{
    public static readonly FeatureKey ClassroomRecording = new("classroom.recording");
    public static readonly FeatureKey ClassroomBreakoutRooms = new("classroom.breakout_rooms");
    public static readonly FeatureKey CustomDomain = new("tenancy.custom_domain");
    public static readonly FeatureKey WhiteLabelBranding = new("tenancy.white_label_branding");
    public static readonly FeatureKey UnlimitedContentTypes = new("customization.unlimited_content_types");
    public static readonly FeatureKey SsoSaml = new("identity.sso.saml");
    public static readonly FeatureKey SsoOidc = new("identity.sso.oidc");
    public static readonly FeatureKey Scim = new("identity.scim");
    public static readonly FeatureKey AdvancedReporting = new("analytics.advanced_reporting");
    public static readonly FeatureKey BulkImport = new("admin.bulk_import");
    public static readonly FeatureKey ApiAccess = new("integrations.api_access");
    public static readonly FeatureKey Webhooks = new("integrations.webhooks");
    public static readonly FeatureKey AuditExport = new("audit.export");
    public static readonly FeatureKey DataResidencySelection = new("compliance.data_residency");

    /// <summary>Every known feature key.</summary>
    public static ImmutableArray<FeatureKey> All { get; } =
    [
        ClassroomRecording,
        ClassroomBreakoutRooms,
        CustomDomain,
        WhiteLabelBranding,
        UnlimitedContentTypes,
        SsoSaml,
        SsoOidc,
        Scim,
        AdvancedReporting,
        BulkImport,
        ApiAccess,
        Webhooks,
        AuditExport,
        DataResidencySelection,
    ];

    private static readonly ImmutableHashSet<string> KnownValues =
        All.Select(k => k.Value).ToImmutableHashSet(StringComparer.Ordinal);

    /// <summary>Returns <c>true</c> when <paramref name="value"/> is a registered feature-key wire string.</summary>
    public static bool IsKnown(string value) => KnownValues.Contains(value);
}
