# Module: Subscriptions

`LearnStack.Hub.Modules.Subscriptions` owns the **per-tenant binding to a plan** and the subscription lifecycle state machine. It is the second input (alongside `Plan`) to the entitlement projection.

Authoritative sources: [ADR-0019 § Hub data model](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0019-learnstack-hub.md), [Architecture 24 § 2 ERD + § 5 plan-upgrade sequence](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md), [ADR-0020 Triple Deployment + Hybrid License](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md).

## Aggregate: `HubSubscription`

`HubSubscription : AuditableEntity<HubSubscriptionId>`. One per tenant (1:1; `ux_subscriptions_tenant_id` unique).

| Column                     | Type             | Notes                                                                                                          |
| -------------------------- | ---------------- | -------------------------------------------------------------------------------------------------------------- |
| `id`                       | uuid PK          |                                                                                                                |
| `tenant_id`                | uuid FK          | → `tenants.id`; unique (1:1). Ordinary FK, **not** an RLS boundary.                                            |
| `plan_id`                  | uuid FK          | → `plans.id`; the currently bound plan                                                                         |
| `status`                   | text             | `SubscriptionStatus` enum (see below); `ck_subscriptions_status`                                               |
| `trial_start`              | timestamptz null |                                                                                                                |
| `trial_end`                | timestamptz null |                                                                                                                |
| `current_period_start`     | timestamptz      |                                                                                                                |
| `current_period_end`       | timestamptz      | feeds `Entitlement.expires_at`                                                                                 |
| `cancel_at_period_end`     | bool             | default false                                                                                                  |
| `payment_provider`         | text null        | `stripe \| iyzico`; **null in P02c-1** (billing is Phase 09b); `ck_subscriptions_payment_provider` allows null |
| `provider_subscription_id` | text null        | **null in P02c-1**                                                                                             |
| audit columns              |                  | from `AuditableEntity`, `*_by` = `OperatorId`                                                                  |

Strongly-typed id: `HubSubscriptionId` — `[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]`.

## State machine: `SubscriptionStatus`

```
   create (with trial) ──► Trial ──activate──► Active
                            │                   │
                            │ trial expires     │ payment fails (P09b dunning)
                            ▼                   ▼
                          Expired           PastDue ──cure──► Active
                                                │ cancel / unrecoverable
                                                ▼
                                            Canceled ──period end──► Expired
```

Values: `Trial | Active | PastDue | Canceled | Expired`.

P02c-1 implements the transitions that don't require a payment provider:

- `StartTrial(plan, trialStart, trialEnd)` — create in `Trial`. Emits `SubscriptionStartedDomainEvent`.
- `Activate(periodStart, periodEnd)` — `Trial | PastDue → Active`. Emits `SubscriptionActivatedDomainEvent`.
- `ChangePlan(newPlanId, periodStart, periodEnd)` — plan upgrade/downgrade while `Active`. Emits `SubscriptionPlanChangedDomainEvent`.
- `Cancel(atPeriodEnd: bool)` — `Active → Canceled` (immediate) or sets `cancel_at_period_end`. Emits `SubscriptionCanceledDomainEvent`.
- `Expire()` — `Trial | Canceled → Expired`. Emits `SubscriptionExpiredDomainEvent`.
- `MarkPastDue()` / `Cure()` — **shells in P02c-1** (the dunning state machine is Phase 09b); the methods + transitions exist so the enum is complete, but no live caller drives them until billing lands.

**Every** successful transition that changes the bound plan, the status, or `current_period_end` triggers an **entitlement recompute** (the handler calls the Entitlements projection service after persisting). This is the primary P02c-1 cross-module flow — see [../architecture/entitlement-projection.md](../architecture/entitlement-projection.md).

## Relationship to tenant creation

When `CreateTenantCommand` (TenantLifecycle) runs, it creates the initial `HubSubscription` in `Trial` bound to the chosen plan, then triggers the first entitlement recompute (generation = 1). In P02c-1 there's no payment step — the trial is created directly. The Stripe/Iyzico checkout that precedes this in production ([Architecture 24 § 5 provisioning sequence](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md)) is Phase 09b.

## Commands + queries (P02c-1)

`Application.Contracts`:

- `StartTrialCommand { TenantId, PlanId, TrialDays }` → `Result<SubscriptionDto>` (usually called by `CreateTenantCommand`, not directly).
- `ActivateSubscriptionCommand { TenantId, PeriodStart, PeriodEnd }` → `Result`.
- `ChangePlanCommand { TenantId, NewPlanId }` → `Result` (proration is Phase 09b; P02c-1 just rebinds + recomputes).
- `CancelSubscriptionCommand { TenantId, AtPeriodEnd }` → `Result`.
- `GetSubscriptionQuery { TenantId }` → `Result<SubscriptionDto>`.
- `GetSubscriptionsByPlanQuery { PlanId }` → `Result<IReadOnlyList<TenantId>>` — **consumed by the Plans module's fan-out recompute**; exposed via Application.Contracts so Plans doesn't reach into Subscriptions' Domain.
- `ListSubscriptionsQuery { Status?, Cursor, Limit }` → `Result<Page<SubscriptionSummaryDto>>`.

Validators: tenant exists (cross-module contract check), plan exists + is active, trial days > 0.

## DbContext

`SubscriptionsDbContext` — `hub` schema, table `subscriptions`. `HasDefaultSchema("hub")`. `ux_subscriptions_tenant_id` unique (1:1 with tenant). FK to `tenants` + `plans` — but note the FKs cross **module DbContext boundaries**; per LearnStack convention, cross-module FKs are modelled as plain `uuid` columns + an index, **not** EF navigation properties into another module's entities (each module owns its own DbContext; no cross-context navigations). Referential integrity across modules is enforced by application logic + (optionally) a DB-level FK if both tables share the `hub` schema. Document the choice; the simplest P02c-1 stance: plain `uuid` column + `ix_subscriptions_tenant_id` / `ix_subscriptions_plan_id` indexes, no EF navigation.

## Audit coverage (formal matrix lands in P02c-4)

| Operation                     | Class | Snapshot                                   |
| ----------------------------- | ----- | ------------------------------------------ |
| `StartTrialCommand`           | MUST  | after                                      |
| `ActivateSubscriptionCommand` | MUST  | before/after status                        |
| `ChangePlanCommand`           | MUST  | before/after plan_id                       |
| `CancelSubscriptionCommand`   | MUST  | before/after status + cancel_at_period_end |
| `GetSubscriptionQuery` / list | MAY   | —                                          |

## Out of scope for P02c-1

- Stripe / Iyzico integration, proration, invoicing (Phase 09b).
- The dunning / PastDue → Cure state machine driver (Phase 09b; transition methods ship as shells).
- `payment_provider` / `provider_subscription_id` population (Phase 09b; columns ship nullable + empty).
