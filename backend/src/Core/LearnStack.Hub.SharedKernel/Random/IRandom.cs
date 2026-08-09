using System.Diagnostics.CodeAnalysis;

namespace LearnStack.Hub.SharedKernel.Random;

/// <summary>
/// Randomness abstraction for domain and application code. Production code
/// never instantiates <see cref="System.Random"/> directly so tests can pin
/// the sequence deterministically. Cryptographic randomness is out of scope.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Mirrors System.Random.Next; C#-only codebase; no VB consumer affected.")]
public interface IRandom
{
    /// <summary>Returns a non-negative random integer less than <paramref name="maxExclusive"/>.</summary>
    int Next(int maxExclusive);

    /// <summary>Returns a random integer in <c>[minInclusive, maxExclusive)</c>.</summary>
    int Next(int minInclusive, int maxExclusive);

    /// <summary>Returns a random double in <c>[0.0, 1.0)</c>.</summary>
    double NextDouble();

    /// <summary>Fills the destination span with random bytes.</summary>
    void NextBytes(Span<byte> destination);
}
