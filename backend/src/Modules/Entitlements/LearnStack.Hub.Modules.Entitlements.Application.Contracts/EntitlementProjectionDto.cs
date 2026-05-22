using System.Text.Json.Serialization;

namespace LearnStack.Hub.Modules.Entitlements.Application.Contracts;

/// <summary>
/// The entitlement projection wire contract (entitlement-projection.md §
/// Projection shape + Architecture 24 § 4). Its System.Text.Json shape <strong>is</strong>
/// the contract LearnStack core consumes — treat changes as contract changes
/// (the <c>EntitlementProjection_Shape_IsStable</c> contract test snapshots it
/// against <c>entitlement-v1.schema.json</c>). Property names are pinned via
/// <see cref="JsonPropertyNameAttribute"/>, independent of any serializer policy.
/// </summary>
public sealed record EntitlementProjectionDto
{
    [JsonPropertyName("tenant_id")]
    public required Guid TenantId { get; init; }

    [JsonPropertyName("tier")]
    public required string Tier { get; init; }

    [JsonPropertyName("features")]
    public required IReadOnlyDictionary<string, bool> Features { get; init; }

    [JsonPropertyName("limits")]
    public required IReadOnlyDictionary<string, long> Limits { get; init; }

    [JsonPropertyName("compliance")]
    public required ComplianceSectionDto Compliance { get; init; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("grace_until")]
    public DateTimeOffset? GraceUntil { get; init; }

    [JsonPropertyName("generation")]
    public required long Generation { get; init; }
}

/// <summary>The <c>compliance</c> envelope: a single <c>caps</c> map (empty <c>{}</c> in P02c-1).</summary>
public sealed record ComplianceSectionDto
{
    [JsonPropertyName("caps")]
    public required IReadOnlyDictionary<string, ComplianceCapDto> Caps { get; init; }
}

/// <summary>A single compliance cap on the wire: <c>{ allowed, forced, value? }</c>.</summary>
public sealed record ComplianceCapDto
{
    [JsonPropertyName("allowed")]
    public required bool Allowed { get; init; }

    [JsonPropertyName("forced")]
    public required bool Forced { get; init; }

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; init; }
}
