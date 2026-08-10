# LearnStack Hub

The control plane application for [LearnStack](https://github.com/HodeTech/LearnStack) — a separate codebase that owns tenant lifecycle, subscription / plan / billing, license issuance, entitlement projection, custom-domain administration, compliance caps, and the operator portal.

LearnStack Hub is **not** an LMS, **not** a tenant-facing surface, and **never** stores tenant content. Hub holds tenant _metadata_ (plan, subscription, license, custom domain, compliance caps); tenant _data_ (courses, lessons, learners, enrollments, classroom sessions) lives exclusively inside LearnStack core.

## Status

**P02c-0 — Repository bootstrap** ✅. Solution scaffold, frontend monorepo, compose stack, CI, and the docs skeleton are in place.

**P02c-1 (Hub Domain Core) shipped 2026-08-09.** It was reviewed against the restructured corpus first: although it was written before [ADR-0033](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0033-audit-durability-model.md), [ADR-0034](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md) and [ADR-0035](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0035-demand-gated-infrastructure.md), it implements none of what they changed — its entitlement wire shape already carries `grace_until` and `generation`, it hosts no endpoint, and its audit behavior is a shell.

**The track is frozen from P02c-2 onward** (owner decision, 2026-08-08), on the ADR-0035 trigger for `IEntitlementProvider`: a tenant must be billed or plan-gated. Until then LearnStack runs on `NullEntitlementProvider` and needs nothing from the Hub. See [CLAUDE.md](CLAUDE.md) and [docs/roadmap/p02c-1-hub-domain-core.md](docs/roadmap/p02c-1-hub-domain-core.md).

**The Hub plan lives in this repository.** [`docs/roadmap/`](docs/roadmap/README.md) carries a document per packet (P02c-0 … P02c-7) plus the post-02c [billing](docs/roadmap/hub-billing.md) and [marketplace](docs/roadmap/hub-marketplace.md) tracks. LearnStack's [phase-02c](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-02c-hub-foundation.md) covers only LearnStack's side of the boundary.

```bash
make install   # one-time: deps + git hooks
make dev       # bring Hub-side compose stack up (requires the LearnStack compose already running)
```

## Why a Separate Repo

LearnStack Hub ships as a **separate git repository** per [ADR-0019](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0019-learnstack-hub.md) — separate CI/CD, separate release cadence, separate Keycloak realm (`learnstack-hub` vs. `learnstack`), separate operator audit stream. The two repos communicate over an internal HTTPS surface carrying mTLS + signed JWT + HMAC body signature on every call.

The architecture deep-dive lives in the sibling repo: [docs/architecture/24-learnstack-hub.md](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md).

## Sibling Layout Expected

Hub documentation **links** to LearnStack core by absolute URL (`https://github.com/HodeTech/LearnStack/blob/main/docs/...`), per [Documentation Standards § Layout](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/13-documentation.md). A relative path does not cross a repository boundary on github.com and depends on a sibling checkout being present and identically capitalised; an absolute URL works everywhere. Shell commands and filesystem references still use `../LearnStack` — those are paths, not links — so the expected on-disk layout still matters:

```text
<parent-dir>/
├── LearnStack/        (https://github.com/HodeTech/LearnStack)
└── LearnStack-Hub/    (this repo)
```

The capitalisation matters for the shell paths: a lower-cased spelling works on macOS's case-insensitive filesystem and fails on Linux CI. Documentation links are unaffected — they are absolute, and the CI link audit now checks every relative link it finds, because a relative cross-repo link is a defect rather than an exemption.

If you check out the two repos somewhere other than as siblings, the shell commands below need adjusting; the documentation links keep working. Multi-root workspace files (`*.code-workspace`) are gitignored — each developer keeps their own.

## Dev Workflow

Hub repo's compose stack is **deliberately minimal**: it only runs services that are Hub-specific (Hub Dapr sidecar, Hub APISIX gateway, Postgres init). Everything else — Postgres, Valkey, Vault, Kafka, Keycloak, Mailpit — is shared with LearnStack core's compose stack and reached via `host.docker.internal`.

**Boot order (each in its own terminal):**

```bash
# 1. LearnStack core compose (shared backends)
cd ../LearnStack
make dev

# 2. Hub-side compose (Dapr sidecar + Hub APISIX + Postgres init for learnstack_hub DB)
cd ../LearnStack-Hub
make dev

# 3. LearnStack core API
cd ../LearnStack/backend
dotnet run --project src/LearnStack.Api

# 4. Hub API
# The host does not load .env itself, so export it first — otherwise the
# connection string resolves to defaults and the API cannot reach Postgres.
cd ../LearnStack-Hub
set -a; . ./.env; set +a
export POSTGRES_HOST=localhost
cd backend
dotnet run --project src/Core/LearnStack.Hub.Api
```

In production, the two repos deploy independently — the shared compose is a dev-time convenience only.

## Direction At A Glance

- **Backend:** .NET 10 + ASP.NET Core + EF Core + MediatR + Hangfire (queue jobs land in P02c-5+).
- **Database:** PostgreSQL 18 (shared instance in dev under `learnstack_hub` database; separate instance in prod). Hub does **not** use Row-Level Security — Hub data is operator-administered, not tenant-isolated.
- **Cache / Pub-Sub / Secrets:** Valkey 8 (shared instance, `hub:*` namespace), Kafka (shared cluster, `learnstack.hub.*` topic prefix), Vault (shared instance, `learnstack-hub/*` path prefix) — all accessed via **Hub's own Dapr sidecar**.
- **API Gateway:** APISIX in standalone YAML mode on its own port (9180 / 9543); separate instance from LearnStack core's APISIX.
- **Frontend:** Next.js 15.5 (App Router) operator portal under `frontend/apps/operator-portal`; flat-config + Next 16 migration tracked in P02c-4 (`apps/operator-portal/.eslintrc.cjs` TODO). Authenticates against the `learnstack-hub` Keycloak realm with MFA required.
- **Identity:** Keycloak `learnstack-hub` realm (separate from `learnstack` tenant-facing realm). The realm export lives in the sibling repo at `../LearnStack/infra/keycloak/realms/learnstack-hub.json` because LearnStack core's compose imports both realms at first boot.
- **Architecture:** Modular monolith (mirrors LearnStack's pattern) with explicit module contracts.

## Contract Surface

Governed by two invariants rather than by a count, per [ADR-0034](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md):

1. **The Hub stores no tenant content** — enforced by `Hub_NeverStores_TenantData`.
2. **Every LearnStack↔Hub crossing goes through a named adapter** — `IEntitlementProvider`, `IUsageReporter`, `IHubTenantSync`. Nothing else holds a Hub client, and nothing resolves a host by calling the Hub.

**Hub → LearnStack** (`/api/internal/*`, internal listener only, hosted in LearnStack core):

| Method   | Path                                          | Purpose                                                  |
| -------- | --------------------------------------------- | -------------------------------------------------------- |
| `POST`   | `/api/internal/tenants`                       | Create tenant + default organization                      |
| `PUT`    | `/api/internal/tenants/{id}/entitlements`     | Push the entitlement projection                           |
| `PUT`    | `/api/internal/tenants/{id}/status`           | Suspend / activate / archive                              |
| `DELETE` | `/api/internal/tenants/{id}`                  | Terminate                                                 |
| `GET`    | `/api/internal/tenants/{id}/usage`            | Pull aggregated usage                                     |
| `PUT`    | `/api/internal/tenants/{id}/host-mappings`    | Push host → `(tenant_id, organization_id?)` mappings      |

**LearnStack → Hub** (hosted here):

| Method | Path                                    | Purpose                                    |
| ------ | --------------------------------------- | ------------------------------------------ |
| `POST` | `/api/v1/internal/license/verify`       | Verify / pull the entitlement projection   |
| `POST` | `/api/v1/internal/license/refresh`      | Scheduled phone-home refresh               |
| `POST` | `/api/v1/usage/report`                  | Report a usage metric (idempotent)         |

Every call carries **mTLS** (LearnStack-internal CA) + **RS256 JWT** (`aud=learnstack-internal`, exp ≤ 5min) + **HMAC-SHA256 body signature** in `X-Signature`. Adding an endpoint requires a new ADR in `../LearnStack/docs/decisions/`, landed in both repositories.

The Hub's own tenant-facing and operator-facing APIs (`/api/v1/tenants/*`, `/api/v1/subscriptions/*`, `/api/v1/webhooks/*`) are **not** part of this surface — they are the Hub's public API, governed here.

TLS certificates and private keys never travel in the entitlement payload. Host mappings go through `PUT /api/internal/tenants/{id}/host-mappings`; cert material moves between the two secret stores by replication and is referenced by path.

## Documentation Map

### Hub-specific (this repo)

- [docs/README.md](docs/README.md) — Hub doc catalogue.
- [docs/roadmap/README.md](docs/roadmap/README.md) — **the Hub plan**, owned here: one document per packet plus the cross-repo blocking table in both directions.
- [docs/architecture/README.md](docs/architecture/README.md) — Hub architecture index.
- [docs/architecture/contract-with-learnstack.md](docs/architecture/contract-with-learnstack.md) — Pointers to LearnStack-side authoritative contracts.
- [docs/architecture/repository-layout.md](docs/architecture/repository-layout.md) — Hub internal folder structure + module topology.
- [docs/decisions/](docs/decisions/) — Hub-internal ADRs (`HUB-NNNN` series).
- [docs/operations/](docs/operations/) — Hub operational runbooks.
- [docs/modules/](docs/modules/) — Hub module deep dives.
- [docs/glossary.md](docs/glossary.md) — Hub-specific terms.

### Authoritative cross-cutting (LearnStack core)

- [ADR-0019 LearnStack Hub](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0019-learnstack-hub.md)
- [ADR-0020 Triple Deployment + Hybrid License](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md)
- [ADR-0021 Feature-Based Entitlement](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md)
- [ADR-0022 Custom Domain + TLS](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0022-custom-domain-tls.md) (Amendment 1; its cert-delivery step is superseded by ADR-0034)
- [ADR-0004 Authentication Strategy](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0004-authentication-strategy.md) (Amendment 1 — `learnstack-hub` realm)
- [ADR-0034 Hub Contract Surface Invariant](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md) — the two invariants and the authoritative endpoint table
- [ADR-0035 Demand-Gated Infrastructure](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0035-demand-gated-infrastructure.md) — why the Hub track waits on a trigger rather than a date
- [Architecture 24 LearnStack Hub](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md)
- [Standards 20 Infrastructure Stack](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/20-infrastructure-stack.md)
- [Phase 02c — LearnStack side](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-02c-hub-foundation.md)

## Conventions

- All documentation in **English** (mirrors [ADR-0007](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0007-documentation-language-and-conventions.md)).
- Diagrams use **Mermaid** in fenced code blocks.
- Cross-cutting architectural decisions live in **LearnStack core** under `../LearnStack/docs/decisions/`; Hub-internal-only decisions live here under `docs/decisions/` with the `HUB-NNNN` numbering series so they never collide with LearnStack ADR numbers. The Hub **roadmap** runs the other way — it is owned here, and LearnStack links to it.
- Engineering rules from LearnStack's [Standards corpus](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/) apply here unless explicitly overridden by a Hub-internal ADR.
- Single source of truth: each piece of knowledge lives in exactly one place. Linking is preferred over copying.
