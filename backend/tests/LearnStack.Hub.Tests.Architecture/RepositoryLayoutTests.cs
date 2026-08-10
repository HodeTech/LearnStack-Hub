using FluentAssertions;
using Xunit;

namespace LearnStack.Hub.Tests.Architecture;

/// <summary>
/// File-system rules that survive any reflection-based check.
/// These walk the working copy at test time, so no compiled assembly is needed.
/// </summary>
public sealed class RepositoryLayoutTests
{
    private static readonly string[] AllowedFrontendApps = ["operator-portal"];

    /// <summary>
    /// ADR-0018 (LearnStack): domain-specific shapes live as tenant customization
    /// data, not code. A `Verticals/` source folder at any level under
    /// `backend/src` is forbidden. Hub inherits this rule by reference — Hub
    /// must not ship vertical-specific shapes either (no "EnglishPlan",
    /// "YogaPlan", etc.; Hub plans are domain-agnostic).
    /// </summary>
    [Fact]
    public void No_Source_Folder_Named_Verticals()
    {
        var srcRoot = RepositoryPaths.BackendSrc();

        var offenders = Directory
            .EnumerateDirectories(srcRoot, "Verticals", SearchOption.AllDirectories)
            .ToArray();

        offenders.Should().BeEmpty(
            "ADR-0018 (LearnStack core) applies to Hub by reference: tenant-specific shapes " +
            "live as data, not code. See https://github.com/HodeTech/LearnStack/blob/main/"
            + "docs/decisions/0018-tenant-driven-customization-model.md.");
    }

    /// <summary>
    /// P02c-0 ships exactly one frontend app (`operator-portal`). Adding a peer
    /// app without a Hub-internal ADR or a Phase 02c packet decision is a
    /// structural deviation.
    /// </summary>
    [Fact]
    public void Frontend_Has_Only_The_OperatorPortal_App()
    {
        var appsRoot = RepositoryPaths.FrontendApps();

        Directory.Exists(appsRoot).Should().BeTrue(
            $"`{appsRoot}` must exist — P02c-0 ships the frontend monorepo with `apps/operator-portal`. " +
            "If you intentionally removed it, update this test and the Hub roadmap together.");

        // Filter dotted directories (.tmp, .cache, .turbo, etc.).
        var appNames = Directory
            .EnumerateDirectories(appsRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Where(name => !name!.StartsWith('.'))
            .ToArray();

        appNames.Should().BeEquivalentTo(
            AllowedFrontendApps,
            "Hub keeps a single operator-facing frontend (operator-portal). " +
            "A second app (e.g. a public marketing site, an analytics dashboard) " +
            "requires a Hub-internal ADR.");
    }
}
