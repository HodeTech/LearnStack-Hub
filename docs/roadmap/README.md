# Hub Roadmap — Phase 02c Packet Status

The authoritative phase plan lives in [LearnStack core's `phase-02c-hub-foundation.md`](../../../learnstack/docs/roadmap/phase-02c-hub-foundation.md). This file is a **status mirror** — it tracks per-packet state inside the Hub repo.

## Packet status

| Packet     | Title                                                                                                                           | State      | PR               |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------- | ---------- | ---------------- |
| **P02c-0** | Repository bootstrap                                                                                                            | ✅ Shipped | (initial commit) |
| **P02c-1** | Hub Domain Core (`LearnStackTenant`, `Plan`, `HubSubscription`, `Entitlement`)                                                  | ⏳ Next    | —                |
| **P02c-2** | Hub-side Internal API + Outbound `LearnStackApiClient`                                                                          | ⏳         | —                |
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

## Dependency on LearnStack core packets

Phase 02c P02c-3 is **blocked** until the following LearnStack core packets ship:

| LearnStack packet           | What it provides                                                                                        | Status |
| --------------------------- | ------------------------------------------------------------------------------------------------------- | ------ |
| P02a-5 (Dapr + APISIX)      | `IEventBus` / `ICacheService` / `ISecretProvider` interfaces + APISIX `/api/internal/*` SSL-object stub | ⏳     |
| P02a-6 (Tenancy schema)     | `platform_entitlement_cache` + `platform_host_to_tenant` tables                                         | ⏳     |
| P02a-7 (Tenant resolution)  | `IHostToTenantResolver` + `TenantResolverMiddleware` + `HubCorrelationMiddleware` stub                  | ⏳     |
| P02a-9 (Entitlement socket) | `IEntitlementProvider` interface + `NullEntitlementProvider` default                                    | ⏳     |

P02c-0, P02c-1, P02c-2, P02c-4 are **unblocked** — they don't depend on LearnStack P02a packets and can ship in parallel.
