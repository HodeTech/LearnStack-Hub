namespace LearnStack.Hub.SharedKernel.Persistence;

/// <summary>
/// Marker for any entity whose updates use optimistic concurrency. EF Core
/// configures <see cref="Version"/> as the row version token so concurrent
/// updates fail with <c>DbUpdateConcurrencyException</c> — translated to a
/// <c>Result.Fail(concurrency_conflict)</c>.
/// </summary>
public interface IOptimisticConcurrency
{
    /// <summary>
    /// Monotonically-increasing version counter. EF Core bumps this on every
    /// <c>SaveChangesAsync</c>; aggregate code does not mutate it directly.
    /// </summary>
    uint Version { get; }
}
