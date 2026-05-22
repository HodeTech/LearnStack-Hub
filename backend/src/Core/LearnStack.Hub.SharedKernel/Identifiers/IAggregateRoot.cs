namespace LearnStack.Hub.SharedKernel.Identifiers;

/// <summary>
/// Marker for the root entity of an aggregate. Repositories accept and return
/// only aggregate roots; entities inside an aggregate are reached through the
/// root. Per ADR-0023 every aggregate root carries an
/// <see cref="IStronglyTypedId{TKey}"/>-shaped Vogen-emitted ID over <see cref="Guid"/>.
/// </summary>
public interface IAggregateRoot<out TId> : IHasId<TId>
    where TId : struct, IStronglyTypedId<Guid>
{
}
