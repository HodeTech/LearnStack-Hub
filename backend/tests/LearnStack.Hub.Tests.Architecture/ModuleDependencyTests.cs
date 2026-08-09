using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace LearnStack.Hub.Tests.Architecture;

/// <summary>
/// Mechanical dependency-direction checks for Hub modules. P02c-0 ships only
/// the meta-test that proves NetArchTest works; the per-module Domain →
/// Application/Infrastructure isolation rules become real once modules land in
/// P02c-1 (mirror of LearnStack core's ModuleDependencyTests).
/// </summary>
public sealed class ModuleDependencyTests
{
    /// <summary>
    /// Positive control: this test plants an explicit IL-level dependency
    /// on `LearnStack.Hub.Domain.AssemblyMarker` via the `_plantedDependency`
    /// field. NetArchTest (Mono.Cecil) walks type/member/method-body
    /// TypeRefs, NOT csproj `ProjectReference` entries — an unused project
    /// reference produces no IL TypeRef, so the planted field is the actual
    /// hook.
    ///
    /// If this meta-test ever passes (i.e. NetArchTest reports the dependency
    /// as absent), every other architecture test in this project is vacuously
    /// green and the suite is meaningless. Keep this meta-test in perpetuity.
    /// </summary>
    [Fact(DisplayName = "(meta) NetArchTest detects a planted forbidden dependency")]
    public void Meta_NetArchTest_DetectsAPlantedViolation()
    {
        // Reference the planted type at runtime too, so a build-time dead-code
        // elimination pass (should one ever apply) cannot strip the IL TypeRef.
        _ = _plantedDependency;

        var testAssembly = typeof(ModuleDependencyTests).Assembly;

        var result = Types.InAssembly(testAssembly)
            .Should()
            .NotHaveDependencyOn("LearnStack.Hub.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeFalse(
            "NetArchTest must detect the planted LearnStack.Hub.Domain.AssemblyMarker IL TypeRef " +
            "(see `_plantedDependency` on this class). A green result here means every other " +
            "architecture test in this project is vacuous and CI cannot be trusted.");
    }

    // Planted IL-level dependency for Meta_NetArchTest_DetectsAPlantedViolation.
    // DO NOT remove or replace with a string mention — only a real Type
    // reference produces the IL TypeRef NetArchTest scans.
    private static readonly Type _plantedDependency = typeof(LearnStack.Hub.Domain.AssemblyMarker);

    [Theory]
    [InlineData("TenantLifecycle")]
    [InlineData("Plans")]
    [InlineData("Subscriptions")]
    [InlineData("Entitlements")]
    public void ModuleDomain_DoesNotDependOn_OtherModuleDomain(string module)
    {
        var domain = HubAssemblies.ModuleDomains
            .Single(a => a.GetName().Name!.Contains($".{module}.", StringComparison.Ordinal));

        var forbidden = HubAssemblies.OtherModuleDomainNamespaces(module);

        var result = Types.InAssembly(domain)
            .Should()
            .NotHaveDependencyOnAll([.. forbidden])
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Domain must not depend on another module's Domain. Offenders: " +
            $"{string.Join(", ", result.FailingTypeNames ?? [])}. Cross-module communication goes " +
            "through Application.Contracts, not Domain (module-topology.md § Dependency direction).");
    }

    [Theory]
    [InlineData("TenantLifecycle")]
    [InlineData("Plans")]
    [InlineData("Subscriptions")]
    [InlineData("Entitlements")]
    public void ModuleDomain_DoesNotDependOn_ApplicationOrInfrastructure(string module)
    {
        var domain = HubAssemblies.ModuleDomains
            .Single(a => a.GetName().Name!.Contains($".{module}.", StringComparison.Ordinal));

        // NB: EF Core itself is NOT forbidden — the Vogen-emitted EfCoreValueConverter
        // nested in a module's strongly-typed id legitimately lives in Domain
        // (ADR-0023 / Standards 01 § Build-time-only exceptions). The rule bans the
        // application + DB-driver concerns: MediatR, FluentValidation, Npgsql.
        var result = Types.InAssembly(domain)
            .Should()
            .NotHaveDependencyOnAny(
                "MediatR",
                "FluentValidation",
                "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Domain must depend only on the SharedKernel (+ the Vogen EF converter) — no MediatR / " +
            $"FluentValidation / Npgsql. Offenders: {string.Join(", ", result.FailingTypeNames ?? [])}.");
    }

    /// <summary>
    /// Every aggregate root inherits Entity&lt;TId&gt; / AuditableEntity&lt;TId&gt; over a
    /// Vogen strongly-typed id (ADR-0023) — no aggregate keyed on a raw Guid.
    /// </summary>
    [Fact]
    public void Aggregate_Roots_Use_StronglyTypedId()
    {
        Type[] aggregates =
        [
            typeof(LearnStack.Hub.Modules.TenantLifecycle.Domain.LearnStackTenant),
            typeof(LearnStack.Hub.Modules.Plans.Domain.Plan),
            typeof(LearnStack.Hub.Modules.Subscriptions.Domain.HubSubscription),
            typeof(LearnStack.Hub.Modules.Entitlements.Domain.Entitlement),
        ];

        foreach (var aggregate in aggregates)
        {
            var entityBase = WalkToEntityBase(aggregate);
            entityBase.Should().NotBeNull($"{aggregate.Name} must inherit Entity<TId> / AuditableEntity<TId>.");

            var idType = entityBase!.GetGenericArguments()[0];
            typeof(LearnStack.Hub.SharedKernel.Identifiers.IStronglyTypedId<Guid>)
                .IsAssignableFrom(idType)
                .Should().BeTrue($"{aggregate.Name}'s id type {idType.Name} must be a Vogen IStronglyTypedId<Guid>.");
        }
    }

    private static Type? WalkToEntityBase(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(LearnStack.Hub.SharedKernel.Domain.Entity<>))
            {
                return current;
            }
        }

        return null;
    }
}
