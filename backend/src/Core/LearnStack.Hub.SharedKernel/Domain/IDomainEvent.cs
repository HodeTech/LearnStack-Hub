using MediatR;

namespace LearnStack.Hub.SharedKernel.Domain;

/// <summary>
/// In-process domain event raised by an aggregate method. Dispatched in-process
/// by MediatR — the cross-module integration-event path (outbox + Dapr pub/sub)
/// is a different mechanism per ADR-0010.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>Unique event identifier. UUIDv7 so insertion order matches occurrence order.</summary>
    Guid EventId { get; }

    /// <summary>UTC instant the event was raised.</summary>
    DateTimeOffset OccurredAt { get; }
}
