using LearnStack.Hub.Modules.TenantLifecycle.Domain.Events;
using LearnStack.Hub.SharedKernel.Domain;
using LearnStack.Hub.SharedKernel.Hosting;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;

namespace LearnStack.Hub.Modules.TenantLifecycle.Domain;

/// <summary>
/// The Hub-side mirror of LearnStack's <c>Tenant</c> — metadata only, never
/// tenant content. Hub is authoritative for plan-related fields; LearnStack
/// core is authoritative for operational fields. Status transitions and
/// phone-home updates mutate it.
/// </summary>
public sealed class LearnStackTenant : AuditableEntity<LearnStackTenantId>
{
    private LearnStackTenant(LearnStackTenantId id)
        : base(id)
    {
    }

    // EF Core materialization ctor.
    private LearnStackTenant()
    {
    }

    public string Slug { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public TenantStatus Status { get; private set; }

    /// <summary>Production deployment shape (never <see cref="DeploymentMode.Development"/> — that is a host concern).</summary>
    public DeploymentMode DeploymentMode { get; private set; }

    public DateTimeOffset? LastPhoneHomeAt { get; private set; }

    /// <summary>
    /// Creates a tenant in <see cref="TenantStatus.Trial"/>. The id is minted by
    /// the caller (app-side UUIDv7) so Hub holds it before flush and pushes the
    /// same id to LearnStack core (P02c-3).
    /// </summary>
    public static LearnStackTenant Create(
        LearnStackTenantId id,
        string slug,
        string displayName,
        DeploymentMode deploymentMode,
        IClock clock,
        OperatorId by)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(clock);

        var tenant = new LearnStackTenant(id)
        {
            Slug = slug,
            DisplayName = displayName,
            Status = TenantStatus.Trial,
            DeploymentMode = deploymentMode,
        };

        tenant.MarkCreated(clock.UtcNow, by);
        tenant.RaiseDomainEvent(new TenantCreatedDomainEvent(id)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return tenant;
    }

    /// <summary><c>Trial | Suspended → Active</c>.</summary>
    public Result<Unit> Activate(IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not (TenantStatus.Trial or TenantStatus.Suspended))
        {
            return InvalidTransition();
        }

        Status = TenantStatus.Active;
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new TenantActivatedDomainEvent(Id)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary><c>Active → Suspended</c>.</summary>
    public Result<Unit> Suspend(string reason, IClock clock, OperatorId by)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not TenantStatus.Active)
        {
            return InvalidTransition();
        }

        Status = TenantStatus.Suspended;
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new TenantSuspendedDomainEvent(Id, reason)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary><c>Active | Suspended → Archived</c>.</summary>
    public Result<Unit> Archive(IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not (TenantStatus.Active or TenantStatus.Suspended))
        {
            return InvalidTransition();
        }

        Status = TenantStatus.Archived;
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new TenantArchivedDomainEvent(Id)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary><c>Archived → Terminated</c>. The hard-delete-with-confirmation flow lands in a later packet.</summary>
    public Result<Unit> Terminate(IClock clock, OperatorId by)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not TenantStatus.Archived)
        {
            return InvalidTransition();
        }

        Status = TenantStatus.Terminated;
        MarkUpdated(clock.UtcNow, by);
        RaiseDomainEvent(new TenantTerminatedDomainEvent(Id)
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = clock.UtcNow,
        });

        return Result<Unit>.Ok(Unit.Value);
    }

    /// <summary>Records a phone-home heartbeat (no status change). P02c-6 drives the caller; the shape ships now.</summary>
    public void RecordPhoneHome(DateTimeOffset at, OperatorId by)
    {
        LastPhoneHomeAt = at;
        MarkUpdated(at, by);
    }

    private static Result<Unit> InvalidTransition() =>
        Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_business_rule_violation")));
}
