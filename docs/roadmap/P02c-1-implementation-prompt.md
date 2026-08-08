# P02c-1 Implementation Prompt — Hub Domain Core

> **Purpose.** A copy-paste kickoff prompt for the agent that implements Phase 02c
> Packet 1 (Hub Domain Core). Hand this whole file to the agent at session start.
>
> **The agent runs from `learnstack-hub` root.** Sibling LearnStack core repo is at `../LearnStack/`.
>
> **Use the project's own workflow skills.** This repo carries a Hub-tailored
> `.claude/skills/` catalogue (see [`.claude/skills/README.md`](../../.claude/skills/README.md)).
> The agent's entry point is [`implement-task`](../../.claude/skills/implement-task/SKILL.md),
> which dispatches [`start-task`](../../.claude/skills/start-task/SKILL.md) → the
> `wire-cross-cutting-foundation` / `add-hub-module` / `add-hub-aggregate` /
> `add-mediatr-handler` / `add-ef-migration` / `add-architecture-test` /
> `add-integration-test` skills → `run-tests-locally` → `commit-and-pr`. **The
> skills carry the mechanical workflow.** This file carries the P02c-1-specific
> context the skills don't (and shouldn't) duplicate.

---

## 0. Environment (read first — these will bite otherwise)

- **.NET 10 SDK is NOT on the default PATH.** The system `dotnet` (`/usr/local/bin/dotnet`) is .NET 9; the project pins `10.0.100` in `backend/global.json`. The real SDK is at `~/.dotnet/dotnet` (10.0.101, rolls forward from the pin). Run `export PATH="$HOME/.dotnet:$PATH"` once at the start of each shell, or prefix every command.
- **Sibling layout is assumed.** Cross-repo references use `../LearnStack/...`. If `../LearnStack/` is missing, stop and report — you cannot mirror the SharedKernel patterns without it.
- **Pre-commit hook.** `make install` activates `.githooks/pre-commit` (Leakwatch repo-root scan + `dotnet format` + prettier + ESLint). Backend-only commits don't trip the ESLint step. `.claude/*` is gitignored except `.claude/skills/`, so runtime lock files don't corrupt staged content.
- **Branch.** Work on `feat/phase-02c-packet-1-hub-domain-core`. **Do not touch the `../LearnStack` repo** — another agent may be active there. P02c-1 is entirely Hub-side; LearnStack-side coordination is P02c-3.

## 1. What P02c-1 delivers

The Hub repo currently (commit `784a5ca`) has empty scaffold projects (AssemblyMarker only). P02c-1 fills the foundation + the first four domain modules:

1. **Hub SharedKernel** — mirror of LearnStack `LearnStack.SharedKernel` (Phase 02a Packet 2), adjusted for Hub (`OperatorId` not `UserId`, `HubException` not `LearnStackException`).
2. **Cross-cutting foundation** — mirror of LearnStack Phase 02a Packet 3, with Hub's **6-step** MediatR pipeline (not 8 — no `TenantContextBehavior`).
3. **Four domain modules** — `TenantLifecycle`, `Plans`, `Subscriptions`, `Entitlements`. Each: four projects, aggregate, DbContext, EF config, MediatR handlers, validators.
4. **EF migrations** — one per module DbContext, in the `hub` schema of the `learnstack_hub` database.
5. **The entitlement projection** — `EntitlementProjectionService` rebuilds `Entitlement` from `Plan` + `HubSubscription`, with the monotonic `generation` counter.
6. **Architecture + unit + integration tests** — Hub-side architecture tests become real; activate the `backend-integration` CI job with the first Testcontainers test.
7. **Plan seed data** — the four illustrative tiers (Starter / Growth / Scale / Enterprise) as data.

**Explicitly NOT in P02c-1** (see §9): the four HTTPS contract endpoints, the outbound `LearnStackApiClient`, the `learnstack.hub.entitlement` Dapr publish, the operator portal, custom domains, compliance policies, license keys, Stripe/Iyzico.

## 2. Required reading (in this order)

### Hub-side design specs (the contract you implement against)

1. [`docs/architecture/module-topology.md`](../architecture/module-topology.md) — the four modules, dependency rules, **the "Hub does NOT use RLS" model**, `hub` schema + `learnstack_hub` database.
2. [`docs/architecture/cross-cutting-foundation.md`](../architecture/cross-cutting-foundation.md) — SharedKernel surface, the **6-step pipeline**, **`OperatorId` not `UserId`**, `HubException`, composition root.
3. [`docs/architecture/entitlement-projection.md`](../architecture/entitlement-projection.md) — projection wire-shape, `generation` algorithm, recompute triggers, key-shape rules.
4. [`docs/modules/tenant-lifecycle.md`](../modules/tenant-lifecycle.md), [`plans.md`](../modules/plans.md), [`subscriptions.md`](../modules/subscriptions.md), [`entitlements.md`](../modules/entitlements.md) — per-aggregate field lists, state machines, commands/queries, DbContext shapes, audit matrices.
5. [`CLAUDE.md`](../../CLAUDE.md) and [`.claude/skills/README.md § The Hub deltas`](../../.claude/skills/README.md) — the five non-negotiable Hub deltas.

### LearnStack-side authority (the cross-cutting decisions — do not contradict)

6. `../LearnStack/docs/decisions/0019-learnstack-hub.md` — Hub data model, boundary.
7. `../LearnStack/docs/decisions/0021-feature-based-entitlement.md` (incl. Amendment 1) — `FeatureKey`/`LimitKey`, projection shape.
8. `../LearnStack/docs/decisions/0020-triple-deployment-hybrid-license.md` — `DeploymentMode`, entitlement consumption.
9. `../LearnStack/docs/decisions/0023-strongly-typed-id-source-generator.md` — Vogen pattern, `IdMask`.
10. `../LearnStack/docs/decisions/0032-exception-handling-logging-and-observability.md` — the cross-cutting architecture you mirror.
11. `../LearnStack/docs/architecture/24-learnstack-hub.md` — the full Hub ERD + projection + module list.
12. `../LearnStack/docs/standards/02-backend-coding.md` + `05-database.md` — coding + DB conventions. **Ignore Standards 05's RLS / tenant-isolation rows** — they don't apply to Hub.

### LearnStack-side CODE to mirror verbatim

13. `../LearnStack/backend/src/LearnStack.SharedKernel/` — the **canonical source** for every type to reproduce in `backend/src/Core/LearnStack.Hub.SharedKernel/` with the `LearnStack.Hub.SharedKernel.*` namespace and the `OperatorId`-for-`UserId` substitution.
14. `../LearnStack/backend/src/LearnStack.Api/Common/` — `LearnStackExceptionHandler`, `ResultExtensions`, `ProblemDetailsFactory`, `HttpStatusMap`. Mirror as `HubExceptionHandler` etc.
15. `../LearnStack/backend/src/LearnStack.Application/Pipeline/` — the MediatR behaviors. Mirror the 6 Hub keeps (drop `TenantContextBehavior`).
16. `../LearnStack/backend/src/Modules/<Module>/` — a representative four-project layout, aggregate shape, DbContext, handler.
17. `../LearnStack/backend/Directory.Packages.props` — current package versions.

## 3. dotnet tooling for migrations

EF migrations need `Microsoft.EntityFrameworkCore.Design` (reserved in `backend/Directory.Packages.props`) and the `dotnet-ef` tool:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet tool install --global dotnet-ef --version 10.* 2>/dev/null || dotnet tool update --global dotnet-ef --version 10.*
```

Each module's `Infrastructure` references `Microsoft.EntityFrameworkCore.Design`. Use:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/LearnStack.Hub.Modules.<Module>.Infrastructure \
  --startup-project src/Core/LearnStack.Hub.Api \
  --context <Module>DbContext \
  --output-dir Persistence/Migrations
```

See the [`add-ef-migration`](../../.claude/skills/add-ef-migration/SKILL.md) skill for naming + review conventions.

## 4. Build order

Build bottom-up so each layer compiles before the next depends on it. Commit at the boundaries (§7). The skills carry the mechanical workflow per step.

### Step A — Hub SharedKernel

→ [`wire-cross-cutting-foundation`](../../.claude/skills/wire-cross-cutting-foundation/SKILL.md) § Step 1.
Mirror `../LearnStack/backend/src/LearnStack.SharedKernel/` folder-for-folder with namespace `LearnStack.Hub.SharedKernel.*` and **two substitutions**: `OperatorId` for `UserId`; `HubException` for `LearnStackException`. Add `FeatureFlags/` with `FeatureKey`/`LimitKey` value objects + initial registries seeded from ADR-0021 Amendment 1 + Architecture 24 § 4. Unit-test the load-bearing types (Result, LocalizedMessage prefix invariant, Entity equality, FixedClock, AuditableEntity audit columns).

### Step B — Cross-cutting foundation

→ [`wire-cross-cutting-foundation`](../../.claude/skills/wire-cross-cutting-foundation/SKILL.md) § Steps 2-6.
`HubExceptionHandler`, `ResultExtensions.ToActionResult`, `ProblemDetailsFactory` (prefix `https://errors.hub.learnstack.dev/`), `HttpStatusMap`. The **6 behaviors** in canonical order — `Validation` (live), `Logging` (live; `ActivitySource("learnstack.hub.mediatr")`), `AuditLog` (shell), `Authorization` (shell), `Transaction` (live), `OutboxFlush` (shell). **No `TenantContextBehavior`.** Serilog + OTel (no LoggerProvider double-export, no tenant span processor). `IErrorTrackingProvider` (NoOp/Sentry/LocalFile) + `IProviderResilience<TPort>`. Composition root reads `DeploymentMode` **once**.

### Step C — Four modules (`backend/src/Modules/<Name>/`)

→ [`add-hub-module`](../../.claude/skills/add-hub-module/SKILL.md) per module + [`add-hub-aggregate`](../../.claude/skills/add-hub-aggregate/SKILL.md) per aggregate.
For each of `TenantLifecycle`, `Plans`, `Subscriptions`, `Entitlements` per the module deep dives. Aggregates use Vogen strongly-typed IDs, inherit `Entity<TId>` / `AuditableEntity<TId>`, return `Result` (never throw `DomainException` for business rules), raise domain events. Cross-module reads via `Application.Contracts` only. **No RLS, no `[TenantOwned]`, no tenant query filter** — Hub's #1 delta.

### Step D — DbContexts + migrations

→ [`add-ef-migration`](../../.claude/skills/add-ef-migration/SKILL.md).
Per-module DbContext, `HasDefaultSchema("hub")`, snake_case, JSONB for dictionary columns, Vogen ID converters. One migration per module; review the generated SQL has no `ENABLE ROW LEVEL SECURITY` / `CREATE POLICY` (if present, your DbContext config wrongly applied a tenant filter — fix the config, regenerate).

### Step E — Tests

→ [`add-architecture-test`](../../.claude/skills/add-architecture-test/SKILL.md) + [`add-integration-test`](../../.claude/skills/add-integration-test/SKILL.md).
**Architecture:** turn P02c-0 placeholders real (`Hub_NeverStores_TenantData` scans the new module assemblies); add per-module `ModuleDependencyTests`; add `Aggregate_Roots_Use_StronglyTypedId`; the recommended `Hub_Has_No_RowLevelSecurity` + `MediatR_Pipeline_Order_Matches_Canonical_Sequence` encode the load-bearing deltas. **Unit:** aggregate state-machine transitions, `Entitlement.Recompute` generation monotonicity, validators. **Integration** (Testcontainers): the entitlement-rebuild round trip (tenant create → trial subscription → recompute → assert generation 1 + projection fields; plan change → recompute → generation 2). **Activate `backend-integration` CI job** (flip `if: false` → real trigger in `.github/workflows/ci.yml`). **Contract:** `EntitlementProjection_Shape_IsStable` snapshot against `entitlement-v1.schema.json`. **Hub integration tests have NO tenant-isolation pair** — Hub has no RLS to isolate.

### Step F — Seed data

`scripts/seed.sh` (and/or EF seed): the four plan tiers from Architecture 24 § 8 (Starter / Growth / Scale / Enterprise) + demo tenant + trial subscription + entitlement. Plans are **data**, not code.

## 5. Hard rules (non-negotiable)

Every skill repeats these; they are listed here as the kickoff checklist.

- **No RLS, no tenant-context machinery.** No `ENABLE ROW LEVEL SECURITY`, no `*_tenant_isolation` policies, no EF global query filters, no `app.tenant_id` GUC, no `DbConnectionInterceptor` for session vars, no `TenantContextBehavior`.
- **`OperatorId`, never `UserId`.** No tenant `UserId` type anywhere in Hub.
- **6-step pipeline.** Validation → Logging → AuditLog → Authorization → Transaction → OutboxFlush → Handler.
- **No imports from LearnStack core.** Mirror by copying source. No `LearnStack.SharedKernel` / `LearnStack.Domain` / `LearnStack.*` references.
- **Hub stores no tenant content.** `Hub_NeverStores_TenantData` enforces.
- **`DomainException` is for programmer errors only.** Business-rule violations → `Result.Fail(...)`.
- **`generation` is monotonic** — starts at 1, only ever +1, never resets/decrements.
- **FeatureKey/LimitKey wire-strings match LearnStack core's registry exactly.**
- **`hub` schema, `learnstack_hub` database.** Every DbContext `HasDefaultSchema("hub")`.
- **One DbContext per module.** Cross-module FKs are plain `uuid` + index, not EF navigations.
- **English docs; Conventional Commits; AI co-author trailer** (`Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`).

## 6. Verification

→ [`run-tests-locally`](../../.claude/skills/run-tests-locally/SKILL.md) for the full command reference. Run continuously; all must pass before commit:

```bash
export PATH="$HOME/.dotnet:$PATH"
cd backend
dotnet build LearnStack.Hub.slnx --nologo                                                    # 0/0
dotnet test LearnStack.Hub.slnx --filter "FullyQualifiedName!~LearnStack.Hub.Tests.Integration" --nologo
dotnet format LearnStack.Hub.slnx --verify-no-changes --no-restore                           # exit 0
dotnet test tests/LearnStack.Hub.Tests.Integration/LearnStack.Hub.Tests.Integration.csproj --nologo   # Testcontainers
cd ..
leakwatch scan fs . --config .leakwatch.yaml --format table --min-severity medium --no-verify  # 0 findings
docker compose -f infra/compose/dev.yml config > /dev/null && echo OK
docker compose -f infra/compose/dev.yml -f infra/compose/e2e.yml config > /dev/null && echo OK
```

## 7. Suggested commit sequence (one branch, multiple commits)

→ [`commit-and-pr`](../../.claude/skills/commit-and-pr/SKILL.md) for the message format.
Land these as separate commits on `feat/phase-02c-packet-1-hub-domain-core`:

1. `feat(hub): P02c-1a — Hub SharedKernel (mirror of LearnStack P02a-2)` — Step A + unit tests.
2. `feat(hub): P02c-1b — cross-cutting foundation (6-step pipeline, exception handler, OTel)` — Step B.
3. `feat(hub): P02c-1c — TenantLifecycle + Plans aggregates + DbContexts + migrations` — half of C+D.
4. `feat(hub): P02c-1d — Subscriptions + Entitlements + projection service + migrations` — rest of C+D.
5. `feat(hub): P02c-1e — architecture + integration + contract tests; activate backend-integration CI` — Step E.
6. `feat(hub): P02c-1f — plan seed data + roadmap status update` — Step F + `docs/roadmap/README.md` (P02c-1 ✅, P02c-2 next).

Each commit must independently build + pass the non-integration tests. Push the branch and open a PR (`gh pr create`) when the full packet is green — or stop and report between sub-commits if you want review.

## 8. Known traps

- **dotnet PATH** (§0) — #1 time-sink. Fix it first.
- **Vogen IDs need a transitive EF Core reference** in the Domain project (the emitted converter requires it at compile time). Mirror how LearnStack's module Domain csproj handles it (`Directory.Build.props`-level or per-project).
- **Prettier reformats Markdown** in the pre-commit hook — let it reformat, re-stage, re-commit.
- **The integration test project + `backend-integration` CI job** are gated `if: false` today. Step E flips them on. Don't forget the CI flip.
- **`Entitlement` PK is the tenant id**, not a surrogate — model it as `Entity<LearnStackTenantId>` with the tenant id as the key.
- **Cross-module reads must go through `Application.Contracts`** (Plans' fan-out asks Subscriptions via `GetSubscriptionsByPlanQuery`) — never reach into another module's Domain or DbContext.
- **Migrations with RLS in the SQL** mean the DbContext wrongly applied a tenant filter. Hub uses none — fix the config, regenerate.

## 9. Out of scope (do NOT build — later packets)

- The four HTTPS contract endpoints + outbound `LearnStackApiClient` + mTLS/JWT/HMAC chain → **P02c-2**.
- `learnstack.hub.entitlement` Dapr publish + real `IOutbox` → **P02c-2** (ship the OutboxFlush shell only).
- `Usage` module + `POST /api/v1/usage/report` → **P02c-2**.
- Operator portal UI, Operators module, Audit module + live audit writer, `AuthorizationBehavior` real logic → **P02c-4**.
- `CustomDomains` + `Compliance` modules (the projection's `compliance_caps` stays empty `{}`) → **P02c-5**.
- `LicenseKeys` + `.lic` + `grace_until` → **P02c-6**.
- Stripe / Iyzico / Invoicing / dunning (`payment_provider` stays null, `MarkPastDue`/`Cure` stay shells) → **Phase 09b**.
- Any change to the `../LearnStack` repo → **P02c-3** (a separate, coordinated packet).

## 10. Definition of done

- [ ] Hub SharedKernel mirrors LearnStack P02a-2 (with `OperatorId` / `HubException`), unit-tested.
- [ ] Cross-cutting foundation wired: `HubExceptionHandler`, 6-step pipeline, `ToActionResult`, Serilog+OTel, `IErrorTrackingProvider`, `IProviderResilience`.
- [ ] Four modules (`TenantLifecycle`, `Plans`, `Subscriptions`, `Entitlements`) with aggregates, state machines, commands/queries, validators, DbContexts, EF configs.
- [ ] EF migrations per module create `hub`-schema tables in `learnstack_hub`; apply cleanly; **no RLS / no policy** in the generated SQL.
- [ ] `EntitlementProjectionService` rebuilds the projection from Plan + Subscription with monotonic `generation`; matches the wire-shape in `docs/architecture/entitlement-projection.md`.
- [ ] Architecture tests real + green; integration tests (Testcontainers) green; `backend-integration` CI job activated.
- [ ] `EntitlementProjection_Shape_IsStable` contract test + `entitlement-v1.schema.json` checked in.
- [ ] Plan seed data (4 tiers) + demo tenant/subscription/entitlement.
- [ ] `dotnet build` 0/0, `dotnet test` green, `dotnet format --verify` exit 0, leakwatch 0 findings, compose configs parse.
- [ ] `docs/roadmap/README.md` updated (P02c-1 ✅, P02c-2 next); any deferrals (analyzer, registry-sync) noted.
- [ ] Branch pushed, PR opened with a summary mapping each commit to the spec docs.

**Start by reading §2 in order, fixing the dotnet PATH (§0), then loading [`implement-task`](../../.claude/skills/implement-task/SKILL.md) and executing §4 step by step.** When in doubt about a pattern, open the LearnStack-side source (§2 items 13-17) and mirror it exactly.
