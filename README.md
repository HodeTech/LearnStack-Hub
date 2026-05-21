# LearnStack Hub

The control plane application for [LearnStack](https://github.com/cemililik/learnstack) — a separate codebase that owns tenant lifecycle, subscription / plan / billing, license issuance, entitlement projection, custom-domain administration, compliance caps, and the operator portal.

LearnStack Hub is **not** an LMS, **not** a tenant-facing surface, and **never** stores tenant content. Hub holds tenant _metadata_ (plan, subscription, license, custom domain, compliance caps); tenant _data_ (courses, lessons, learners, enrollments, classroom sessions) lives exclusively inside LearnStack core.

## Status

**Phase 02c — Repository Bootstrap (P02c-0)** ✅. Solution scaffold + frontend monorepo + compose stack + CI + docs skeleton are in place. No Hub domain code yet — that lands in P02c-1 (Hub Domain Core: `LearnStackTenant` mirror, `Plan`, `HubSubscription`, `Entitlement`).

See [docs/roadmap/README.md](docs/roadmap/README.md) for the per-packet breakdown (P02c-0 through P02c-7) and the [authoritative LearnStack-side roadmap](../learnstack/docs/roadmap/phase-02c-hub-foundation.md).

```bash
make install   # one-time: deps + git hooks
make dev       # bring Hub-side compose stack up (requires learnstack compose already running)
```

## Why a Separate Repo

LearnStack Hub ships as a **separate git repository** per [ADR-0019](../learnstack/docs/decisions/0019-learnstack-hub.md) — separate CI/CD, separate release cadence, separate Keycloak realm (`learnstack-hub` vs. `learnstack`), separate operator audit stream. The two repos communicate through a **closed four-endpoint HTTPS contract surface** with mTLS + signed JWT + HMAC body signature on every call.

The architecture deep-dive lives in the sibling repo: [docs/architecture/24-learnstack-hub.md](../learnstack/docs/architecture/24-learnstack-hub.md).

## Sibling Layout Expected

Hub repo's documentation cross-links to LearnStack core via relative sibling paths (`../learnstack/...`). The expected on-disk layout is:

```
<parent-dir>/
├── learnstack/        (https://github.com/cemililik/learnstack)
└── learnstack-hub/    (this repo)
```

If you check out the two repos somewhere other than as siblings, cross-repo doc links will 404. Multi-root workspace files (`*.code-workspace`) are gitignored — each developer keeps their own.

## Dev Workflow

Hub repo's compose stack is **deliberately minimal**: it only runs services that are Hub-specific (Hub Dapr sidecar, Hub APISIX gateway, Postgres init). Everything else — Postgres, Valkey, Vault, Kafka, Keycloak, Mailpit — is shared with LearnStack core's compose stack and reached via `host.docker.internal`.

**Boot order (each in its own terminal):**

```bash
# 1. LearnStack core compose (shared backends)
cd ../learnstack
make dev

# 2. Hub-side compose (Dapr sidecar + Hub APISIX + Postgres init for learnstack_hub DB)
cd ../learnstack-hub
make dev

# 3. LearnStack core API
cd ../learnstack/backend
dotnet run --project src/LearnStack.Api

# 4. Hub API
cd ../learnstack-hub/backend
dotnet run --project src/LearnStack.Hub.Api
```

In production, the two repos deploy independently — the shared compose is a dev-time convenience only.

## Direction At A Glance

- **Backend:** .NET 10 + ASP.NET Core + EF Core + MediatR + Hangfire (queue jobs land in P02c-5+).
- **Database:** PostgreSQL 18 (shared instance in dev under `learnstack_hub` database; separate instance in prod). Hub does **not** use Row-Level Security — Hub data is operator-administered, not tenant-isolated.
- **Cache / Pub-Sub / Secrets:** Valkey 8 (shared instance, `hub:*` namespace), Kafka (shared cluster, `learnstack.hub.*` topic prefix), Vault (shared instance, `learnstack-hub/*` path prefix) — all accessed via **Hub's own Dapr sidecar**.
- **API Gateway:** APISIX in standalone YAML mode on its own port (9180 / 9543); separate instance from LearnStack core's APISIX.
- **Frontend:** Next.js 16 (App Router) operator portal under `frontend/apps/operator-portal`. Authenticates against the `learnstack-hub` Keycloak realm with MFA required.
- **Identity:** Keycloak `learnstack-hub` realm (separate from `learnstack` tenant-facing realm). The realm export lives in the sibling repo at `learnstack/infra/keycloak/realms/learnstack-hub.json` because LearnStack core's compose imports both realms at first boot.
- **Architecture:** Modular monolith (mirrors LearnStack's pattern) with explicit module contracts.

## Hub HTTPS Contract Surface

Four endpoints, closed list ([Standards 20 § Hub HTTPS Contract Surface](../learnstack/docs/standards/20-infrastructure-stack.md)):

| Direction        | Method + Path                                 | Purpose                                                                   | Hosted in       |
| ---------------- | --------------------------------------------- | ------------------------------------------------------------------------- | --------------- |
| Hub → LearnStack | `POST /api/internal/tenants`                  | Create tenant + default org                                               | LearnStack core |
| Hub → LearnStack | `PUT /api/internal/tenants/{id}/entitlements` | Push entitlement projection (incl. host mapping per ADR-0022 Amendment 1) | LearnStack core |
| LearnStack → Hub | `POST /api/v1/internal/license/verify`        | License verify                                                            | **Hub**         |
| LearnStack → Hub | `POST /api/v1/usage/report`                   | Usage telemetry                                                           | **Hub**         |

Every call carries **mTLS** (LearnStack-internal CA) + **RS256 JWT** (`aud=learnstack-internal`, exp ≤ 5min) + **HMAC-SHA256 body signature** in `X-Signature`. Adding a fifth endpoint requires a new ADR.

## Documentation Map

### Hub-specific (this repo)

- [docs/README.md](docs/README.md) — Hub doc catalogue.
- [docs/architecture/contract-with-learnstack.md](docs/architecture/contract-with-learnstack.md) — Pointers to LearnStack-side authoritative contracts.
- [docs/architecture/repository-layout.md](docs/architecture/repository-layout.md) — Hub internal folder structure + module topology.
- [docs/decisions/](docs/decisions/) — Hub-internal ADRs (start with `HUB-0001`).
- [docs/operations/](docs/operations/) — Hub operational runbooks (Phase 11+).
- [docs/modules/](docs/modules/) — Hub module deep dives (Phase 02c-1+).
- [docs/roadmap/README.md](docs/roadmap/README.md) — P02c-0..7 packet plan.
- [docs/glossary.md](docs/glossary.md) — Hub-specific terms.

### Authoritative cross-cutting (LearnStack core)

- [ADR-0019 LearnStack Hub](../learnstack/docs/decisions/0019-learnstack-hub.md)
- [ADR-0020 Triple Deployment + Hybrid License](../learnstack/docs/decisions/0020-triple-deployment-hybrid-license.md)
- [ADR-0021 Feature-Based Entitlement](../learnstack/docs/decisions/0021-feature-based-entitlement.md)
- [ADR-0022 Custom Domain + TLS](../learnstack/docs/decisions/0022-custom-domain-tls.md) (Amendment 1)
- [ADR-0004 Authentication Strategy](../learnstack/docs/decisions/0004-authentication-strategy.md) (Amendment 1 — `learnstack-hub` realm)
- [Architecture 24 LearnStack Hub](../learnstack/docs/architecture/24-learnstack-hub.md)
- [Standards 20 Infrastructure Stack](../learnstack/docs/standards/20-infrastructure-stack.md)
- [Phase 02c roadmap](../learnstack/docs/roadmap/phase-02c-hub-foundation.md)

## Conventions

- All documentation in **English** (mirrors [ADR-0007](../learnstack/docs/decisions/0007-documentation-language-and-conventions.md)).
- Diagrams use **Mermaid** in fenced code blocks.
- Cross-cutting architectural decisions live in **LearnStack core** under `learnstack/docs/decisions/`; Hub-internal-only decisions live here under `docs/decisions/` with the `HUB-NNNN` numbering series so they never collide with LearnStack ADR numbers.
- Engineering rules from LearnStack's [Standards corpus](../learnstack/docs/standards/) apply here unless explicitly overridden by a Hub-internal ADR.
- Single source of truth: each piece of knowledge lives in exactly one place. Linking is preferred over copying.
