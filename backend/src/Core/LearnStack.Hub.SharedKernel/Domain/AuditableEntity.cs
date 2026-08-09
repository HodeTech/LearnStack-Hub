using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Persistence;

namespace LearnStack.Hub.SharedKernel.Domain;

/// <summary>
/// Mutable aggregate base. Carries the audit columns every Hub aggregate
/// mirrors (<c>CreatedAt</c> / <c>CreatedBy</c> / <c>UpdatedAt</c> /
/// <c>UpdatedBy</c> / <c>DeletedAt</c> / <c>DeletedBy</c> / <c>Version</c>) and
/// implements <see cref="ISoftDelete"/> + <see cref="IOptimisticConcurrency"/>.
/// </summary>
/// <remarks>
/// The load-bearing Hub adjustment: the <c>*By</c> audit columns are typed on
/// <see cref="OperatorId"/> (a Hub operator), NOT a tenant <c>UserId</c> — Hub
/// actions are performed by operators, never tenant users.
/// </remarks>
public abstract class AuditableEntity<TId>
    : Entity<TId>, ISoftDelete, IOptimisticConcurrency
    where TId : struct, IStronglyTypedId<Guid>
{
    protected AuditableEntity(TId id)
        : base(id)
    {
    }

    // EF Core / ORM materialization ctor.
    protected AuditableEntity()
    {
    }

    public DateTimeOffset CreatedAt { get; protected set; }

    public OperatorId CreatedBy { get; protected set; }

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public OperatorId? UpdatedBy { get; protected set; }

    public DateTimeOffset? DeletedAt { get; protected set; }

    public OperatorId? DeletedBy { get; protected set; }

    public uint Version { get; protected set; }

    /// <summary>
    /// Convenience projection of <see cref="DeletedAt"/> for in-process
    /// callers. EF global query filters should gate on <see cref="DeletedAt"/>
    /// directly — but note Hub does NOT register soft-delete query filters
    /// (operator-administered, cross-tenant by design); this property is for
    /// CLR-side reads only.
    /// </summary>
    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>
    /// Stamps <see cref="CreatedAt"/> / <see cref="CreatedBy"/> on first
    /// persist. Throws when already stamped — audit-trail integrity rules out
    /// silent overwrites.
    /// </summary>
    public void MarkCreated(DateTimeOffset at, OperatorId by)
    {
        EnsureValidAuditInput(at, by);

        if (CreatedAt != default)
        {
            throw new InvalidOperationException(
                "MarkCreated has already been called on this aggregate; the created-at / created-by columns are immutable after first stamp.");
        }

        CreatedAt = at;
        CreatedBy = by;
    }

    /// <summary>Stamps <see cref="UpdatedAt"/> / <see cref="UpdatedBy"/>.</summary>
    public void MarkUpdated(DateTimeOffset at, OperatorId by)
    {
        EnsureValidAuditInput(at, by);

        UpdatedAt = at;
        UpdatedBy = by;
    }

    /// <summary>
    /// Marks the entity soft-deleted and bumps <see cref="UpdatedAt"/> /
    /// <see cref="UpdatedBy"/> so the last-touched timestamp stays monotonic.
    /// </summary>
    public void SoftDelete(DateTimeOffset at, OperatorId by)
    {
        EnsureValidAuditInput(at, by);

        DeletedAt = at;
        DeletedBy = by;
        UpdatedAt = at;
        UpdatedBy = by;
    }

    // Audit metadata must always be meaningful: the default timestamp and the
    // default OperatorId (Guid.Empty) are programmer-error sentinels. Fail loud
    // at the call site rather than persisting them.
    private static void EnsureValidAuditInput(DateTimeOffset at, OperatorId by)
    {
        if (at == default)
        {
            throw new ArgumentException(
                "Audit timestamp must be a meaningful instant, not default(DateTimeOffset). Pass the value from IClock.UtcNow.",
                nameof(at));
        }

        if (by.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Audit actor must be a real OperatorId, not default(OperatorId). Pass the resolved operator id.",
                nameof(by));
        }
    }
}
