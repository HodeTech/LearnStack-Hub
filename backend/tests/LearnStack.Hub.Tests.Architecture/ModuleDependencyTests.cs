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

    // TODO(2026-05-21, @platform, phase-02c-1): Once modules land in
    // src/Modules/<X>/, extend with the Hub equivalent of LearnStack core's
    // module-isolation rules:
    //   - ModuleDomain_DoesNotDependOn_OtherModuleDomain
    //   - ModuleDomain_DoesNotDependOn_AnyApplicationOrInfrastructure
    //   - Hub_Modules_DoNotReference_LearnStack_Internals
}
