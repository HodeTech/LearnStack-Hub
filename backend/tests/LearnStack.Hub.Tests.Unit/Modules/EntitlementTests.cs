using FluentAssertions;
using LearnStack.Hub.Modules.Entitlements.Domain;
using LearnStack.Hub.SharedKernel.Compliance;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Time;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.Modules;

public sealed class EntitlementTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero));

    private static readonly IReadOnlyDictionary<string, bool> Features =
        new Dictionary<string, bool> { ["classroom.recording"] = true };

    private static readonly IReadOnlyDictionary<string, long> Limits =
        new Dictionary<string, long> { ["limits.max_users"] = 500 };

    private static readonly IReadOnlyDictionary<string, ComplianceCap> EmptyCaps =
        new Dictionary<string, ComplianceCap>();

    private static Entitlement Initial() => Entitlement.CreateInitial(
        LearnStackTenantId.From(Guid.CreateVersion7()),
        "growth",
        Features,
        Limits,
        EmptyCaps,
        Clock.UtcNow.AddDays(14),
        graceUntil: null,
        Clock);

    [Fact]
    public void CreateInitial_StartsAtGenerationOne()
    {
        var entitlement = Initial();

        entitlement.Generation.Should().Be(1);
        entitlement.Tier.Should().Be("growth");
        entitlement.UpdatedAt.Should().Be(Clock.UtcNow);
        entitlement.ComplianceCaps.Should().BeEmpty();
    }

    [Fact]
    public void Recompute_IncrementsGenerationByExactlyOne_AndNeverResets()
    {
        var entitlement = Initial();

        for (var expected = 2; expected <= 5; expected++)
        {
            entitlement.Recompute("scale", Features, Limits, EmptyCaps, Clock.UtcNow.AddDays(30), null, Clock);
            entitlement.Generation.Should().Be(expected);
        }

        entitlement.Tier.Should().Be("scale");
    }

    [Fact]
    public void Recompute_ReplacesProjectionFields()
    {
        var entitlement = Initial();

        var newLimits = new Dictionary<string, long> { ["limits.max_users"] = 5000 };
        entitlement.Recompute("scale", Features, newLimits, EmptyCaps, null, null, Clock);

        entitlement.Limits["limits.max_users"].Should().Be(5000);
        entitlement.ExpiresAt.Should().BeNull();
    }
}
