using System.Collections.ObjectModel;
using LearnStack.Hub.SharedKernel.Identifiers;

namespace LearnStack.Hub.SharedKernel.Domain;

/// <summary>
/// Aggregate base. Carries identity and raises in-process
/// <see cref="IDomainEvent"/>s; does <em>not</em> carry the audit columns —
/// those belong to mutable aggregates and live on
/// <see cref="AuditableEntity{TId}"/>. <see cref="Entitlement"/> inherits this
/// base directly (one row per tenant, replaced wholesale on recompute, no audit
/// columns).
/// </summary>
/// <remarks>
/// Identity-based equality with three guards: transient entities
/// (<see cref="Id"/> equal to <c>default(TId)</c>) match only by reference,
/// runtime-type mismatches never match even when the ID matches, and the hash
/// code partitions transient instances apart so EF Core's change tracker and
/// any <c>HashSet</c> navigation behave correctly. Domain-event collection
/// state is lazily allocated.
/// </remarks>
public abstract class Entity<TId> : IHasId<TId>, IHasDomainEvents
    where TId : struct, IStronglyTypedId<Guid>
{
    private List<IDomainEvent>? _domainEvents;
    private ReadOnlyCollection<IDomainEvent>? _domainEventsView;

    protected Entity(TId id)
    {
        Id = id;
    }

    // EF Core / ORM materialization ctor.
    protected Entity()
    {
    }

    public TId Id { get; protected init; }

    /// <summary>
    /// In-process domain events raised since the last
    /// <see cref="ClearDomainEvents"/>. Returns a cached read-only wrapper so
    /// callers cannot mutate the collection out from under the aggregate.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEventsView ??= (_domainEvents ??= []).AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        (_domainEvents ??= []).Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents?.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (Id.Equals(default(TId)) || other.Id.Equals(default(TId)))
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() =>
        Id.Equals(default(TId))
            ? base.GetHashCode()
            : HashCode.Combine(GetType(), Id);
}
