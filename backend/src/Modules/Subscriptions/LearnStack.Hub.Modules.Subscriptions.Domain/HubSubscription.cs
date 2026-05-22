using LearnStack.Hub.Modules.Subscriptions.Domain.Events;
using LearnStack.Hub.SharedKernel.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;

namespace LearnStack.Hub.Modules.Subscriptions.Domain;

/// <summary>
/// Per-tenant binding to a plan + the subscription lifecycle state machine. One
/// per tenant (1:1). The second input (alongside <c>Plan</c>) to the entitlement
/// projection: its <c>CurrentPeriodEnd</c> feeds <c>Entitlement.expires_at</c>.
/// </summary>
/// <remarks>
/// <c>PlanId</c> is a plain <see cref="Guid"/> cross-module FK (not a typed
/// reference into the Plans module's Domain), per the module-boundary rules.
/// <c>payment_provider</c> / <c>provider_subscription_id</c> stay null in P02c-1
/// (billing is Phase 09b); the dunning <c>MarkPastDue</c>/<c>Cure</c> transitions
/// ship as shells so the enum is complete.
/// </remarks>
public sealed class HubSubscription : AuditableEntity<HubSubscriptionId>
{
    private HubSubscription(HubSubscriptionId id)
        : base(id)
    {
    }

    // EF Core materialization ctor.
    private HubSubscription()
    {
    }

    public LearnStackTenantId TenantId { get; private set; }

    public Guid PlanId { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public DateTimeOffset? TrialStart { get; private set; }

    public DateTimeOffset? TrialEnd { get; private set; }

    public DateTimeOffset CurrentPeriodStart { get; private set; }

    public DateTimeOffset CurrentPeriodEnd { get; private set; }

    public bool CancelAtPeriodEnd { get; private set; }

    public string? PaymentProvider { get; private set; }

    public string? ProviderSubscriptionId { get; private set; }

    /// <summary>Creates a subscription in <see cref="SubscriptionStatus.Trial"/> bound to <paramref name="planId"/>.</summary>
    public static HubSubscription StartTrial(
        HubSubscriptionId id,
        LearnStackTenantId tenantId,
        Guid planId,
        DateTimeOffset trialStart,
        DateTimeOffset trialEnd,
        IClock clock,
        OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var subscription = new HubSubscription(id)
        {
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Trial,
            TrialStart = trialStart,
            TrialEnd = trialEnd,
            CurrentPeriodStart = trialStart,
            CurrentPeriodEnd = trialEnd,
            CancelAtPeriodEnd = false,
        };

        subscription.MarkCreated(clock.UtcNow, by);
        subscription.RaiseDomainEvent(new SubscriptionStartedDomainEvent(tenantId, planId)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return subscription;
    }

    /// <summary><c>Trial | PastDue → Active</c> with a fresh billing period.</summary>
    public Result<Unit> Activate(DateTimeOffset periodStart, DateTimeOffset periodEnd, IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not (SubscriptionStatus.Trial or SubscriptionStatus.PastDue))
        {
            return InvalidTransition();
        }

        Status = SubscriptionStatus.Active;
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new SubscriptionActivatedDomainEvent(TenantId)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary>Rebinds an active subscription to a new plan (proration is Phase 09b).</summary>
    public Result<Unit> ChangePlan(Guid newPlanId, DateTimeOffset periodStart, DateTimeOffset periodEnd, IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not SubscriptionStatus.Active)
        {
            return InvalidTransition();
        }

        PlanId = newPlanId;
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new SubscriptionPlanChangedDomainEvent(TenantId, newPlanId)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary><c>Active → Canceled</c> immediately, or flags cancel-at-period-end.</summary>
    public Result<Unit> Cancel(bool atPeriodEnd, IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not SubscriptionStatus.Active)
        {
            return InvalidTransition();
        }

        if (atPeriodEnd)
        {
            CancelAtPeriodEnd = true;
        }
        else
        {
            Status = SubscriptionStatus.Canceled;
        }

        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new SubscriptionCanceledDomainEvent(TenantId, atPeriodEnd)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary><c>Trial | Canceled → Expired</c>.</summary>
    public Result<Unit> Expire(IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not (SubscriptionStatus.Trial or SubscriptionStatus.Canceled))
        {
            return InvalidTransition();
        }

        Status = SubscriptionStatus.Expired;
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new SubscriptionExpiredDomainEvent(TenantId)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary><c>Active → PastDue</c> — shell; the dunning driver lands in Phase 09b.</summary>
    public Result<Unit> MarkPastDue(IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not SubscriptionStatus.Active)
        {
            return InvalidTransition();
        }

        Status = SubscriptionStatus.PastDue;
        MarkUpdated(clock.UtcNow, by);
        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary><c>PastDue → Active</c> — shell; the dunning driver lands in Phase 09b.</summary>
    public Result<Unit> Cure(DateTimeOffset periodStart, DateTimeOffset periodEnd, IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not SubscriptionStatus.PastDue)
        {
            return InvalidTransition();
        }

        Status = SubscriptionStatus.Active;
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        MarkUpdated(clock.UtcNow, by);
        return Result<Unit>.Ok(Unit.Value);
    }

    private static Result<Unit> InvalidTransition() =>
        Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_business_rule_violation")));
}
