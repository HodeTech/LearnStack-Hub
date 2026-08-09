using FluentAssertions;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Time;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.SharedKernel;

public sealed class ClockAndAuditTests
{
    [Fact]
    public void FixedClock_AdvancesOnlyExplicitly()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero));

        var t0 = clock.UtcNow;
        clock.Advance(TimeSpan.FromHours(1));

        clock.UtcNow.Should().Be(t0.AddHours(1));
    }

    [Fact]
    public void FixedClock_NormalisesToUtc()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 5, 22, 3, 0, 0, TimeSpan.FromHours(3)));

        clock.UtcNow.Offset.Should().Be(TimeSpan.Zero);
        clock.UtcNow.Hour.Should().Be(0);
    }

    [Fact]
    public void FixedGuidFactory_ReturnsSequenceThenThrows()
    {
        var g1 = Guid.CreateVersion7();
        var factory = new FixedGuidFactory(g1);

        factory.NewUuidV7().Should().Be(g1);

        var act = () => factory.NewUuidV7();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void OperatorId_RoundTripsGuid()
    {
        var guid = Guid.CreateVersion7();

        var id = OperatorId.From(guid);

        id.Value.Should().Be(guid);
    }
}
