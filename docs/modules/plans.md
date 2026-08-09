# Module: Plans

`LearnStack.Hub.Modules.Plans` owns the **plan catalogue** — the set of plans operators author, each carrying the feature toggles, numeric limits, and (later) compliance defaults that a subscription projects into a tenant's entitlement.

Authoritative sources: [ADR-0021 Feature-Based Entitlement](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md) (+ Amendment 1), [Architecture 24 § 2 ERD + § 8 plan tiers](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md).

## Aggregate: `Plan`

`Plan : AuditableEntity<PlanId>`.

| Column                | Type          | Notes                                                                                               |
| --------------------- | ------------- | --------------------------------------------------------------------------------------------------- |
| `id`                  | uuid PK       |                                                                                                     |
| `name`                | text          | e.g. "Growth Monthly"                                                                               |
| `tier`                | text          | `PlanTier` enum: `starter \| growth \| scale \| enterprise \| custom`; `ck_plans_tier`              |
| `features`            | jsonb         | `Dictionary<string, bool>` — keys from the `FeatureKey` registry (no `.enabled` suffix)             |
| `limits`              | jsonb         | `Dictionary<string, long>` — keys from the `LimitKey` registry; `-1` = unlimited, `0` = unavailable |
| `compliance_defaults` | jsonb         | `{ <capKey>: { allowed, forced, value? } }`; **empty `{}` in P02c-1** (Compliance lands P02c-5)     |
| `base_price_usd`      | numeric(10,2) |                                                                                                     |
| `billing_cycle`       | text          | `monthly \| annual`; `ck_plans_billing_cycle`                                                       |
| `currency`            | text          | ISO 4217, default `USD`                                                                             |
| `is_active`           | bool          | inactive plans can't be newly subscribed; existing subscriptions keep theirs                        |
| audit columns         |               | from `AuditableEntity`, `*_by` = `OperatorId`                                                       |

Strongly-typed id: `PlanId` — `[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]`.

## Feature / Limit key registries

Hub mirrors LearnStack core's typed `FeatureKey` / `LimitKey` value objects ([ADR-0021 Amendment 1](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md)):

```csharp
public readonly record struct FeatureKey(string Value);
public readonly record struct LimitKey(string Value);
```

- These live in `LearnStack.Hub.SharedKernel.FeatureFlags` (mirror of `LearnStack.SharedKernel.FeatureFlags`).
- The `FeatureKeys` / `LimitKeys` static registries enumerate the known keys. **Hub is the authoring side** — the plan editor (P02c-4) writes these keys into `Plan.features` / `Plan.limits`. The wire-format strings (snake_case dotted, no `.enabled` suffix) must match LearnStack core's registry exactly so the projection LearnStack consumes lines up.
- A `Plan` validator checks that every key in `features` / `limits` is a known registry key — an unknown key is a `Result.Fail(validation_failed)`, not a silent accept. (This is the Hub-side analogue of LearnStack's `FeatureKey_AllReferences_AreInRegistry` architecture test; in Hub it's a runtime validator because keys arrive as data, not code references.)

> **Registry sync.** Because the two repos each keep their own copy of the key registries, they can drift. P02c-1 ships the Hub registry seeded from [ADR-0021 Amendment 1](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md) + [Architecture 24 § 4](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md). A future cross-repo reconciliation (or a shared `LearnStack.Contracts` package, Phase 11) is the durable fix. Note this in the Hub roadmap.

## Plan-change → entitlement recompute fan-out

A `Plan` definition change must recompute the entitlement of **every** subscription bound to that plan ([Architecture 24 § 4 Recompute rule](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md)). In P02c-1:

- `UpdatePlanCommand` handler, after persisting the plan change, asks the Subscriptions module (via Application.Contracts) for all subscription tenant-ids bound to the plan, then calls the Entitlements projection service per tenant.
- For a large fan-out this would be a background job (Hangfire) — but P02c-1 has no real tenant volume; an in-process loop is acceptable, with a TODO noting the Hangfire migration when volume warrants (Phase 09b / 11).

## Commands + queries (P02c-1)

`Application.Contracts`:

- `CreatePlanCommand { Name, Tier, Features, Limits, BasePriceUsd, BillingCycle, Currency }` → `Result<PlanDto>`.
- `UpdatePlanCommand { PlanId, ... }` → `Result` (triggers fan-out recompute).
- `DeactivatePlanCommand { PlanId }` → `Result`.
- `GetPlanQuery { PlanId }` → `Result<PlanDto>`.
- `ListPlansQuery { ActiveOnly?, Cursor, Limit }` → `Result<Page<PlanSummaryDto>>`.

Validators: tier in enum, billing cycle in enum, currency ISO 4217, every feature/limit key in registry, base price ≥ 0.

## DbContext

`PlansDbContext` — `hub` schema, table `plans`. `HasDefaultSchema("hub")`. JSONB columns for `features` / `limits` / `compliance_defaults`. No RLS (plans are global, operator-authored, not tenant-scoped).

## Seed data (P02c-1)

`scripts/seed.sh` (and/or an EF seed) provisions the four illustrative tiers from [Architecture 24 § 8](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md): Starter ($49), Growth ($199), Scale ($799), Enterprise (custom). These give P02c-1's tenant-creation flow a plan to bind to and exercise the projection. Keep them as **data**, not code constants.

## Audit coverage (formal matrix lands in P02c-4)

| Operation                         | Class | Snapshot                              |
| --------------------------------- | ----- | ------------------------------------- |
| `CreatePlanCommand`               | MUST  | after                                 |
| `UpdatePlanCommand`               | MUST  | before/after (features + limits diff) |
| `DeactivatePlanCommand`           | MUST  | before/after is_active                |
| `GetPlanQuery` / `ListPlansQuery` | MAY   | —                                     |

## Out of scope for P02c-1

- The operator plan-editor UI (P02c-4).
- `compliance_defaults` content — the column ships empty; CompliancePolicy + caps land in P02c-5.
- Hangfire-backed fan-out for large plan-change recomputes (Phase 09b / 11).
