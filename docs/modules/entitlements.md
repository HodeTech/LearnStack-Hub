# Module: Entitlements

`LearnStack.Hub.Modules.Entitlements` owns the **entitlement projection** — the flattened, denormalised read model of `Plan` + `HubSubscription` (+ `CompliancePolicy`, from P02c-5) per tenant. It is the single shape LearnStack core consumes across the Hub HTTPS contract surface.

The full design lives in [../architecture/entitlement-projection.md](../architecture/entitlement-projection.md) — this file is the module-level summary. Read the architecture doc for the recompute algorithm, the wire-format contract, and the `generation` semantics.

Authoritative sources: [ADR-0021 Feature-Based Entitlement](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md), [Architecture 24 § 4](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md), [ADR-0020](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md).

## Aggregate: `Entitlement`

`Entitlement : Entity<LearnStackTenantId>` — note `Entity`, **not** `AuditableEntity`. There is exactly one row per tenant (PK = tenant id), replaced wholesale on recompute. It is not soft-deletable and does not carry the audit-column set; its "audit trail" is the monotonic `generation` + `updated_at`.

| Column            | Type             | Notes                                                           |
| ----------------- | ---------------- | --------------------------------------------------------------- |
| `tenant_id`       | uuid PK          | 1:1 with `LearnStackTenant`; PK is the tenant id (no surrogate) |
| `tier`            | text             | mirrors current `Plan.tier`                                     |
| `features`        | jsonb            | `Dictionary<string,bool>`                                       |
| `limits`          | jsonb            | `Dictionary<string,long>`                                       |
| `compliance_caps` | jsonb            | `{ caps: {...} }`; **empty `{}` in P02c-1**                     |
| `expires_at`      | timestamptz null | from `subscription.current_period_end`                          |
| `grace_until`     | timestamptz null | null in P02c-1                                                  |
| `generation`      | bigint           | **monotonic**, starts at 1, +1 per recompute                    |
| `updated_at`      | timestamptz      | last recompute (`IClock.UtcNow`)                                |

`Entitlement.Recompute(tier, features, limits, complianceCaps, expiresAt, graceUntil, clock)` — replaces the projection fields, bumps `generation` by exactly 1, sets `updated_at`. A unit test asserts `generation` strictly increases and never resets.

## Projection service

`EntitlementProjectionService` (in `...Entitlements.Application`) is the orchestrator that the Subscriptions / Plans / TenantLifecycle handlers call after a state change. Algorithm + triggers: [../architecture/entitlement-projection.md § Recompute algorithm](../architecture/entitlement-projection.md).

It reads Plan + Subscription via **Application.Contracts calls** into those modules (never their Domain types). It writes the `Entitlement` row in its own DbContext transaction (the `TransactionBehavior` covers commit/rollback).

In P02c-1 the service's final step — publishing `learnstack.hub.entitlement` via `IOutbox` — is a **no-op shell**. The Dapr publish + the outbound `LearnStackApiClient` push land in **P02c-2**. P02c-1's deliverable is the correct in-process recompute + persist + monotonic generation.

## Commands + queries (P02c-1)

`Application.Contracts`:

- `RecomputeEntitlementCommand { TenantId }` → `Result<EntitlementProjectionDto>` — the public entry the other modules call.
- `GetEntitlementQuery { TenantId }` → `Result<EntitlementProjectionDto>` — read the current projection (this is the shape `POST /api/v1/internal/license/verify` will serve in P02c-2).

`EntitlementProjectionDto` is the serialisation target matching the wire contract in [../architecture/entitlement-projection.md § Projection shape](../architecture/entitlement-projection.md). Its System.Text.Json shape **is** the contract — treat changes to it as contract changes.

## DbContext

`EntitlementsDbContext` — `hub` schema, table `entitlements`. `HasDefaultSchema("hub")`. JSONB columns for `features` / `limits` / `compliance_caps`. PK = `tenant_id` (the `LearnStackTenantId` Vogen converter registered). No RLS.

## Architecture / contract tests (recommended in P02c-1)

- `EntitlementProjection_Shape_IsStable` ([ADR-0021 § Architecture tests](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md)) — snapshot-test the serialised `EntitlementProjectionDto` JSON against a checked-in `entitlement-v1.schema.json` under `LearnStack.Hub.Tests.Contract`. This is the contract guard; land it here because P02c-1 is where the projection serialiser is born.
- Unit test: `generation` strictly increases across successive `Recompute` calls; starts at 1.
- Integration test (Testcontainers, lights up the `backend-integration` CI job): create tenant → trial subscription → recompute → assert `Entitlement` row exists with generation 1, correct tier/features/limits; change plan → recompute → assert generation 2 + updated fields.

## Audit coverage

Recompute is a system operation, not an operator action — it is **not** in the operator-audit MUST list (the operator actions that _trigger_ recompute — plan change, subscription change — are audited in their own modules). `GetEntitlementQuery` is MAY.

## Out of scope for P02c-1

- `learnstack.hub.entitlement` Dapr publish + `IOutbox` (P02c-2).
- The `POST /api/v1/internal/license/verify` endpoint that serves this projection to LearnStack core (P02c-2).
- `compliance_caps` content (P02c-5).
- `grace_until` / license-driven expiry (P02c-6).
