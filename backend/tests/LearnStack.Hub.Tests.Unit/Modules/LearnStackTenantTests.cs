using FluentAssertions;
using LearnStack.Hub.Modules.TenantLifecycle.Domain;
using LearnStack.Hub.SharedKernel.Hosting;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Time;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.Modules;

public sealed class LearnStackTenantTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero));
    private static readonly OperatorId Actor = HubSystemActors.SystemOperator;

    private static LearnStackTenant NewTenant() => LearnStackTenant.Create(
        LearnStackTenantId.From(Guid.CreateVersion7()),
        "acme",
        "Acme Inc",
        DeploymentMode.SaaS,
        Clock,
        Actor);

    [Fact]
    public void Create_StartsInTrial()
    {
        var tenant = NewTenant();

        tenant.Status.Should().Be(TenantStatus.Trial);
        tenant.CreatedAt.Should().Be(Clock.UtcNow);
    }

    [Fact]
    public void Activate_FromTrial_Succeeds()
    {
        var tenant = NewTenant();

        var result = tenant.Activate(Clock, Actor);

        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Suspend_FromActive_Succeeds_ThenReactivate()
    {
        var tenant = NewTenant();
        tenant.Activate(Clock, Actor);

        tenant.Suspend("non-payment", Clock, Actor).IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Suspended);

        tenant.Activate(Clock, Actor).IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Suspend_FromTrial_Fails()
    {
        var tenant = NewTenant();

        var result = tenant.Suspend("reason", Clock, Actor);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("business_rule_violation");
        tenant.Status.Should().Be(TenantStatus.Trial);
    }

    [Fact]
    public void Terminate_RequiresArchived()
    {
        var tenant = NewTenant();
        tenant.Activate(Clock, Actor);

        tenant.Terminate(Clock, Actor).IsFailure.Should().BeTrue();

        tenant.Archive(Clock, Actor).IsSuccess.Should().BeTrue();
        tenant.Terminate(Clock, Actor).IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Terminated);
    }
}
