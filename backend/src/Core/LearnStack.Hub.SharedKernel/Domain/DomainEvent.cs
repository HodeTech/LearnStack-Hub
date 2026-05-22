namespace LearnStack.Hub.SharedKernel.Domain;

/// <summary>
/// Base record for in-process domain events. <see cref="EventId"/> and
/// <see cref="OccurredAt"/> are <c>required init</c>: every event MUST be
/// stamped from the aggregate's injected <c>IGuidFactory</c> / <c>IClock</c>
/// so the deterministic-test abstractions are never bypassed.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public required Guid EventId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }
}
