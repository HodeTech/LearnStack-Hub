---
name: add-architecture-test
description: >
  Encode a Hub structural rule as a non-skippable test in
  LearnStack.Hub.Tests.Architecture (NetArchTest / xUnit). USE FOR: turning a written
  rule (no-RLS, no-tenant-content, dependency direction, naming, marker presence) into
  a mechanical check; backing a Hub-internal ADR with the rule it implies; extending a
  placeholder test (e.g. Hub_NeverStores_TenantData) to scan a new module. DO NOT USE
  FOR: business-logic tests (Unit), runtime behaviour checks (Integration), or a "nice
  to enforce" rule with no doc/ADR backing — architecture tests carry weight; land them
  only after the rule is in the corpus.
---

# Adding a Hub architecture test

## Purpose

Make a structural rule mechanical + non-skippable. Hub's architecture suite is the backstop for the Hub deltas (no RLS, no tenant content, dependency direction) and the boundary invariants. Mirrors LearnStack's `add-architecture-test`.

## When to use

- A written rule (CLAUDE.md hard rule, a doc invariant, a `HUB-NNNN` ADR) needs mechanical enforcement.
- A placeholder test must become real (e.g. `Hub_NeverStores_TenantData` scanning a newly-landed module).

## When not to use

- Business logic → `LearnStack.Hub.Tests.Unit`.
- Runtime behaviour → `LearnStack.Hub.Tests.Integration`.
- A rule with no doc/ADR backing → write the rule first ([write-adr](../write-adr/SKILL.md)).

## The Hub architecture-test set

The tests `LearnStack.Hub.Tests.Architecture` should carry as P02c-1 lands modules (authoritative list: [Architecture 24 § 10](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md), Hub-side):

| Test                                                                | Asserts                                                                                                                                                                                                                                                       |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Meta_NetArchTest_DetectsAPlantedViolation`                         | The scanner works (planted IL TypeRef detected). Keep in perpetuity.                                                                                                                                                                                          |
| `No_Source_Folder_Named_Verticals`                                  | ADR-0018 mirror — no domain-folder.                                                                                                                                                                                                                           |
| `Frontend_Has_Only_The_OperatorPortal_App`                          | Single operator-portal app.                                                                                                                                                                                                                                   |
| `Hub_NeverStores_TenantData`                                        | No `Course`/`Lesson`/`Enrollment`/`LiveSession`/`LessonItem`/`MediaAsset` type fragments in Hub assemblies (NOT `User` — operator-user carve-out). Scans the real module assemblies once they land.                                                           |
| `Hub_Modules_DoNotReference_LearnStack_Internals`                   | No `LearnStack.SharedKernel`/`Domain`/`Infrastructure`/`Modules.*` (sibling-repo) refs.                                                                                                                                                                       |
| `ModuleDomain_DoesNotDependOn_OtherModuleDomain`                    | Cross-module Domain isolation.                                                                                                                                                                                                                                |
| `ModuleDomain_DoesNotDependOn_AnyApplicationOrInfrastructure`       | Domain references only SharedKernel.                                                                                                                                                                                                                          |
| `Aggregate_Roots_Use_StronglyTypedId`                               | Every `IAggregateRoot<TId>` uses a Vogen `[ValueObject<Guid>]` id.                                                                                                                                                                                            |
| `Hub_Has_No_RowLevelSecurity` _(recommended)_                       | No EF `HasQueryFilter` for tenant isolation; no `[TenantOwned]`/`[OrganizationScoped]` markers exist; (optionally) a migration scan asserts no `ENABLE ROW LEVEL SECURITY` / `CREATE POLICY` in Hub migrations. This is the test that encodes Hub's #1 delta. |
| `MediatR_Pipeline_Order_Matches_Canonical_Sequence` _(recommended)_ | The 6-step Hub order, no `TenantContextBehavior`.                                                                                                                                                                                                             |

Integration-test-shaped boundary rules (`Internal_API_Endpoints_AreNot_Public`, `Hub_Operator_JWT_NeverAccepted_On_LearnStack_Routes`) land in P02c-2/P02c-3 as integration tests, not NetArchTest.

## Workflow

### Step 1 — Confirm the rule exists in the corpus

The rule must be written somewhere (CLAUDE.md, a Hub doc, a `HUB-NNNN` ADR, or the LearnStack-side ADR Hub inherits). If not, write it first — architecture tests enforce decisions, they don't invent them.

### Step 2 — Write the test

- Reflection / dependency rules → NetArchTest (`Types.InAssembly(...).Should().NotHaveDependencyOn(...)`), mirroring `ModuleDependencyTests`. Reference the module assemblies via `typeof(<AssemblyMarker>).Assembly`.
- Type-name / marker scans → reflect over `assembly.GetTypes()`.
- Filesystem rules → `RepositoryPaths` walk (mirror `RepositoryLayoutTests`).
- Give each test a `[Fact(DisplayName=…)]` or a clear name; cite the rule's source doc/ADR in a comment.

### Step 3 — Prove it bites

A test that's vacuously green is worse than none. Either rely on the meta-test (for the scanner) or temporarily plant a violation to confirm the test fails, then remove it.

### Step 4 — Wire new module assemblies

When a module lands, add its assemblies to the architecture-test project references + the per-module test data so `Hub_NeverStores_TenantData` + the dependency tests scan it.

## Validation

- Test in `LearnStack.Hub.Tests.Architecture`; green for compliant code; demonstrably fails on a planted violation.
- The rule it enforces is in the corpus (doc/ADR cited).
- New modules are scanned (assembly references added).

## Common pitfalls

- **A vacuously-green test.** Prove it can fail. The meta-test exists precisely because an unreferenced assembly produces no IL TypeRef.
- **Enforcing an uncodified rule.** Write the rule first.
- **Forgetting to add a new module's assemblies.** The scan silently skips what it can't see.
