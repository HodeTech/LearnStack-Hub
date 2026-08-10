# Hub entitlement projection

The `Entitlement` aggregate is the **single most load-bearing thing Hub produces**: a flattened, denormalised projection of `Plan` + `HubSubscription` (+ `CompliancePolicy`, from P02c-5) per tenant. It is the only shape LearnStack core's `HubEntitlementProvider` consumes, and the only shape pushed across the `PUT /api/internal/tenants/{id}/entitlements` contract.

Authoritative sources: [ADR-0021 Feature-Based Entitlement](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md) (+ Amendment 1), [Architecture 24 § 4 Entitlement projection](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md), [ADR-0020 Triple Deployment + Hybrid License](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md).

## Projection shape (the wire contract)

This is the JSON the projection serialises to (per [Architecture 24 § 4](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md)). It is the contract LearnStack core mirrors into `platform_entitlement_cache`:

```json
{
  "tenant_id": "tenant-uuid",
  "tier": "growth",
  "features": {
    "classroom.recording": true,
    "tenancy.custom_domain": true,
    "tenancy.white_label_branding": true,
    "customization.unlimited_content_types": true,
    "identity.sso.saml": false,
    "analytics.advanced_reporting": false,
    "integrations.api_access": true,
    "integrations.webhooks": true,
    "audit.export": true
  },
  "limits": {
    "limits.max_users": 500,
    "limits.max_organizations": 10,
    "limits.classroom_minutes_per_month": 50000,
    "limits.recording_storage_gb": 500,
    "limits.media_storage_gb": 1000,
    "limits.media_bandwidth_gb_per_month": 1000,
    "limits.api_rate_per_minute": 6000,
    "limits.max_custom_content_types": -1,
    "limits.max_page_block_definitions": -1
  },
  "compliance": {
    "caps": {
      "gdpr.hard_delete.enabled": { "allowed": true, "forced": false },
      "audit.retention.days": { "allowed": true, "forced": true, "value": 365 },
      "data.residency.region": {
        "allowed": false,
        "forced": true,
        "value": "eu-west"
      }
    }
  },
  "expires_at": "2027-05-18T00:00:00Z",
  "grace_until": null,
  "generation": 42
}
```

### Key-shape rules (load-bearing; do not drift)

- **Feature keys** use the dotted snake_case form with **no `.enabled` suffix** (dropped in [ADR-0021 Amendment 1](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md)). Every feature is implicitly boolean. e.g. `classroom.recording`, `tenancy.custom_domain`, `identity.sso.saml`.
- **Limit keys** carry the `limits.` prefix; value `-1` = unlimited, `0` = not available. e.g. `limits.max_users`.
- **Compliance-cap keys** keep their own `.enabled` portion as part of the cap name (it is NOT a redundant suffix), and the value is a `{ allowed, forced, value? }` object — not a bare bool.
- The `tier` mirrors the `Plan.tier` (`starter | growth | scale | enterprise | custom`).

P02c-1 stores `features` / `limits` / `compliance_caps` as **JSONB columns** on the `entitlements` table. The projection serialiser produces exactly the JSON above.

## `Entitlement` aggregate (P02c-1)

Per [Architecture 24 § 2 ERD](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md):

| Column            | Type             | Notes                                                                                                              |
| ----------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------ |
| `tenant_id`       | uuid PK          | 1:1 with `LearnStackTenant`; the PK is the tenant id (no separate surrogate)                                       |
| `tier`            | text             | mirrors current `Plan.tier`                                                                                        |
| `features`        | jsonb            | `Dictionary<string, bool>` serialised                                                                              |
| `limits`          | jsonb            | `Dictionary<string, long>` serialised                                                                              |
| `compliance_caps` | jsonb            | `{ caps: { <capKey>: { allowed, forced, value? } } }`; **empty `{}` in P02c-1** (CompliancePolicy lands in P02c-5) |
| `expires_at`      | timestamptz null | from subscription period / license                                                                                 |
| `grace_until`     | timestamptz null | null unless in grace                                                                                               |
| `generation`      | bigint           | **monotonic**; see below                                                                                           |
| `updated_at`      | timestamptz      | last recompute instant                                                                                             |

`Entitlement` inherits `Entity<TenantId>` (append-mostly; it is recomputed in place but is not a soft-deletable `AuditableEntity` — there is exactly one row per tenant, replaced wholesale on recompute). The aggregate exposes a `Recompute(...)` method that bumps `generation` and replaces the projection fields atomically.

## The `generation` counter — the cache-coherency primitive

`generation` is a **monotonic** `bigint`, incremented by exactly 1 on every recompute. It is how cache invalidation stays correct across the Hub → LearnStack boundary:

- Hub increments `generation` on every `Recompute`.
- The `learnstack.hub.entitlement` Dapr event carries `{ tenant_id, generation, expires_at }`.
- LearnStack core's cache accepts a pushed projection only if `received.generation >= cached.generation` — a late / out-of-order delivery carrying an older generation is rejected, preventing stale-overwrite.

Rules:

- `generation` starts at **1** on tenant creation (initial entitlement).
- It **only ever increments**, never resets or decrements — even across plan downgrades.
- The increment + the field replacement happen in the **same transaction** (the `TransactionBehavior` covers this).

## Recompute algorithm

`EntitlementProjectionService.Recompute(TenantId)` (in `LearnStack.Hub.Modules.Entitlements.Application`):

```
INPUT:  tenantId
STEPS:
  1. Load the tenant's HubSubscription (Subscriptions module, via Application.Contracts call).
  2. Load the bound Plan (Plans module, via Application.Contracts call).
  3. Load the tenant's CompliancePolicy rows (Compliance module) —
     in P02c-1 this returns EMPTY; compliance.caps = {}.
  4. Compose:
       tier            = plan.tier
       features        = plan.features                 (Dictionary<string,bool>)
       limits          = plan.limits                   (Dictionary<string,long>)
       compliance.caps = merge(plan.compliance_defaults, tenant.compliance_overrides)
                         → P02c-1: {} (no plan compliance defaults wired yet either;
                            keep the column shape ready, value empty)
       expires_at      = subscription.current_period_end (or null for perpetual)
       grace_until     = null in P02c-1 (grace logic lands with license/dunning)
  5. Load existing Entitlement row (if any).
       generation_next = existing is null ? 1 : existing.generation + 1
  6. Upsert Entitlement (tenant_id PK) with the composed fields + generation_next + updated_at = clock.UtcNow.
  7. P02c-2: enqueue learnstack.hub.entitlement integration event via IOutbox.
     P02c-1: the publish is a no-op shell; the in-process recompute + persist is the deliverable.
RETURN: Result<EntitlementProjection>
```

### When recompute fires (P02c-1 triggers)

Per [Architecture 24 § 4 Recompute rule](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md), recompute fires on:

| Trigger                                                                              | P02c-1?   | Mechanism                                                                                   |
| ------------------------------------------------------------------------------------ | --------- | ------------------------------------------------------------------------------------------- |
| Tenant creation                                                                      | ✅        | `LearnStackTenant` created → initial `HubSubscription` (Trial) → recompute (generation = 1) |
| `HubSubscription` state change (upgrade / downgrade / cancel / renew / trial→active) | ✅        | Subscriptions handler calls the Entitlements projection service                             |
| `Plan` definition change                                                             | ✅        | Plans handler triggers recompute for **every** subscription bound to that plan              |
| `CompliancePolicy` change                                                            | ⏳ P02c-5 | not wired in P02c-1                                                                         |
| `LicenseKey` re-issuance                                                             | ⏳ P02c-6 | not wired in P02c-1                                                                         |

The cross-module trigger (Subscriptions / Plans → Entitlements) uses an in-process Application-contract call or an intra-module domain event handled in the same transaction — see [module-topology.md § P02c-1 cross-module flow](module-topology.md). The **Dapr publish** of `learnstack.hub.entitlement` is **stubbed** in P02c-1 (it lands in P02c-2 with `IOutbox` + the outbound `LearnStackApiClient`).

## Fallback / degraded semantics (context — not P02c-1 work)

These are LearnStack-core-side behaviours that consume the projection; documented here so the Hub-side shape stays compatible. They are **not** P02c-1 deliverables:

- `NullEntitlementProvider` (LearnStack Development mode): all features `true`, all limits `null`.
- `HubEntitlementProvider` + Hub unreachable: the read path is normative and lives on
  the LearnStack side — L1 in-process cache → L2 distributed cache → the durable
  `platform_entitlement_cache` row carrying its own grace window → the Hub. Past the
  grace window, resolution is per feature-key class: fail-open keys stay enabled,
  fail-closed keys are refused. It never throws out of a feature-flag check. See
  [ADR-0034 § The entitlement read path](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md)
  and [LearnStack Phase 02c](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-02c-hub-foundation.md); this document
  does not restate them.

Hub's only obligation is to keep emitting a projection whose shape matches the contract above, with a correct monotonic `generation`.

## Architecture-test hooks

- `EntitlementProjection_Shape_IsStable` (from [ADR-0021 § Architecture tests](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md)) — snapshot-test the serialised projection JSON against a checked-in `entitlement-v1.schema.json`. A breaking change requires a schema-version bump. Recommended to land this in P02c-1 since the projection serialiser is the contract surface.
- `generation` monotonicity is not an architecture test (it's a runtime invariant) — cover it with a unit test on the `Entitlement.Recompute` method (generation strictly increases) and an integration test on the projection service.
