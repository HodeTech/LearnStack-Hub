using FluentAssertions;
using LearnStack.Hub.Modules.Subscriptions.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Time;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.Modules;

public sealed class HubSubscriptionTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero));
    private static readonly OperatorId Actor = HubSystemActors.SystemOperator;
    private static readonly Guid PlanId = Guid.CreateVersion7();

    private static HubSubscription NewTrial() => HubSubscription.StartTrial(
        HubSubscriptionId.From(Guid.CreateVersion7()),
        LearnStackTenantId.From(Guid.CreateVersion7()),
        PlanId,
        Clock.UtcNow,
        Clock.UtcNow.AddDays(14),
        Clock,
        Actor);

    [Fact]
    public void StartTrial_SetsTrialStateAndPeriod()
    {
        var sub = NewTrial();

        sub.Status.Should().Be(SubscriptionStatus.Trial);
        sub.CurrentPeriodEnd.Should().Be(Clock.UtcNow.AddDays(14));
    }

    [Fact]
    public void Activate_FromTrial_Succeeds()
    {
        var sub = NewTrial();

        var result = sub.Activate(Clock.UtcNow, Clock.UtcNow.AddMonths(1), Clock, Actor);

        result.IsSuccess.Should().BeTrue();
        sub.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void ChangePlan_FromTrialOrActive_Rebinds()
    {
        var sub = NewTrial();

        // Allowed during Trial (switching the selected plan before activation).
        var trialPlan = Guid.CreateVersion7();
        sub.ChangePlan(trialPlan, Clock.UtcNow, Clock.UtcNow.AddMonths(1), Clock, Actor).IsSuccess.Should().BeTrue();
        sub.PlanId.Should().Be(trialPlan);

        // And during Active (an upgrade/downgrade).
        sub.Activate(Clock.UtcNow, Clock.UtcNow.AddMonths(1), Clock, Actor);
        var activePlan = Guid.CreateVersion7();
        sub.ChangePlan(activePlan, Clock.UtcNow, Clock.UtcNow.AddMonths(1), Clock, Actor).IsSuccess.Should().BeTrue();
        sub.PlanId.Should().Be(activePlan);
    }

    [Fact]
    public void ChangePlan_FromTerminalState_Fails()
    {
        var sub = NewTrial();
        sub.Expire(Clock, Actor);

        sub.ChangePlan(Guid.CreateVersion7(), Clock.UtcNow, Clock.UtcNow.AddMonths(1), Clock, Actor)
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_AtPeriodEnd_FlagsButKeepsActive()
    {
        var sub = NewTrial();
        sub.Activate(Clock.UtcNow, Clock.UtcNow.AddMonths(1), Clock, Actor);

        sub.Cancel(atPeriodEnd: true, Clock, Actor).IsSuccess.Should().BeTrue();

        sub.CancelAtPeriodEnd.Should().BeTrue();
        sub.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Expire_FromTrial_Succeeds()
    {
        var sub = NewTrial();

        sub.Expire(Clock, Actor).IsSuccess.Should().BeTrue();
        sub.Status.Should().Be(SubscriptionStatus.Expired);
    }
}
