using FluentAssertions;
using Xunit;

namespace LearnStack.Hub.Tests.Architecture;

/// <summary>
/// Hub-specific boundary rules. Per ADR-0019 § Architecture tests, four
/// Hub-side rules ship in Phase 02c:
///
///   1. <see cref="Hub_NeverStores_TenantData"/> — no tenant-content table
///      types in Hub assemblies (this file).
///   2. `Hub_Modules_DoNotReference_LearnStack_Internals` — lands in P02c-1
///      once Hub modules exist.
///   3. `Internal_API_Endpoints_AreNot_Public` — integration test, lands in
///      P02c-2 with the Hub-side `/api/v1/internal/license/verify` handler.
///   4. `Hub_Operator_JWT_NeverAccepted_On_LearnStack_Routes` — integration
///      test, lands in P02c-3 LearnStack-side PR.
///
/// Plus from related ADRs:
///   - `IEntitlementProvider_Implementations_Are_Three` (ADR-0020) — lands in
///     P02c-3 LearnStack-side.
///   - `NullEntitlementProvider_NotRegistered_OutsideDevelopment` (ADR-0020)
///     — lands in P02c-3 LearnStack-side.
///   - `LicenseKey_Validation_Is_Pinned_RSA2048` (ADR-0020) — lands in P02c-6.
///   - `Cert_PrivateKey_NeverLeavesVault_To_Logs` (ADR-0022) — lands in P02c-5.
///   - `CustomDomain_TenantId_NeverReadFrom_RequestBody` (ADR-0022) — lands in
///     P02c-5.
/// </summary>
public sealed class HubBoundaryTests
{
    /// <summary>
    /// Hub must never store tenant content. Forbidden type-name fragments:
    /// `Course`, `Lesson`, `Enrollment`, `LiveSession`, `LessonItem`,
    /// `MediaAsset`. (Tenant `User` is separate from operator users; operator
    /// users live in the Operators module and are NOT considered tenant data.)
    ///
    /// P02c-0 has zero source files to scan, so this test is vacuously green.
    /// Once Hub modules land in P02c-1, the scan walks every type name in the
    /// Hub assemblies and asserts none of the forbidden fragments appear.
    /// </summary>
    [Fact]
    public void Hub_NeverStores_TenantData()
    {
        var hubAssemblies = new[]
        {
            typeof(LearnStack.Hub.SharedKernel.AssemblyMarker).Assembly,
            typeof(LearnStack.Hub.Domain.AssemblyMarker).Assembly,
            typeof(LearnStack.Hub.Application.Contracts.AssemblyMarker).Assembly,
            typeof(LearnStack.Hub.Application.AssemblyMarker).Assembly,
            typeof(LearnStack.Hub.Infrastructure.AssemblyMarker).Assembly,
            typeof(LearnStack.Hub.Infrastructure.Audit.AssemblyMarker).Assembly,
        };

        // Forbidden type-name fragments. Matched against full type names so a
        // type like `LessonProgress` would trigger on "Lesson" — that's
        // intentional; Hub MUST NOT contain any of these.
        var forbiddenFragments = new[]
        {
            "Course",
            "Lesson",
            "Enrollment",
            "LiveSession",
            "LessonItem",
            "MediaAsset",
        };

        var offenders = hubAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => forbiddenFragments.Any(fragment =>
                t.FullName?.Contains(fragment, StringComparison.Ordinal) == true))
            .Select(t => t.FullName)
            .ToArray();

        offenders.Should().BeEmpty(
            "Hub must never store tenant content. The forbidden type-name fragments " +
            $"({string.Join(", ", forbiddenFragments)}) belong exclusively to LearnStack core. " +
            "If you need to reference one of these concepts from Hub, model it as metadata " +
            "(e.g. `UsageMetric` for course-completion counts) — not as a content table. " +
            "See ADR-0019 § Hub data model.");
    }
}
