namespace LearnStack.Hub.SharedKernel.Time;

/// <summary>
/// Production <see cref="IClock"/> backed by the BCL system clock. Registered
/// as a singleton at the composition root.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
