using FluentAssertions;
using LearnStack.Hub.Modules.Entitlements.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;
using LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;
using Xunit;

namespace LearnStack.Hub.Tests.Integration;

/// <summary>
/// End-to-end of the primary P02c-1 flow against a real Postgres: create tenant
/// → trial subscription → entitlement (generation 1); change plan → recompute
/// (generation 2). Exercises the 6-step pipeline, the shared-connection
/// cross-module transaction, and the projection service.
/// </summary>
[Collection(HubApiCollection.Name)]
public sealed class EntitlementFlowTests(HubApiFixture fixture)
{
    private static IReadOnlyDictionary<string, bool> GrowthFeatures => new Dictionary<string, bool>
    {
        ["classroom.recording"] = true,
        ["tenancy.custom_domain"] = true,
        ["identity.sso.saml"] = false,
    };

    private static IReadOnlyDictionary<string, long> GrowthLimits => new Dictionary<string, long>
    {
        ["limits.max_users"] = 500,
        ["limits.max_organizations"] = 10,
    };

    [Fact]
    public async Task CreateTenant_Then_ChangePlan_RecomputesEntitlement_WithMonotonicGeneration()
    {
        // A unique slug per run keeps the shared container reusable across tests.
        var slug = $"acme-{Guid.NewGuid():N}".ToLowerInvariant()[..20];

        var growth = await CreatePlan("Growth Monthly", "growth", GrowthFeatures, GrowthLimits);
        var scale = await CreatePlan(
            "Scale Monthly",
            "scale",
            new Dictionary<string, bool> { ["classroom.recording"] = true, ["analytics.advanced_reporting"] = true },
            new Dictionary<string, long> { ["limits.max_users"] = 5000 });

        // Create tenant → trial subscription → initial entitlement (generation 1).
        var created = await fixture.SendAsync(new CreateTenantCommand(slug, "Acme Inc", "SaaS", growth.Id));
        created.IsSuccess.Should().BeTrue(created.Error?.Code);
        var tenantId = created.Value!.Id;

        var afterCreate = await fixture.SendAsync(new GetEntitlementQuery(tenantId));
        afterCreate.IsSuccess.Should().BeTrue(afterCreate.Error?.Code);
        var gen1 = afterCreate.Value!;
        gen1.Generation.Should().Be(1);
        gen1.Tier.Should().Be("growth");
        gen1.Features.Should().ContainKey("classroom.recording").WhoseValue.Should().BeTrue();
        gen1.Limits["limits.max_users"].Should().Be(500);
        gen1.Compliance.Caps.Should().BeEmpty();
        gen1.ExpiresAt.Should().NotBeNull();

        // Change plan → recompute (generation 2, new tier).
        var changed = await fixture.SendAsync(new ChangePlanCommand(tenantId, scale.Id));
        changed.IsSuccess.Should().BeTrue(changed.Error?.Code);

        var afterChange = await fixture.SendAsync(new GetEntitlementQuery(tenantId));
        afterChange.IsSuccess.Should().BeTrue();
        var gen2 = afterChange.Value!;
        gen2.Generation.Should().Be(2);
        gen2.Tier.Should().Be("scale");
        gen2.Limits["limits.max_users"].Should().Be(5000);
    }

    [Fact]
    public async Task GetSubscriptionsByPlan_ReturnsBoundTenant()
    {
        var slug = $"beta-{Guid.NewGuid():N}".ToLowerInvariant()[..20];
        var plan = await CreatePlan("Starter Monthly", "starter", GrowthFeatures, GrowthLimits);

        var created = await fixture.SendAsync(new CreateTenantCommand(slug, "Beta LLC", "SaaS", plan.Id));
        created.IsSuccess.Should().BeTrue(created.Error?.Code);

        var bound = await fixture.SendAsync(new GetSubscriptionsByPlanQuery(plan.Id));
        bound.IsSuccess.Should().BeTrue();
        bound.Value!.Should().Contain(created.Value!.Id);
    }

    private async Task<PlanDto> CreatePlan(
        string name,
        string tier,
        IReadOnlyDictionary<string, bool> features,
        IReadOnlyDictionary<string, long> limits)
    {
        var result = await fixture.SendAsync(
            new CreatePlanCommand(name, tier, features, limits, 199m, "monthly", "USD"));
        result.IsSuccess.Should().BeTrue(result.Error?.Code);
        return result.Value!;
    }
}
