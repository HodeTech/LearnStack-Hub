namespace LearnStack.Hub.SharedKernel.Time;

/// <summary>
/// Wall-clock abstraction for domain and application code. No production code
/// reads <c>DateTimeOffset.UtcNow</c> directly — every timestamp flows through
/// <see cref="IClock"/> so tests can pin time deterministically.
/// </summary>
public interface IClock
{
    /// <summary>Current UTC instant. Persisted timestamps are always UTC.</summary>
    DateTimeOffset UtcNow { get; }
}
