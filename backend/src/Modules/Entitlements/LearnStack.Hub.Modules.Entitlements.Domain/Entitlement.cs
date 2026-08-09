using LearnStack.Hub.SharedKernel.Compliance;
using LearnStack.Hub.SharedKernel.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Time;

namespace LearnStack.Hub.Modules.Entitlements.Domain;

/// <summary>
/// The flattened, denormalised projection of <c>Plan</c> + <c>HubSubscription</c>
/// (+ <c>CompliancePolicy</c>, P02c-5) per tenant — the single shape LearnStack
/// core consumes across the Hub HTTPS contract surface. Inherits
/// <see cref="Entity{TId}"/>, NOT <c>AuditableEntity</c>: exactly one row per
/// tenant (PK = tenant id), replaced wholesale on recompute; its audit trail is
/// the monotonic <see cref="Generation"/> + <see cref="UpdatedAt"/>.
/// </summary>
public sealed class Entitlement : Entity<LearnStackTenantId>
{
    // Non-readonly: EF replaces these field instances on materialization.
    private Dictionary<string, bool> _features = new(StringComparer.Ordinal);
    private Dictionary<string, long> _limits = new(StringComparer.Ordinal);
    private Dictionary<string, ComplianceCap> _complianceCaps = new(StringComparer.Ordinal);

    private Entitlement(LearnStackTenantId tenantId)
        : base(tenantId)
    {
    }

    // EF Core materialization ctor.
    private Entitlement()
    {
    }

    public string Tier { get; private set; } = string.Empty;

    public IReadOnlyDictionary<string, bool> Features => _features;

    public IReadOnlyDictionary<string, long> Limits => _limits;

    /// <summary>Compliance caps — empty in P02c-1; populated by the Compliance module (P02c-5).</summary>
    public IReadOnlyDictionary<string, ComplianceCap> ComplianceCaps => _complianceCaps;

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? GraceUntil { get; private set; }

    /// <summary>Monotonic cache-coherency counter: starts at 1, only ever +1, never resets or decrements.</summary>
    public long Generation { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates the first entitlement for a tenant with <see cref="Generation"/> = 1.</summary>
    public static Entitlement CreateInitial(
        LearnStackTenantId tenantId,
        string tier,
        IReadOnlyDictionary<string, bool> features,
        IReadOnlyDictionary<string, long> limits,
        IReadOnlyDictionary<string, ComplianceCap> complianceCaps,
        DateTimeOffset? expiresAt,
        DateTimeOffset? graceUntil,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var entitlement = new Entitlement(tenantId);
        entitlement.Apply(tier, features, limits, complianceCaps, expiresAt, graceUntil);
        entitlement.Generation = 1;
        entitlement.UpdatedAt = clock.UtcNow;
        return entitlement;
    }

    /// <summary>
    /// Replaces the projection fields and bumps <see cref="Generation"/> by
    /// exactly 1. The increment + field replacement happen in the same
    /// transaction (the live TransactionBehavior covers commit/rollback).
    /// </summary>
    public void Recompute(
        string tier,
        IReadOnlyDictionary<string, bool> features,
        IReadOnlyDictionary<string, long> limits,
        IReadOnlyDictionary<string, ComplianceCap> complianceCaps,
        DateTimeOffset? expiresAt,
        DateTimeOffset? graceUntil,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        Apply(tier, features, limits, complianceCaps, expiresAt, graceUntil);
        Generation++;
        UpdatedAt = clock.UtcNow;
    }

    private void Apply(
        string tier,
        IReadOnlyDictionary<string, bool> features,
        IReadOnlyDictionary<string, long> limits,
        IReadOnlyDictionary<string, ComplianceCap> complianceCaps,
        DateTimeOffset? expiresAt,
        DateTimeOffset? graceUntil)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tier);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(complianceCaps);

        Tier = tier;
        _features = new Dictionary<string, bool>(features, StringComparer.Ordinal);
        _limits = new Dictionary<string, long>(limits, StringComparer.Ordinal);
        _complianceCaps = new Dictionary<string, ComplianceCap>(complianceCaps, StringComparer.Ordinal);
        ExpiresAt = expiresAt;
        GraceUntil = graceUntil;
    }
}
