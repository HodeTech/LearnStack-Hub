# Hub Roadmap — Phase 02c Packet Status

The authoritative phase plan lives in [LearnStack core's `phase-02c-hub-foundation.md`](../../../learnstack/docs/roadmap/phase-02c-hub-foundation.md). This file is a **status mirror** — it tracks per-packet state inside the Hub repo.

## Packet status

| Packet     | Title                                                                                                                           | State      | PR               |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------- | ---------- | ---------------- |
| **P02c-0** | Repository bootstrap                                                                                                            | ✅ Shipped | (initial commit) |
| **P02c-1** | Hub Domain Core (`LearnStackTenant`, `Plan`, `HubSubscription`, `Entitlement`)                                                  | ✅ Shipped | this branch      |
| **P02c-2** | Hub-side Internal API + Outbound `LearnStackApiClient`                                                                          | ⏳ Next    | —                |
| **P02c-3** | LearnStack core PR (`HubEntitlementProvider`, `IUsageReporter`, internal-API handlers) — **blocked on LearnStack P02a-5/6/7/9** | ⏳         | —                |
| **P02c-4** | Operator Portal MVP                                                                                                             | ⏳         | —                |
| **P02c-5** | Custom Domain Lifecycle                                                                                                         | ⏳         | —                |
| **P02c-6** | License Key (functional skeleton)                                                                                               | ⏳         | —                |
| **P02c-7** | End-to-End Exit Gate                                                                                                            | ⏳         | —                |

## P02c-0 deliverables (this commit)

- ✅ Sibling `learnstack-hub` repo at `/Users/dev/Documents/Projects/learnstack-hub` (`git init`)
- ✅ Top-level config files (`.gitignore`, `.editorconfig`, `.gitattributes`, `.env.example`, `.leakwatch.yaml`, `.leakwatchignore`)
- ✅ Top-level docs (`README.md`, `CLAUDE.md`, `AGENTS.md`, `CONTRIBUTING.md`)
- ✅ Makefile + `.githooks/pre-commit` (chmod +x; activated via `make hooks`)
- ✅ Backend solution (`LearnStack.Hub.slnx`) + 7 core projects + 4 test projects
- ✅ Frontend pnpm monorepo (`apps/operator-portal` + `packages/{config,sdk,ui}`)
- ✅ Infra: `compose/dev.yml` + `compose/e2e.yml` + APISIX config + Dapr components + Postgres init + Keycloak README
- ✅ `scripts/seed.sh` (orchestrator only; real seed data in P02c-1+)
- ✅ `.github/workflows/ci.yml` + CODEOWNERS + PR template
- ✅ `docs/` scaffolding (architecture pointers, decisions template, operations placeholder, modules placeholder, glossary)
- ✅ Architecture test placeholders (meta-test + `No_Source_Folder_Named_Verticals` + `Hub_NeverStores_TenantData` placeholder)

## P02c-1 deliverables (this branch)

- ✅ Hub SharedKernel (mirror of LearnStack P02a-2): `Result<T>`/`Error`, `LocalizedMessage`, `Entity`/`AuditableEntity` (audit columns on `OperatorId`), Vogen ids (`OperatorId`, `LearnStackTenantId`), `IClock`/`IGuidFactory`/`IRandom`, pagination, `HubException` hierarchy, secrets, observability (`CapturedContext` operator-scoped), resilience, `DeploymentMode`, `FeatureFlags` registries, `IUnitOfWork`
- ✅ Cross-cutting foundation: `HubExceptionHandler` + Problem Details, the **6-step** MediatR pipeline (no `TenantContextBehavior`; live `TransactionBehavior`), Serilog + OpenTelemetry, `IErrorTrackingProvider` (NoOp / Sentry shell / LocalFile) branched by `DeploymentMode`, Polly `IProviderResilience`
- ✅ Four modules — `TenantLifecycle` / `Plans` / `Subscriptions` / `Entitlements` — each with aggregate + state machine + commands/queries + validators + DbContext + EF config + repository + migration (`hub` schema, `learnstack_hub` db, per-module history table, no RLS)
- ✅ `EntitlementProjectionService` rebuilds the projection from `Plan` + `HubSubscription` with monotonic `generation`; `learnstack.hub.entitlement` Dapr publish stays a no-op shell (P02c-2)
- ✅ Tests: architecture (boundary, per-module dependency, aggregate-id, pipeline-order, meta) + unit (aggregate state machines + generation monotonicity) + contract (`EntitlementProjection_Shape_IsStable` vs `entitlement-v1.schema.json`) + integration (Testcontainers full flow); `backend-integration` CI job activated
- ✅ Seed: 4 plan tiers + demo tenant via `dotnet run -- --seed` / `make seed` (idempotent)

### Deferred from P02c-1 (tracked follow-ups)

- ⏳ **Roslyn `DomainException` analyzer** (`LearnStack.Hub.Analyzers`) — deferred per cross-cutting-foundation.md § 5; the `DomainException`-vs-`Result.Fail` rule rides code review until then. Target: P02c-2.
- ⏳ **EF-Core OpenTelemetry instrumentation** (`AddEntityFrameworkCoreInstrumentation`) — the only published package is a 1.x-beta whose `OpenTelemetry.Api` floor conflicts with the stable 1.15.x instrumentation set; reserved in `Directory.Packages.props`. Target: P02c-2.
- ⏳ **Feature/limit registry sync** — Hub seeds `FeatureKeys`/`LimitKeys` from the projection wire-shape (Architecture 24 § 4); a cross-repo reconciliation with LearnStack core's registry (or a shared `LearnStack.Contracts` package) is the durable fix. Target: Phase 11.
- ⏳ **SQL keyset pagination** — list repositories slice in memory in P02c-1 (tiny volume); promote to `ORDER BY ... WHERE id > cursor` when volume warrants.

## Dependency on LearnStack core packets

Phase 02c P02c-3 is **blocked** until the following LearnStack core packets ship:

| LearnStack packet           | What it provides                                                                                        | Status |
| --------------------------- | ------------------------------------------------------------------------------------------------------- | ------ |
| P02a-5 (Dapr + APISIX)      | `IEventBus` / `ICacheService` / `ISecretProvider` interfaces + APISIX `/api/internal/*` SSL-object stub | ⏳     |
| P02a-6 (Tenancy schema)     | `platform_entitlement_cache` + `platform_host_to_tenant` tables                                         | ⏳     |
| P02a-7 (Tenant resolution)  | `IHostToTenantResolver` + `TenantResolverMiddleware` + `HubCorrelationMiddleware` stub                  | ⏳     |
| P02a-9 (Entitlement socket) | `IEntitlementProvider` interface + `NullEntitlementProvider` default                                    | ⏳     |

P02c-0, P02c-1, P02c-2, P02c-4 are **unblocked** — they don't depend on LearnStack P02a packets and can ship in parallel.
