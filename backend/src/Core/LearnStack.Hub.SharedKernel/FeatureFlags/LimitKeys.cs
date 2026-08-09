using System.Collections.Immutable;

namespace LearnStack.Hub.SharedKernel.FeatureFlags;

/// <summary>
/// The Hub-side registry of known <see cref="LimitKey"/>s. Keys carry the
/// <c>limits.</c> prefix per the entitlement projection wire-shape
/// (Architecture 24 § 4 + <c>docs/architecture/entitlement-projection.md</c>),
/// which is the contract LearnStack core consumes. Values project as
/// <c>long</c>: <c>-1</c> = unlimited, <c>0</c> = not available. See
/// <see cref="FeatureKeys"/> for the registry-sync caveat.
/// </summary>
public static class LimitKeys
{
    public static readonly LimitKey MaxUsers = new("limits.max_users");
    public static readonly LimitKey MaxOrganizations = new("limits.max_organizations");
    public static readonly LimitKey ClassroomMinutesPerMonth = new("limits.classroom_minutes_per_month");
    public static readonly LimitKey RecordingStorageGb = new("limits.recording_storage_gb");
    public static readonly LimitKey MediaStorageGb = new("limits.media_storage_gb");
    public static readonly LimitKey MediaBandwidthGbPerMonth = new("limits.media_bandwidth_gb_per_month");
    public static readonly LimitKey ApiRatePerMinute = new("limits.api_rate_per_minute");
    public static readonly LimitKey MaxCustomContentTypes = new("limits.max_custom_content_types");
    public static readonly LimitKey MaxPageBlockDefinitions = new("limits.max_page_block_definitions");

    /// <summary>Every known limit key.</summary>
    public static ImmutableArray<LimitKey> All { get; } =
    [
        MaxUsers,
        MaxOrganizations,
        ClassroomMinutesPerMonth,
        RecordingStorageGb,
        MediaStorageGb,
        MediaBandwidthGbPerMonth,
        ApiRatePerMinute,
        MaxCustomContentTypes,
        MaxPageBlockDefinitions,
    ];

    private static readonly ImmutableHashSet<string> KnownValues =
        All.Select(k => k.Value).ToImmutableHashSet(StringComparer.Ordinal);

    /// <summary>Returns <c>true</c> when <paramref name="value"/> is a registered limit-key wire string.</summary>
    public static bool IsKnown(string value) => KnownValues.Contains(value);
}
