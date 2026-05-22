# Working in this repository

This file is read first by Claude Code (and any other agent following the convention). It tells you what this project is, what state it is in, and the conventions you must follow when contributing.

## What this is

**LearnStack Hub** is the control plane companion to [LearnStack core](../learnstack) — a separate codebase that owns tenant lifecycle, subscription / plan / billing, license issuance, entitlement projection, custom-domain administration, compliance caps, and the operator portal. The two repos communicate through a **closed four-endpoint HTTPS contract surface** with mTLS + signed JWT + HMAC body signature on every call. See [ADR-0019](../learnstack/docs/decisions/0019-learnstack-hub.md).

Hub **never** stores tenant content. Hub holds tenant _metadata_ (plan, subscription, license, custom domain, compliance caps); tenant _data_ (courses, lessons, learners, enrollments, classroom sessions) lives exclusively inside LearnStack core. This separation is enforced by the `Hub_NeverStores_TenantData` architecture test.

## What state this is in

**Phase 02c — Repository Bootstrap (P02c-0)** ✅. Solution scaffold + frontend monorepo + compose stack + CI + docs skeleton are in place. No Hub domain code yet — that lands in P02c-1 (Hub Domain Core).

| Packet                                                                                                                                   | State          |
| ---------------------------------------------------------------------------------------------------------------------------------------- | -------------- |
| P02c-0 — Repository bootstrap                                                                                                            | ✅ this commit |
| P02c-1 — Hub Domain Core (`LearnStackTenant`, `Plan`, `HubSubscription`, `Entitlement`)                                                  | ⏳ next        |
| P02c-2 — Hub-side internal API + outbound `LearnStackApiClient`                                                                          | ⏳             |
| P02c-3 — LearnStack core PR (`HubEntitlementProvider`, `IUsageReporter`, internal-API handlers) — **blocked on LearnStack P02a-5/6/7/9** | ⏳             |
| P02c-4 — Operator portal MVP                                                                                                             | ⏳             |
| P02c-5 — Custom domain lifecycle                                                                                                         | ⏳             |
| P02c-6 — License key skeleton                                                                                                            | ⏳             |
| P02c-7 — End-to-end exit gate                                                                                                            | ⏳             |

## Where to start

For any task in this repo, read in this order:

1. [README.md](README.md) — direction at a glance.
2. [ADR-0019 LearnStack Hub](../learnstack/docs/decisions/0019-learnstack-hub.md) — the boundary contract.
3. [Architecture 24 LearnStack Hub](../learnstack/docs/architecture/24-learnstack-hub.md) — the deep dive.
4. [Standards 20 § Hub HTTPS Contract Surface](../learnstack/docs/standards/20-infrastructure-stack.md) — the four-endpoint rule.
5. [docs/roadmap/README.md](docs/roadmap/README.md) — P02c packet plan.
6. [docs/architecture/contract-with-learnstack.md](docs/architecture/contract-with-learnstack.md) — pointers to LearnStack-side authority.
7. [docs/glossary.md](docs/glossary.md) — Hub-specific terms.

Then pick an entry-point skill from **this repo's** catalogue at [`.claude/skills/`](.claude/skills/README.md). Hub maintains its own Hub-tailored skill set — the `add-hub-*` workflows encode Hub's deltas from LearnStack core (no RLS, `OperatorId` not `UserId`, the 6-step MediatR pipeline, the `hub` schema, the `learnstack_hub` database). The entry point for substantive work is [implement-task](.claude/skills/implement-task/SKILL.md); for scoping-only use [start-task](.claude/skills/start-task/SKILL.md); for review run [standards-check](.claude/skills/standards-check/SKILL.md) then [code-review](.claude/skills/code-review/SKILL.md).

> Hub skills are project-local: an agent running from the `learnstack-hub` root loads them from `.claude/skills/`. They cite LearnStack core's standards / ADRs by sibling path (`../learnstack/docs/...`) for the cross-cutting authority and carry only the Hub-specific workflow on top — they do not duplicate the standards.

## Hard rules

These rules are **non-negotiable** for any change in this repo:

- **No tenant content tables.** Hub schema must NOT contain `course`, `lesson`, `user` (tenant users — operator users are separate), `enrollment`, `live_session`, `lesson_item`, `media_asset`, or any tenant data table. Architecture test `Hub_NeverStores_TenantData` enforces.
- **No imports from LearnStack core internals.** Hub modules may reference **only** LearnStack `Application.Contracts` DTOs (and even those are kept as local copies in this repo until a shared NuGet package is introduced — Phase 11). LearnStack `Domain` / `Infrastructure` / `Modules.*` types are off-limits. Architecture test `Hub_Modules_DoNotReference_LearnStack_Internals` enforces.
- **The Hub HTTPS contract surface is closed at four endpoints.** Adding a fifth requires a new ADR (filed in `learnstack/docs/decisions/`, not here, since the surface is LearnStack-facing).
- **`/api/internal/*` endpoints are NOT internet-exposed.** They are bound to an internal listener (or a dedicated APISIX route guarded by mTLS SSL-object). Architecture test `Internal_API_Endpoints_AreNot_Public` enforces.
- **Hub never writes to LearnStack's K8s state.** Cert + route propagation flows through Dapr pub/sub events + the `PUT /api/internal/tenants/{id}/entitlements` push (per [ADR-0022 Amendment 1](../learnstack/docs/decisions/0022-custom-domain-tls.md)). Hub never holds Kubernetes credentials on LearnStack's cluster.
- **Two-realm Keycloak boundary.** Hub authenticates against the `learnstack-hub` realm only. The `learnstack` realm is rejected on Hub endpoints; the `learnstack-hub` realm is rejected on LearnStack tenant-facing endpoints.
- **Modular monolith.** Hub follows the same module boundary rules as LearnStack — no cross-module Domain dependencies; cross-module communication through Application.Contracts or integration events.
- **English documentation.** All docs in English per [ADR-0007](../learnstack/docs/decisions/0007-documentation-language-and-conventions.md).
- **Mermaid for diagrams.** Fenced ` ```mermaid ` blocks; remain readable as text for renderers that don't support Mermaid.
- **Single source of truth.** Cross-cutting architectural decisions live in `learnstack/docs/decisions/`; Hub-internal-only decisions live in `docs/decisions/` here with the `HUB-NNNN` numbering series.

## Things to never do

- Import `LearnStack.Domain`, `LearnStack.Infrastructure`, or `LearnStack.Modules.*` types into this repo. Use local DTO copies of `LearnStack.Application.Contracts` until a shared NuGet package is introduced.
- Add `course`, `lesson`, `enrollment`, `user` (tenant), `live_session`, or any other tenant-content table to the Hub schema.
- Bind `/api/internal/*` endpoints to the internet-facing listener.
- Hold Kubernetes credentials on LearnStack's cluster from Hub code. Cert + route propagation goes through Dapr events and the entitlement-push internal API.
- Import Stripe / Iyzico SDK types outside `LearnStack.Hub.Infrastructure.Stripe` / `.Iyzico` adapter projects (those don't exist yet — Phase 09b creates them).
- Import `Sentry.SentrySdk` from a module assembly. Error capture goes through `IErrorTrackingProvider` (Phase 02c-1+).
- Add a fifth endpoint to the Hub HTTPS contract surface — requires a new ADR in `learnstack/docs/decisions/`.
- Bypass `WebhookLedger` for Stripe / Iyzico webhook processing (Phase 09b). Idempotency unique constraint enforces.
- Throw `DomainException` for expected business-rule violations — return `Result.Fail(business_rule_violation, ...)` instead (mirrors LearnStack [ADR-0032 § Sub-decision 4](../learnstack/docs/decisions/0032-exception-handling-logging-and-observability.md)).
- Edit an Accepted ADR's decision section in either repo. Write a new ADR that supersedes it.
- Reuse an ADR number.

## Conventions when editing code

Hub follows LearnStack's engineering standards by reference unless explicitly overridden by a Hub-internal ADR:

- C# / .NET 10, strongly-typed IDs via Vogen, records, MediatR pipeline (Phase 02c-1), EF Core with per-module `DbContext`.
- TypeScript strict + Next.js App Router for the operator portal.
- REST + RFC 7807 Problem Details + cursor pagination + idempotency keys.
- OpenTelemetry + correlation ID end to end (Phase 02c-1 wires the same cross-cutting foundation as LearnStack core P02a-3).
- Permission keys `{module}.{resource}.{action}` with closed action set; operator-scope permissions only (LearnStack handles tenant permissions).

## Commit conventions

- Conventional Commits: `type(scope): subject`. Subject in imperative mood; ≤ 72 chars.
- Hub-specific scopes: `hub`, `hub-portal`, `hub-domain`, `hub-infra`, `hub-docs`.
- Commits made with AI assistance carry the trailer:
  `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>`

## Cross-repo coordination

When a packet changes both repos (e.g., P02c-3 lands the LearnStack-side internal-API handlers + Hub-side outbound client), the two PRs must be **coordinated**:

1. Hub-side PR opens first; it carries the canonical contract shape.
2. LearnStack-side PR references the Hub-side PR's commit hash.
3. Both PRs merge in the same session; either-side merge alone leaves the contract dangling.

Adding or changing a cross-repo contract endpoint requires a new ADR in `learnstack/docs/decisions/` (per Hub HTTPS Contract Surface rule).
