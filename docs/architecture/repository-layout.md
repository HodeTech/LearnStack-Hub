# Repository layout

LearnStack Hub mirrors LearnStack core's modular-monolith repository pattern.

```
learnstack-hub/
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
│   │   └── Modules/                                 # Hub modules (P02c-1+)
│   │       ├── TenantLifecycle/                     # P02c-1
│   │       ├── Plans/                               # P02c-1
│   │       ├── Subscriptions/                       # P02c-1
│   │       ├── Entitlements/                        # P02c-1
│   │       ├── CustomDomains/                       # P02c-5
│   │       ├── Compliance/                          # P02c-5
│   │       ├── Usage/                               # P02c-2
│   │       ├── LicenseKeys/                         # P02c-6
│   │       ├── Invoicing/                           # Phase 09b
│   │       ├── Audit/                               # P02c-4
│   │       └── Operators/                           # P02c-4
│   └── tests/
│       ├── LearnStack.Hub.Tests.Unit/               # domain + application unit tests
│       ├── LearnStack.Hub.Tests.Integration/        # Testcontainers (P02c-2+)
│       ├── LearnStack.Hub.Tests.Architecture/       # NetArchTest rules (mandatory, non-skippable)
│       └── LearnStack.Hub.Tests.Contract/           # OpenAPI contract assertions (P02c-2+)
├── frontend/
│   ├── pnpm-workspace.yaml
│   ├── apps/
│   │   └── operator-portal/             # Next.js 16 App Router (P02c-0 scaffold; P02c-4 content)
│   └── packages/
│       ├── config/                      # shared ESLint, TypeScript, Tailwind config
│       ├── sdk/                         # generated Hub API client (P02c-2)
│       └── ui/                          # operator portal design-system primitives (P02c-4)
├── infra/
│   ├── compose/
│   │   ├── dev.yml                      # Hub-only services (Dapr sidecar + APISIX + Postgres init)
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
│   │   ├── contract-with-learnstack.md  # pointers to LearnStack-side authority
│   │   └── repository-layout.md         # (this file)
│   ├── decisions/                       # Hub-internal ADRs (HUB-NNNN series)
│   ├── operations/                      # runbooks (Phase 11+)
│   ├── modules/                         # per-module deep dives (P02c-1+)
│   ├── roadmap/                         # P02c packet status mirror
│   └── glossary.md                      # Hub-specific terms
├── scripts/
│   └── seed.sh                          # idempotent dev seed (P02c-1+ fills with real data)
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

## Comparison with LearnStack core

| Concern           | LearnStack core                                                                                                                             | LearnStack Hub                                                                                                                                                     |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Top-level layout  | backend / frontend / docs / infra / scripts                                                                                                 | Same                                                                                                                                                               |
| Backend solution  | LearnStack.slnx                                                                                                                             | LearnStack.Hub.slnx                                                                                                                                                |
| Core projects     | 7 (SharedKernel, Domain, Application.Contracts, Application, Infrastructure, Infrastructure.Audit, Api)                                     | 7 (same names, Hub prefix)                                                                                                                                         |
| Modules           | 7 (Tenancy, Identity, Customization, Audit, Content, Media, Education)                                                                      | 11 planned (TenantLifecycle, Plans, Subscriptions, Entitlements, CustomDomains, Compliance, Usage, LicenseKeys, Invoicing, Audit, Operators) — none in P02c-0      |
| Test projects     | 4 (Unit, Integration, Architecture, Contract)                                                                                               | 4 (same names)                                                                                                                                                     |
| Frontend apps     | apps/web (tenant-facing)                                                                                                                    | apps/operator-portal (operator-facing)                                                                                                                             |
| Frontend packages | config / sdk / ui (`@learnstack/*`)                                                                                                         | config / sdk / ui (`@learnstack-hub/*`)                                                                                                                            |
| Compose services  | 15 (Postgres, Valkey, SeaweedFS, Mailpit, Meilisearch, Keycloak, LiveKit, Coturn, Kafka, kafka-ui, Vault, Dapr placement + sidecar, APISIX) | ~4 (Dapr placement, Dapr sidecar, APISIX, Postgres init). Shares LearnStack core's Postgres / Valkey / Vault / Kafka / Keycloak / Mailpit via host.docker.internal |
| APISIX port       | 9080 / 9443 / 9091                                                                                                                          | 9180 / 9543 / 9191 (no collision)                                                                                                                                  |
| Dapr ports        | placement 50005, sidecar 3500/50001                                                                                                         | placement 50006, sidecar 3501/50002                                                                                                                                |
| API port          | 5080                                                                                                                                        | 5181                                                                                                                                                               |
