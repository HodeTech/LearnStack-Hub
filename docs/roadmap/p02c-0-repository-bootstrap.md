# P02c-0: Repository Bootstrap

> **Status: ✅ Shipped (2026-05-21), on `main`.** Landed as the initial commit plus three review passes. Nothing in this document is planned work; it is the delivery record for the packet.

## Goal

Stand up the `learnstack-hub` repository as a second, independently buildable codebase that a LearnStack developer can be productive in on the first day — same directory shape, same solution layout, same CI gates, same commit conventions — before any Hub domain code exists.

This is the packet that makes [ADR-0019](../../../LearnStack/docs/decisions/0019-learnstack-hub.md)'s "separate repository" decision real. Everything it fixes is cheap to fix now and expensive later: the project graph, the schema and database names, the dev-stack port allocations that must not collide with LearnStack's, and the CI gates that every subsequent commit is measured against.

It deliberately ships **zero** domain code. A bootstrap that also carries aggregates cannot be reviewed as a bootstrap.

## Scope

### Repository shape

The full layout is documented in [repository-layout.md](../architecture/repository-layout.md) and mirrors LearnStack core's: `backend/`, `frontend/`, `infra/`, `docs/`, `scripts/`, plus the top-level tooling and agent-guidance files.

### Backend

- `LearnStack.Hub.slnx` with the seven core projects (`SharedKernel`, `Domain`, `Application.Contracts`, `Application`, `Infrastructure`, `Infrastructure.Audit`, `Api`) and the four test projects (`Unit`, `Integration`, `Architecture`, `Contract`) — the same names as LearnStack core with the `Hub` prefix.
- `backend/src/Modules/` exists with a `README.md` describing the planned module topology and no module subdirectories. Modules land from P02c-1 onward.
- .NET 10 pinned in `global.json`; central package versions in `Directory.Packages.props`.

### Frontend

- pnpm monorepo with `apps/operator-portal` (Next.js App Router) and `packages/{config,sdk,ui}` under the `@learnstack-hub/*` scope. Scaffold only — the portal's content is [P02c-4](p02c-4-operator-portal.md), and the SDK is generated from P02c-2's OpenAPI document.

### Infrastructure

- `infra/compose/dev.yml` plus an `e2e.yml` ephemeral overlay running only the Hub-specific services: Dapr placement + sidecar, APISIX, and the `learnstack_hub` database init. Postgres, Valkey, Vault, Kafka, Keycloak and Mailpit are shared with LearnStack core's stack over `host.docker.internal`.
- Every Hub port is offset from LearnStack core's so both stacks run side by side: APISIX 9180 / 9543 / 9191, Dapr placement 50006, Dapr sidecar 3501 / 50002, API 5181.
- `infra/keycloak/README.md` documents the shared-instance, two-realm topology per [ADR-0004 Amendment 1](../../../LearnStack/docs/decisions/0004-authentication-strategy.md).

### Developer experience and CI

- Repo-root `Makefile` (`dev` / `build` / `test` / `lint` / `format` / `seed` / `install` / `hooks`), `.env.example` as the single source of truth for dev environment variables, and a `.githooks/pre-commit` running `dotnet format` + prettier + ESLint + a Leakwatch scan.
- `.github/workflows/ci.yml` mirroring LearnStack core's job shape: backend, frontend, meta, and secret-scan. `backend-integration` ships gated `if: false` — it has no test to run until a Testcontainers-backed suite exists.
- `scripts/seed.sh` as an orchestrator shell with no seed data behind it.

### Documentation skeleton

- `docs/architecture/contract-with-learnstack.md` as a pointer-only file: every load-bearing contract rule stays in LearnStack's corpus and is linked, never mirrored.
- `docs/decisions/` reserving the `HUB-NNNN` series for Hub-internal-only decisions, with a template.
- `docs/glossary.md` for Hub-specific terms, `docs/operations/` and `docs/modules/` as placeholders.
- `CLAUDE.md` carrying the hard rules, `AGENTS.md` redirecting to it, `CONTRIBUTING.md`, and a `README.md`.

### Architecture-test placeholders

Three tests land as structural placeholders that become real when module assemblies exist: the meta-test that keeps the suite non-skippable, `No_Source_Folder_Named_Verticals`, and `Hub_NeverStores_TenantData`.

## Deliverables

- Sibling `learnstack-hub` git repository (GitHub: `cemililik/LearnStack-Hub`), initialised and pushed.
- Top-level config: `.gitignore`, `.editorconfig`, `.gitattributes`, `.env.example`, `.leakwatch.yaml`, `.leakwatchignore`.
- Top-level docs: `README.md`, `CLAUDE.md`, `AGENTS.md`, `CONTRIBUTING.md`.
- `Makefile` + `.githooks/pre-commit` (executable; activated by `make hooks`).
- Backend solution `LearnStack.Hub.slnx` with 7 core projects + 4 test projects, building green at 0 warnings / 0 errors.
- Frontend pnpm monorepo (`apps/operator-portal` + `packages/{config,sdk,ui}`) that typechecks, lints and builds.
- `infra/`: `compose/dev.yml`, `compose/e2e.yml`, APISIX config, Dapr components, Postgres init for the `learnstack_hub` database, Keycloak topology README.
- `scripts/seed.sh` orchestrator.
- `.github/workflows/ci.yml` + `CODEOWNERS` + pull-request template.
- `docs/` scaffolding: architecture pointers, decisions template, operations and modules placeholders, glossary.
- Four architecture tests and one smoke test passing.

## Completion Criteria

- `dotnet build LearnStack.Hub.slnx` is clean at 0 warnings and 0 errors; the architecture and smoke suites pass.
- The frontend monorepo installs, typechecks, lints and builds.
- Both compose files parse (`docker compose config`), and bringing the Hub stack up alongside LearnStack core's stack produces no port collision.
- CI is green on the backend, frontend, meta and secret-scan jobs; `backend-integration` is explicitly gated rather than silently absent.
- The pre-commit hook runs the formatter chain and the Leakwatch scan, and a repo-wide scan reports no findings.
- No Hub domain code exists. `backend/src/Modules/` holds only its own `README.md`.

## Risks

- **Silent divergence from LearnStack core's conventions.** Two repositories with the same shape drift the moment one changes and the other does not. Mitigated by keeping the project names, solution layout, CI job names and commit conventions mechanically identical, so a divergence is visible in a diff rather than buried in behaviour.
- **Port and service collisions with the LearnStack dev stack.** Two full compose stacks on one workstation is the normal case, not the exception. Mitigated by offsetting every Hub port and by sharing the heavy backing services instead of duplicating them.
- **The SharedKernel copy drifts from its original.** Hub reproduces LearnStack's `SharedKernel` patterns rather than importing them, because the two repositories release independently. The copy is made in [P02c-1](p02c-1-hub-domain-core.md), not here, but the decision belongs to the bootstrap: a shared `LearnStack.Foundation` package is a Phase 11 re-evaluation, and until then the two copies are reconciled by review.
- **Agent guidance that points at the wrong repository.** The bootstrap originally deferred to LearnStack's skill catalogue "by reference", which an agent running from the Hub root cannot load. Fixed after the fact by the Hub-local catalogue at [`.claude/skills/`](../../.claude/skills/README.md).

## Phase Exit Decision

P02c-0 is complete, and was complete when a clean checkout could build the backend solution, build the frontend monorepo, bring both compose stacks up together, and pass CI — with no Hub domain code present and no unresolved reference to a LearnStack assembly anywhere in the project graph.

[P02c-1](p02c-1-hub-domain-core.md) begins from that state.
