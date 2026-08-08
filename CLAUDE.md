# Working in this repository

This file is read first by Claude Code (and any other agent following the convention). It tells you what this project is, what state it is in, and the conventions you must follow when contributing.

## What this is

**LearnStack Hub** is the control plane companion to [LearnStack core](../LearnStack) — a separate codebase that owns tenant lifecycle, subscription / plan / billing, license issuance, entitlement projection, custom-domain administration, compliance caps, and the operator portal. The two repos communicate over an internal HTTPS surface carrying mTLS + signed JWT + HMAC body signature on every call. See [ADR-0019](../LearnStack/docs/decisions/0019-learnstack-hub.md) and [ADR-0034](../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md).

Hub **never** stores tenant content. Hub holds tenant _metadata_ (plan, subscription, license, custom domain, compliance caps); tenant _data_ (courses, lessons, learners, enrollments, classroom sessions) lives exclusively inside LearnStack core. This separation is enforced by the `Hub_NeverStores_TenantData` architecture test.

## What state this is in

**P02c-0 (Repository bootstrap)** ✅ — solution scaffold, frontend monorepo, compose stack, CI, and the docs skeleton are in place. No Hub domain code is on `main`.

**P02c-1 is frozen by owner decision (2026-08-08).** The branch `feat/phase-02c-packet-1-hub-domain-core` exists and carries the Hub domain core, but it is not merged. It was written against the pre-restructure corpus, and three decisions have moved underneath it: [ADR-0033](../LearnStack/docs/decisions/0033-audit-durability-model.md) (audit durability), [ADR-0034](../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md) (the real endpoint set, plus host-mapping and TLS key delivery), and [ADR-0035](../LearnStack/docs/decisions/0035-demand-gated-infrastructure.md) (which makes the Hub a demand-gated integration rather than a Phase-02a prerequisite).

**It unfreezes when both hold:**

1. The [ADR-0035](../LearnStack/docs/decisions/0035-demand-gated-infrastructure.md) trigger for `IEntitlementProvider` fires — **a tenant must be billed or plan-gated**. Until then LearnStack runs on `NullEntitlementProvider` and needs nothing from the Hub.
2. The branch is reconciled with ADR-0033 and ADR-0034 — the endpoint set, the `host-mappings` path, the entitlement read path, and the audit model.

Until then, work in this repo is documentation and planning only. Merging Hub domain code against a superseded contract would put the cross-repo snapshot tests out of step in both directions.

## Roadmap

**This repository owns the Hub plan.** [`docs/roadmap/`](docs/roadmap/README.md) holds a document per packet — Goal, Scope, Deliverables, Completion Criteria, Risks, Phase Exit Decision — not a status mirror of a LearnStack file. LearnStack's [phase-02c](../LearnStack/docs/roadmap/phase-02c-hub-foundation.md) covers only LearnStack's side of the boundary and points here for the rest.

| Packet | Document | State |
| ------ | -------- | ----- |
| P02c-0 — Repository bootstrap | [p02c-0-repository-bootstrap.md](docs/roadmap/p02c-0-repository-bootstrap.md) | ✅ Shipped |
| P02c-1 — Hub Domain Core (`LearnStackTenant`, `Plan`, `HubSubscription`, `Entitlement`) | [p02c-1-hub-domain-core.md](docs/roadmap/p02c-1-hub-domain-core.md) | ❄️ Frozen — branch exists, not merged |
| P02c-2 — Hub-side internal API + outbound `LearnStackApiClient` | [p02c-2-internal-api-and-contract.md](docs/roadmap/p02c-2-internal-api-and-contract.md) | ⏳ |
| P02c-3 — LearnStack-side integration (`HubEntitlementProvider`, `IUsageReporter`, internal-API handlers) | [p02c-3-learnstack-integration.md](docs/roadmap/p02c-3-learnstack-integration.md) | ⏳ |
| P02c-4 — Operator portal MVP | [p02c-4-operator-portal.md](docs/roadmap/p02c-4-operator-portal.md) | ⏳ |
| P02c-5 — Custom domain lifecycle | [p02c-5-custom-domain-lifecycle.md](docs/roadmap/p02c-5-custom-domain-lifecycle.md) | ⏳ |
| P02c-6 — License key skeleton | [p02c-6-license-key.md](docs/roadmap/p02c-6-license-key.md) | ⏳ |
| P02c-7 — End-to-end exit gate | [p02c-7-exit-gate.md](docs/roadmap/p02c-7-exit-gate.md) | ⏳ |

Post-02c Hub tracks — [hub-billing.md](docs/roadmap/hub-billing.md) and [hub-marketplace.md](docs/roadmap/hub-marketplace.md) — also live here; LearnStack's `phase-09b` and `phase-12` are pointers at those documents.

The `P02c-N` identifiers are load-bearing across both repositories (branch names, PR titles, architecture-test registrations, cross-repo blocking tables). Do not renumber them.

## Where to start

For any task in this repo, read in this order:

1. [README.md](README.md) — direction at a glance.
2. [ADR-0019 LearnStack Hub](../LearnStack/docs/decisions/0019-learnstack-hub.md) — why the Hub is a separate repository.
3. [ADR-0034 Hub Contract Surface Invariant](../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md) — the two invariants and the real endpoint set.
4. [Architecture 24 LearnStack Hub](../LearnStack/docs/architecture/24-learnstack-hub.md) — the deep dive.
5. [Standards 20 § Hub HTTPS Contract Surface](../LearnStack/docs/standards/20-infrastructure-stack.md) — the rule as written for reviewers.
6. [docs/roadmap/README.md](docs/roadmap/README.md) — the Hub plan, owned here.
7. [docs/architecture/contract-with-learnstack.md](docs/architecture/contract-with-learnstack.md) — pointers to LearnStack-side authority.
8. [docs/glossary.md](docs/glossary.md) — Hub-specific terms.

Then pick an entry-point skill from **this repo's** catalogue at [`.claude/skills/`](.claude/skills/README.md). Hub maintains its own Hub-tailored skill set — the `add-hub-*` workflows encode Hub's deltas from LearnStack core (no RLS, `OperatorId` not `UserId`, the 6-step MediatR pipeline, the `hub` schema, the `learnstack_hub` database). The entry point for substantive work is [implement-task](.claude/skills/implement-task/SKILL.md); for scoping-only use [start-task](.claude/skills/start-task/SKILL.md); for review run [standards-check](.claude/skills/standards-check/SKILL.md) then [code-review](.claude/skills/code-review/SKILL.md).

> Hub skills are project-local: an agent running from the `LearnStack-Hub` root loads them from `.claude/skills/`. They cite LearnStack core's standards / ADRs by sibling path (`../LearnStack/docs/...`) for the cross-cutting authority and carry only the Hub-specific workflow on top — they do not duplicate the standards.

## Hard rules

These rules are **non-negotiable** for any change in this repo:

- **No tenant content tables.** Hub schema must NOT contain `course`, `lesson`, `user` (tenant users — operator users are separate), `enrollment`, `live_session`, `lesson_item`, `media_asset`, or any tenant data table. Architecture test `Hub_NeverStores_TenantData` enforces. This is invariant 1 of [ADR-0034](../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md).
- **No imports from LearnStack core internals.** Hub modules may reference **only** LearnStack `Application.Contracts` DTOs (and even those are kept as local copies in this repo until a shared NuGet package is introduced — Phase 11). LearnStack `Domain` / `Infrastructure` / `Modules.*` types are off-limits. Architecture test `Hub_Modules_DoNotReference_LearnStack_Internals` enforces.
- **The contract surface is governed by two invariants, not by a count** ([ADR-0034](../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md)). The old "closed at four endpoints" rule was never true — the corpus enumerated six paths while claiming four, and protecting the number is what pushed TLS private keys into the entitlement payload. The rules that replace it:
  1. **The Hub stores no tenant content.**
  2. **Every LearnStack↔Hub crossing goes through a named adapter** — `IEntitlementProvider`, `IUsageReporter`, `IHubTenantSync`. No other type may hold a Hub client, and nothing resolves a host by calling the Hub.

  Adding an endpoint still requires a new ADR filed in `../LearnStack/docs/decisions/`, not here — the surface is a cross-repository contract and both repositories have to agree. ADR-0034 carries the authoritative endpoint table.
- **TLS certificates and private keys never travel in the entitlement payload.** Host mappings go through `PUT /api/internal/tenants/{id}/host-mappings`; cert material moves between the Hub-owned and LearnStack-owned secret stores by secret-store replication and is referenced from the host-mapping payload **by path, never by value**. [ADR-0022 Amendment 1](../LearnStack/docs/decisions/0022-custom-domain-tls.md)'s step 3 is superseded by [ADR-0034](../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md).
- **`/api/internal/*` endpoints are NOT internet-exposed.** They are bound to an internal listener (or a dedicated APISIX route guarded by mTLS SSL-object). Architecture test `Internal_API_Endpoints_AreNot_Public` enforces.
- **Hub never writes to LearnStack's K8s state.** Cert + route propagation flows through published events and the internal-API pushes. Hub never holds Kubernetes credentials on LearnStack's cluster — the guarantee [ADR-0022 Amendment 1](../LearnStack/docs/decisions/0022-custom-domain-tls.md) exists to protect, and the one part of it ADR-0034 leaves unchanged.
- **Two-realm Keycloak boundary.** Hub authenticates against the `learnstack-hub` realm only. The `learnstack` realm is rejected on Hub endpoints; the `learnstack-hub` realm is rejected on LearnStack tenant-facing endpoints.
- **Modular monolith.** Hub follows the same module boundary rules as LearnStack — no cross-module Domain dependencies; cross-module communication through Application.Contracts or integration events.
- **English documentation.** All docs in English per [ADR-0007](../LearnStack/docs/decisions/0007-documentation-language-and-conventions.md).
- **Mermaid for diagrams.** Fenced ` ```mermaid ` blocks; remain readable as text for renderers that don't support Mermaid.
- **Single source of truth.** Cross-cutting architectural decisions live in `../LearnStack/docs/decisions/`; Hub-internal-only decisions live in `docs/decisions/` here with the `HUB-NNNN` numbering series. The **Hub roadmap** is the exception that runs the other way: it is owned here, and LearnStack links to it.

## Things to never do

- Import `LearnStack.Domain`, `LearnStack.Infrastructure`, or `LearnStack.Modules.*` types into this repo. Use local DTO copies of `LearnStack.Application.Contracts` until a shared NuGet package is introduced.
- Add `course`, `lesson`, `enrollment`, `user` (tenant), `live_session`, or any other tenant-content table to the Hub schema.
- Bind `/api/internal/*` endpoints to the internet-facing listener.
- Hold Kubernetes credentials on LearnStack's cluster from Hub code. Route propagation goes through `PUT /api/internal/tenants/{id}/host-mappings`; cert material goes through secret-store replication.
- Import Stripe / Iyzico SDK types outside `LearnStack.Hub.Infrastructure.Stripe` / `.Iyzico` adapter projects (those don't exist yet — [hub-billing.md](docs/roadmap/hub-billing.md) creates them).
- Import `Sentry.SentrySdk` from a module assembly. Error capture goes through `IErrorTrackingProvider` (P02c-1+).
- Add an endpoint to the LearnStack↔Hub contract surface without a new ADR in `../LearnStack/docs/decisions/`, landed in both repositories.
- Carry TLS certificates or private keys in any payload LearnStack caches, logs, audits or mirrors — including the entitlement projection.
- Serve a host lookup from the Hub. `IHostToTenantResolver` on the LearnStack side reads `platform_host_to_tenant` and nothing else; a Hub outage must not take tenant public pages down.
- Merge Hub domain code while P02c-1 is frozen. See [What state this is in](#what-state-this-is-in) for the two conditions that unfreeze it.
- Bypass `WebhookLedger` for Stripe / Iyzico webhook processing. Idempotency unique constraint enforces.
- Throw `DomainException` for expected business-rule violations — return `Result.Fail(business_rule_violation, ...)` instead (mirrors LearnStack [ADR-0032 § Sub-decision 4](../LearnStack/docs/decisions/0032-exception-handling-logging-and-observability.md)).
- Edit an Accepted ADR's decision section in either repo. Write a new ADR that supersedes it.
- Reuse an ADR number.

## Conventions when editing code

Hub follows LearnStack's engineering standards by reference unless explicitly overridden by a Hub-internal ADR:

- C# / .NET 10, strongly-typed IDs via Vogen, records, MediatR pipeline (P02c-1), EF Core with per-module `DbContext`.
- TypeScript strict + Next.js App Router for the operator portal (`frontend/apps/operator-portal`).
- REST + RFC 7807 Problem Details + cursor pagination + idempotency keys.
- OpenTelemetry + correlation ID end to end. P02c-1 wires the Hub's mirror of the LearnStack P02a-3 cross-cutting foundation — with **six** pipeline steps, not eight: Hub has no `TenantContextBehavior`, because Hub data is operator-administered rather than tenant-isolated.
- Permission keys `{module}.{resource}.{action}` with closed action set; operator-scope permissions only (LearnStack handles tenant permissions).

## Commit conventions

- Conventional Commits: `type(scope): subject`. Subject in imperative mood; ≤ 72 chars.
- Hub-specific scopes: `hub`, `hub-portal`, `hub-domain`, `hub-infra`, `hub-docs`.
- Commits made with AI assistance carry the trailer:
  `Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>`

## Cross-repo coordination

When a packet changes both repos (e.g., P02c-3 lands the LearnStack-side internal-API handlers + Hub-side outbound client), the two PRs must be **coordinated**:

1. Hub-side PR opens first; it carries the canonical contract shape.
2. LearnStack-side PR references the Hub-side PR's commit hash.
3. Both PRs merge in the same session; either-side merge alone leaves the contract dangling.

The wire shape is pinned by a checked-in `entitlement-v1.schema.json` and a snapshot test in **each** repository, per [ADR-0034](../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md). Both snapshots move together or the contract has drifted.

On-disk sibling layout is `LearnStack/` and `LearnStack-Hub/` — with those exact capitalisations. Links written as `../LearnStack/...` resolve only on case-insensitive filesystems, and the CI link audit deliberately skips cross-repo links, so it will not catch the mistake for you.
