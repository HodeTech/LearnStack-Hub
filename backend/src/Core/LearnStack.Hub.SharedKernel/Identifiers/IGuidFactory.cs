namespace LearnStack.Hub.SharedKernel.Identifiers;

/// <summary>
/// GUID minting abstraction. Application-side GUIDs flow through this interface
/// so tests can pin the sequence deterministically.
/// </summary>
public interface IGuidFactory
{
    /// <summary>
    /// Mints a UUIDv7 (<c>Guid.CreateVersion7</c>). Preferred for every new
    /// aggregate root identifier because the timestamp prefix keeps DB-side
    /// indexes sorted by insertion order.
    /// </summary>
    Guid NewUuidV7();

    /// <summary>Mints a UUIDv4. Reserved for identifiers that should not leak insertion order.</summary>
    Guid NewUuidV4();
}
