using FluentAssertions;
using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Time;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.Modules;

public sealed class PlanTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero));
    private static readonly OperatorId Actor = HubSystemActors.SystemOperator;

    private static Plan NewPlan() => Plan.Create(
        PlanId.From(Guid.CreateVersion7()),
        "Growth Monthly",
        PlanTier.Growth,
        new Dictionary<string, bool> { ["classroom.recording"] = true },
        new Dictionary<string, long> { ["limits.max_users"] = 500 },
        199m,
        BillingCycle.Monthly,
        "USD",
        Clock,
        Actor);

    [Fact]
    public void Create_IsActive_WithFeaturesAndLimits()
    {
        var plan = NewPlan();

        plan.IsActive.Should().BeTrue();
        plan.Tier.Should().Be(PlanTier.Growth);
        plan.Features["classroom.recording"].Should().BeTrue();
        plan.Limits["limits.max_users"].Should().Be(500);
    }

    [Fact]
    public void Update_ReplacesDefinition()
    {
        var plan = NewPlan();

        var result = plan.Update(
            "Growth Annual",
            new Dictionary<string, bool> { ["classroom.recording"] = false },
            new Dictionary<string, long> { ["limits.max_users"] = 1000 },
            1990m,
            BillingCycle.Annual,
            "USD",
            Clock,
            Actor);

        result.IsSuccess.Should().BeTrue();
        plan.Name.Should().Be("Growth Annual");
        plan.BillingCycle.Should().Be(BillingCycle.Annual);
        plan.Features["classroom.recording"].Should().BeFalse();
        plan.Limits["limits.max_users"].Should().Be(1000);
    }

    [Fact]
    public void Deactivate_Twice_Fails()
    {
        var plan = NewPlan();

        plan.Deactivate(Clock, Actor).IsSuccess.Should().BeTrue();
        plan.IsActive.Should().BeFalse();

        var second = plan.Deactivate(Clock, Actor);
        second.IsFailure.Should().BeTrue();
        second.Error!.Code.Should().Be("business_rule_violation");
    }
}
