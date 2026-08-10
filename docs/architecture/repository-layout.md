# Repository layout

LearnStack Hub mirrors LearnStack core's modular-monolith repository pattern. The two
repositories sit side by side on disk as `LearnStack/` and `LearnStack-Hub/`, with those
exact capitalisations. That layout governs **filesystem paths and shell commands**
(`cd ../LearnStack`) only; cross-repo documentation **links** from this file are absolute
GitHub URLs (`https://github.com/HodeTech/LearnStack/blob/main/docs/...`), because a
relative path does not cross a repository boundary on GitHub.

This tree describes what is on `main`. `backend/src/Modules/` carries the four
[P02c-1](../roadmap/p02c-1-hub-domain-core.md) modules — `TenantLifecycle`, `Plans`,
`Subscriptions`, `Entitlements` — merged 2026-08-09. The remaining seven land in later
packets, which are frozen from P02c-2 onward.

```text
LearnStack-Hub/
├── backend/
│   ├── LearnStack.Hub.slnx              # .NET solution
│   ├── Directory.Build.props            # .NET central config
│   ├── Directory.Packages.props         # NuGet central versions
│   ├── global.json                      # SDK pin (10.0.100)
│   ├── src/
│   │   ├── Core/
│   │   │   ├── LearnStack.Hub.SharedKernel/         # base types: Result<T>, Entity<TId>, IClock, ...
│   │   │   ├── LearnStack.Hub.Domain/               # cross-module domain marker
│   │   │   ├── LearnStack.Hub.Application.Contracts/# CQRS DTOs that cross module boundaries
│   │   │   ├── LearnStack.Hub.Application/          # MediatR handlers for cross-module work
│   │   │   ├── LearnStack.Hub.Infrastructure/       # EF Core DbContexts, provider adapters, LearnStackApiClient
│   │   │   ├── LearnStack.Hub.Infrastructure.Audit/ # Hub operator audit pipeline
│   │   │   └── LearnStack.Hub.Api/                  # ASP.NET Core host
│   │   └── Modules/                                 # 4 modules on main (P02c-1); 7 planned (see below)
│   │       ├── TenantLifecycle/ Plans/ Subscriptions/ Entitlements/   # four packages each
│   │       └── README.md                            # the module topology table, co-located with the code
│   └── tests/
│       ├── LearnStack.Hub.Tests.Unit/               # domain + application unit tests
│       ├── LearnStack.Hub.Tests.Integration/        # Testcontainers Postgres — the entitlement-rebuild round trip (P02c-1)
│       ├── LearnStack.Hub.Tests.Architecture/       # NetArchTest + file-system rules (mandatory, non-skippable)
│       │   ├── HubBoundaryTests.cs                  # Hub_NeverStores_TenantData, Hub_Modules_DoNotReference_LearnStack_Internals
│       │   ├── ModuleDependencyTests.cs             # module dependency direction
│       │   ├── RepositoryLayoutTests.cs             # No_Source_Folder_Named_Verticals, Frontend_Has_Only_The_OperatorPortal_App
│       │   └── RepositoryPaths.cs                   # working-copy path resolution shared by the file-system rules
│       └── LearnStack.Hub.Tests.Contract/           # entitlement-v1.schema.json + its snapshot test; OpenAPI assertions from P02c-2
├── frontend/
│   ├── pnpm-workspace.yaml
│   ├── pnpm-lock.yaml
│   ├── apps/
│   │   └── operator-portal/             # Next.js 15.5 App Router (P02c-0 scaffold; P02c-4 content; flat-config + Next 16 migration tracked in P02c-4)
│   └── packages/
│       ├── config/                      # shared ESLint, TypeScript, Tailwind config
│       ├── sdk/                         # generated Hub API client (P02c-2)
│       └── ui/                          # operator portal design-system primitives (P02c-4)
├── infra/
│   ├── compose/
│   │   ├── README.md                    # what this stack runs and what it borrows from LearnStack core
│   │   ├── dev.yml                      # Hub-only services (Postgres init + Dapr placement + Dapr sidecar + APISIX)
│   │   └── e2e.yml                      # ephemeral overlay
│   ├── apisix/
│   │   ├── config.yaml                  # APISIX standalone mode
│   │   └── apisix.yaml                  # route table (Hub-side internal API endpoints)
│   ├── dapr/
│   │   ├── config/dapr-config.yaml
│   │   └── components/                  # pubsub-kafka, statestore-valkey, secretstore-{envvar,vault}
│   ├── postgres/
│   │   └── init/01-create-hub-database.sql
│   └── keycloak/
│       └── README.md                    # documents shared-instance + two-realm topology
├── docs/
│   ├── README.md                        # this directory's catalogue
│   ├── architecture/
│   │   ├── README.md                    # architecture index with per-doc status
│   │   ├── contract-with-learnstack.md  # pointers to LearnStack-side authority
│   │   ├── cross-cutting-foundation.md  # Hub SharedKernel mirror; the 6-step MediatR pipeline; OperatorId
│   │   ├── entitlement-projection.md    # projection wire-shape, generation counter, recompute triggers
│   │   ├── module-topology.md           # module dependency graph; the "Hub does NOT use RLS" model
│   │   └── repository-layout.md         # (this file)
│   ├── roadmap/                         # the authoritative Hub plan — one document per packet
│   ├── decisions/                       # Hub-internal ADRs (HUB-NNNN series) — README + template only today
│   ├── operations/                      # runbooks — README only today
│   ├── modules/                         # per-module deep dives — 4 shipped (see below)
│   └── glossary.md                      # Hub-specific terms
├── scripts/
│   └── seed.sh                          # idempotent dev seed — orchestrates `dotnet run -- --seed` (plan tiers + demo tenant)
├── .claude/
│   └── skills/                          # 18 Hub-tailored skills + README; git-tracked via a .gitignore un-ignore rule
├── .github/
│   ├── workflows/ci.yml                 # backend + frontend + meta + secret-scan
│   ├── CODEOWNERS
│   └── pull_request_template.md
├── .githooks/
│   └── pre-commit                       # dotnet format + prettier + eslint + leakwatch
├── README.md
├── CLAUDE.md                            # agent guidance (hard rules)
├── AGENTS.md                            # redirect to CLAUDE.md
├── CONTRIBUTING.md
├── Makefile                             # dev / build / test / lint / format / seed / install / hooks
├── .env.example                         # single source of truth for dev env vars
├── .gitignore
├── .gitattributes
├── .editorconfig
├── .leakwatch.yaml
└── .leakwatchignore
```

## Documentation on disk

`docs/` is no longer a skeleton. What is written today:

| Directory          | Files on disk                                                                                                              | State                                        |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------- |
| `docs/architecture/` | `README.md`, `contract-with-learnstack.md`, `cross-cutting-foundation.md`, `entitlement-projection.md`, `module-topology.md`, `repository-layout.md` | Five architecture docs plus the index. `learnstack-api-client.md` (P02c-2) and `operator-portal.md` (P02c-4) are still to come — see [architecture/README.md](README.md). |
| `docs/modules/`    | `README.md`, `tenant-lifecycle.md`, `plans.md`, `subscriptions.md`, `entitlements.md`                                       | Four module deep dives, one per P02c-1 module. Authored as design specification ahead of the implementation; since P02c-1 merged (2026-08-09) they are the living description of the code on `main`. |
| `docs/roadmap/`    | `README.md`, one document per packet, `P02c-1-implementation-prompt.md`                                                     | The authoritative Hub plan, owned in this repository. |
| `docs/decisions/`  | `README.md`, `template.md`                                                                                                 | No `HUB-NNNN` ADR has been needed yet.       |
| `docs/operations/` | `README.md`                                                                                                                | Runbooks land with the first non-dev deployment. |
| `docs/glossary.md` | single file                                                                                                                | Hub-specific terms.                          |

## Module topology (four on disk, seven planned)

The four [P02c-1](../roadmap/p02c-1-hub-domain-core.md) modules — `TenantLifecycle`, `Plans`, `Subscriptions`, `Entitlements` — **are on `main`** as of 2026-08-09, with their specifications under `docs/modules/`. The seven below them are not yet on disk and land across the remaining Hub packets, which are frozen from P02c-2 onward.

| Module subdirectory under `backend/src/Modules/` | Lands in                                     | Aggregates                                      |
| ------------------------------------------------ | -------------------------------------------- | ----------------------------------------------- |
| `TenantLifecycle/`                               | P02c-1 (on `main`)                           | `LearnStackTenant` (mirror)                     |
| `Plans/`                                         | P02c-1 (on `main`)                           | `Plan`                                          |
| `Subscriptions/`                                 | P02c-1 (on `main`)                           | `HubSubscription`                               |
| `Entitlements/`                                  | P02c-1 (on `main`)                           | `Entitlement` (projection)                      |
| `Usage/`                                         | P02c-2                                       | `UsageAggregate`                                |
| `Audit/`                                         | P02c-4                                       | `AuditEntry` (operator audit)                   |
| `Operators/`                                     | P02c-4                                       | Operator role + permission mapping              |
| `CustomDomains/`                                 | P02c-5                                       | `CustomDomain`                                  |
| `Compliance/`                                    | P02c-5                                       | `CompliancePolicy`                              |
| `LicenseKeys/`                                   | P02c-6                                       | `LicenseKey`                                    |
| `Invoicing/`                                     | [hub-billing](../roadmap/hub-billing.md)     | `HubInvoice`, `HubInvoiceLine`, `WebhookLedger` |

Each module follows the same four-layer pattern as LearnStack core (`Application.Contracts`, `Application`, `Domain`, `Infrastructure`). See [`backend/src/Modules/README.md`](../../backend/src/Modules/README.md) for the same table maintained co-located with the code.

## Comparison with LearnStack core

| Concern           | LearnStack core                                                                                                                                                                                     | LearnStack Hub                                                                                                                                                          |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Top-level layout  | backend / frontend / docs / infra / scripts                                                                                                                                                          | Same                                                                                                                                                                     |
| Backend solution  | `LearnStack.slnx`                                                                                                                                                                                    | `LearnStack.Hub.slnx`                                                                                                                                                    |
| Core project root | Flat under `backend/src/`                                                                                                                                                                            | Nested under `backend/src/Core/`                                                                                                                                         |
| Core projects     | 10 (SharedKernel, Domain, Application.Contracts, Application, Infrastructure, Infrastructure.Audit, Infrastructure.ErrorTracking, Infrastructure.Observability, Infrastructure.Resilience, Api) + a Roslyn analyzer project under `backend/analyzers/` | 7 (SharedKernel, Domain, Application.Contracts, Application, Infrastructure, Infrastructure.Audit, Api) — Hub prefix. The three extra LearnStack infrastructure projects landed with its P02a-3 cross-cutting foundation; Hub folds the equivalents into `Infrastructure` |
| MediatR pipeline  | 8 steps                                                                                                                                                                                              | **6 steps** — no `TenantContextBehavior`, because Hub data is operator-administered rather than tenant-isolated. See [cross-cutting-foundation.md](cross-cutting-foundation.md) |
| Row Level Security | Every tenant-owned table                                                                                                                                                                            | None. Hub stores no tenant content                                                                                                                                       |
| Modules           | 7 (Tenancy, Identity, Customization, Audit, Content, Media, Education) — directories exist, domain code lands across Phase 02a packets 6–9                                                            | 11 planned; **four on `main`** — `TenantLifecycle`, `Plans`, `Subscriptions`, `Entitlements`, shipped by P02c-1                                                                                  |
| Test projects     | 4 (Unit, Integration, Architecture, Contract)                                                                                                                                                        | 4 (same names)                                                                                                                                                           |
| Frontend apps     | `apps/web` (tenant-facing)                                                                                                                                                                           | `apps/operator-portal` (operator-facing), pinned by `Frontend_Has_Only_The_OperatorPortal_App`                                                                            |
| Frontend packages | config / sdk / ui (`@learnstack/*`)                                                                                                                                                                  | config / sdk / ui (`@learnstack-hub/*`)                                                                                                                                  |
| Compose services  | 14 (Postgres, Valkey, SeaweedFS, Mailpit, Meilisearch, Keycloak, LiveKit, Coturn, Kafka, kafka-ui, Vault, Dapr placement, Dapr sidecar, APISIX). LearnStack P02a-5 moves the demand-gated ones behind a non-default profile per ADR-0035 | 4 (Postgres init, Dapr placement, Dapr sidecar, APISIX). Shares LearnStack core's Postgres / Valkey / Vault / Kafka / Keycloak / Mailpit via `host.docker.internal`      |
| APISIX port       | 9080 / 9443 / 9091                                                                                                                                                                                   | 9180 / 9543 / 9191 (no collision)                                                                                                                                        |
| Dapr ports        | placement 50005, sidecar 3500/50001                                                                                                                                                                  | placement 50006, sidecar 3501/50002                                                                                                                                      |
| API port          | 5080                                                                                                                                                                                                 | 5181                                                                                                                                                                     |
