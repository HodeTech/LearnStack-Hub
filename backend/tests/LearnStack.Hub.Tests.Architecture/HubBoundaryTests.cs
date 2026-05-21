using FluentAssertions;
using Xunit;

namespace LearnStack.Hub.Tests.Architecture;

/// <summary>
/// Hub-specific boundary rules. The authoritative list of Hub-side architecture
/// tests is in `../learnstack/docs/architecture/24-learnstack-hub.md` § 10
/// (six tests):
///
///   1. `Hub_NeverStores_TenantContent` (Architecture 24 spelling; ADR-0019
///      § Architecture tests calls the same rule `Hub_NeverStores_TenantData`)
///      — see <see cref="Hub_NeverStores_TenantData"/> below. This repo uses
///      the ADR-0019 name because the ADR is the Accepted decision; the
///      drift is a corpus-side reconciliation belonging to LearnStack core.
///   2. `Hub_Modules_DoNotReference_LearnStack_Internals` — lands in P02c-1
///      once Hub modules exist.
///   3. `Internal_API_Endpoints_AreNot_Public` — integration test; lands in
///      P02c-2 with the Hub-side `/api/v1/internal/license/verify` handler.
///   4. `Stripe_SDK_Types_NotImportedOutsideInfrastructure` — lands in
///      Phase 09b when Stripe integration arrives.
///   5. `Iyzico_SDK_Types_NotImportedOutsideInfrastructure` — lands in
///      Phase 09b when Iyzico integration arrives.
///   6. `Hub_Operator_JWT_NeverAccepted_On_LearnStack_Routes` — integration
///      test; lands in P02c-3 LearnStack-side PR (the test lives on the
///      LearnStack tenant-facing surface, not in this repo's test project).
///
/// Plus from related ADRs (LearnStack-side, lands via P02c-3 / P02c-5 / P02c-6):
///   - `IEntitlementProvider_Implementations_Are_Three` (ADR-0020) — P02c-3.
///   - `NullEntitlementProvider_NotRegistered_OutsideDevelopment` (ADR-0020)
///     — P02c-3.
///   - `LicenseKey_Validation_Is_Pinned_RSA2048` (ADR-0020) — P02c-6.
///   - `Cert_PrivateKey_NeverLeavesVault_To_Logs` (ADR-0022) — P02c-5.
///   - `CustomDomain_TenantId_NeverReadFrom_RequestBody` (ADR-0022) — P02c-5.
///
/// KNOWN CORPUS DRIFT (not a Hub bug; flagged for the LearnStack-side
/// reconciliation packet): ADR-0019 § Architecture tests lists only four
/// rules and uses the name `Hub_NeverStores_TenantData`; Architecture 24
/// § 10 lists six rules and uses `Hub_NeverStores_TenantContent`. Hub
/// follows ADR-0019's name (the Accepted decision is authority); P02c-3
/// or a follow-up amendment to ADR-0019 should fold all six tests into the
/// ADR's § Architecture tests section so the two docs agree.
/// </summary>
public sealed class HubBoundaryTests
{
    /// <summary>
    /// Hub must never store tenant content.
    ///
    /// Forbidden type-name fragments: `Course`, `Lesson`, `Enrollment`,
    /// `LiveSession`, `LessonItem`, `MediaAsset`. Matched substring-style
    /// against full type names so a type like `LessonProgress` triggers on
    /// `Lesson` — that is intentional; Hub MUST NOT contain any of these.
    ///
    /// Why `User` is NOT in the forbidden list (and intentionally never will
    /// be): Hub has operator users in the Operators module (`Operator`,
    /// `OperatorRole`, etc.). Banning the substring `User` would collide with
    /// every legitimate operator-side type. The tenant-vs-operator User
    /// distinction lives in CLAUDE.md prose + reviewer discipline + the
    /// Operators module's permission model — not in this scanner. A
    /// future-proofing alternative (only scan `LearnStack.Hub.Modules.*`
    /// once those exist) is queued under the TODO below.
    ///
    /// P02c-0 has zero domain source files to scan, so this test is vacuously
    /// green today. Once Hub modules land in P02c-1, the scan walks every type
    /// name in the Hub assemblies and asserts none of the forbidden fragments
    /// appear.
    /// </summary>
    // TODO(2026-05-21, @platform, phase-02c-1): once `LearnStack.Hub.Modules.*`
    // assemblies exist, narrow the scan to those (and `Infrastructure.Audit`
    // since it can hold tenant references). Today the scan walks ALL six core
    // assemblies including test fixtures' transitive types — broader than
    // strictly necessary. Narrowing reduces false-positive surface and makes
    // intent clearer. Keep `User` excluded from the forbidden list per the
    // operator-user carve-out documented above.
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

        // Forbidden type-name fragments. Substring-matched against full type
        // names. See the summary above for the User-carve-out rationale.
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
            "Note: tenant `User` is NOT in the forbidden list because Hub has operator users " +
            "(in the Operators module); the tenant-vs-operator User distinction is enforced " +
            "via CLAUDE.md + reviewer discipline + the Operators module's permission model, " +
            "not via this scanner. See ADR-0019 § Hub data model.");
    }
}
