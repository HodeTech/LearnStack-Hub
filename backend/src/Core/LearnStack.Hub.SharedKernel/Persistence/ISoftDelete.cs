using LearnStack.Hub.SharedKernel.Identifiers;

namespace LearnStack.Hub.SharedKernel.Persistence;

/// <summary>
/// Marker for any entity that participates in soft deletion. Callers never set
/// <see cref="DeletedAt"/> / <see cref="DeletedBy"/> directly — use the
/// aggregate's <c>SoftDelete</c> method. The actor is an <see cref="OperatorId"/>
/// (Hub operators perform deletions), not a tenant user.
/// </summary>
public interface ISoftDelete
{
    DateTimeOffset? DeletedAt { get; }

    OperatorId? DeletedBy { get; }
}
