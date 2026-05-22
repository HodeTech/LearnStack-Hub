using LearnStack.Hub.Modules.Plans.Domain.Events;
using LearnStack.Hub.SharedKernel.Compliance;
using LearnStack.Hub.SharedKernel.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;

namespace LearnStack.Hub.Modules.Plans.Domain;

/// <summary>
/// A plan in the catalogue: the feature toggles, numeric limits, and (later)
/// compliance defaults a subscription projects into a tenant's entitlement.
/// Operators author plans; plans are data, never code. Feature / limit key
/// validation lives in the command validator (keys arrive as data), so the
/// factory trusts already-validated input.
/// </summary>
public sealed class Plan : AuditableEntity<PlanId>
{
    // Non-readonly: EF replaces these field instances on materialization (the
    // JSONB value converter deserialises into a fresh dictionary).
    private Dictionary<string, bool> _features = new(StringComparer.Ordinal);
    private Dictionary<string, long> _limits = new(StringComparer.Ordinal);
    private Dictionary<string, ComplianceCap> _complianceDefaults = new(StringComparer.Ordinal);

    private Plan(PlanId id)
        : base(id)
    {
    }

    // EF Core materialization ctor.
    private Plan()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public PlanTier Tier { get; private set; }

    public IReadOnlyDictionary<string, bool> Features => _features;

    public IReadOnlyDictionary<string, long> Limits => _limits;

    /// <summary>Compliance defaults — empty in P02c-1; populated by the Compliance module (P02c-5).</summary>
    public IReadOnlyDictionary<string, ComplianceCap> ComplianceDefaults => _complianceDefaults;

    public decimal BasePriceUsd { get; private set; }

    public BillingCycle BillingCycle { get; private set; }

    public string Currency { get; private set; } = "USD";

    public bool IsActive { get; private set; }

    /// <summary>
    /// Creates an active plan and stamps audit columns. Input is assumed
    /// validated (feature/limit keys checked by the command validator).
    /// </summary>
    public static Plan Create(
        PlanId id,
        string name,
        PlanTier tier,
        IReadOnlyDictionary<string, bool> features,
        IReadOnlyDictionary<string, long> limits,
        decimal basePriceUsd,
        BillingCycle billingCycle,
        string currency,
        IClock clock,
        OperatorId by)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(clock);

        var plan = new Plan(id)
        {
            Name = name,
            Tier = tier,
            BasePriceUsd = basePriceUsd,
            BillingCycle = billingCycle,
            Currency = currency,
            IsActive = true,
        };

        plan.ReplaceFeatures(features);
        plan.ReplaceLimits(limits);
        plan.MarkCreated(clock.UtcNow, by);
        plan.RaiseDomainEvent(new PlanCreatedDomainEvent(id, tier)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return plan;
    }

    /// <summary>
    /// Replaces the mutable plan definition (name, features, limits, pricing).
    /// Tier is immutable after creation. Raises <see cref="PlanUpdatedDomainEvent"/>
    /// so the Plans handler can fan out an entitlement recompute.
    /// </summary>
    public Result<Unit> Update(
        string name,
        IReadOnlyDictionary<string, bool> features,
        IReadOnlyDictionary<string, long> limits,
        decimal basePriceUsd,
        BillingCycle billingCycle,
        string currency,
        IClock clock,
        OperatorId by)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(clock);

        Name = name;
        BasePriceUsd = basePriceUsd;
        BillingCycle = billingCycle;
        Currency = currency;
        ReplaceFeatures(features);
        ReplaceLimits(limits);
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new PlanUpdatedDomainEvent(Id)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary>Deactivates the plan. Already-inactive is a business-rule violation, not a programmer error.</summary>
    public Result<Unit> Deactivate(IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (!IsActive)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_business_rule_violation")));
        }

        IsActive = false;
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new PlanDeactivatedDomainEvent(Id)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    private void ReplaceFeatures(IReadOnlyDictionary<string, bool> features)
    {
        _features.Clear();
        foreach (var (key, value) in features)
        {
            _features[key] = value;
        }
    }

    private void ReplaceLimits(IReadOnlyDictionary<string, long> limits)
    {
        _limits.Clear();
        foreach (var (key, value) in limits)
        {
            _limits[key] = value;
        }
    }
}
