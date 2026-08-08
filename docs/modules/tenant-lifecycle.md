# Module: TenantLifecycle

`LearnStack.Hub.Modules.TenantLifecycle` owns the Hub-side mirror of LearnStack's `Tenant` aggregate. It is the root of the Hub domain graph: every subscription, entitlement, custom domain, compliance policy, and usage aggregate hangs off a `LearnStackTenant`.

Authoritative sources: [ADR-0019 § Hub data model](../../../LearnStack/docs/decisions/0019-learnstack-hub.md), [Architecture 24 § 2 ERD](../../../LearnStack/docs/architecture/24-learnstack-hub.md).

## Aggregate: `LearnStackTenant`

The name is deliberate — it is the Hub's **mirror** of the LearnStack-side `Tenant`, not a second source of truth. Hub is authoritative for _plan-related_ fields; LearnStack core is authoritative for _operational_ fields. The mirror carries metadata only — **never** tenant content.

`LearnStackTenant : AuditableEntity<LearnStackTenantId>` (Hub holds it mutably; status transitions + last-phone-home updates mutate it).

| Column               | Type             | Notes                                                                                                                                                                                                                                                              |
| -------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `id`                 | uuid PK          | **Mirror of the LearnStack-side `tenant.id`** — same UUID on both sides. Minted by Hub at provisioning and pushed to LearnStack via `POST /api/internal/tenants` (P02c-3). Use app-side `Guid.CreateVersion7()` via `IGuidFactory` so Hub has the id before flush. |
| `slug`               | text             | unique; `ux_tenants_slug`                                                                                                                                                                                                                                          |
| `display_name`       | text             |                                                                                                                                                                                                                                                                    |
| `status`             | text             | `TenantStatus` enum (see below); `ck_tenants_status` check constraint                                                                                                                                                                                              |
| `deployment_mode`    | text             | `DeploymentMode` enum: `SaaS \| Dedicated \| SelfHostedOnline \| SelfHostedAirGapped`; `ck_tenants_deployment_mode`                                                                                                                                                |
| `last_phone_home_at` | timestamptz null | updated by phone-home (P02c-6); null in P02c-1                                                                                                                                                                                                                     |
| audit columns        |                  | `created_at/by`, `updated_at/by` (`OperatorId`), `deleted_at/by`, `version` from `AuditableEntity`                                                                                                                                                                 |

Strongly-typed id: `LearnStackTenantId` — `[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]`.

> Note on `Development` mode: LearnStack's `DeploymentMode` enum has five values including `Development`, but a _tenant's_ `deployment_mode` only takes the four production values above (`Development` is a host-composition concern, not a tenant attribute). The check constraint lists the four.

## State machine: `TenantStatus`

```
                 ┌──────────┐
   create ──────►│  Trial   │
                 └────┬─────┘
                      │ activate (subscription → Active)
                      ▼
                 ┌──────────┐   suspend   ┌───────────┐
                 │  Active  │────────────►│ Suspended │
                 │          │◄────────────│           │
                 └────┬─────┘  reactivate └─────┬─────┘
                      │ archive                  │ archive
                      ▼                          ▼
                 ┌──────────┐               ┌──────────┐
                 │ Archived │               │ Archived │
                 └────┬─────┘               └──────────┘
                      │ terminate (hard delete, P02c later)
                      ▼
                 ┌────────────┐
                 │ Terminated │
                 └────────────┘
```

Values: `Trial | Active | Suspended | Archived | Terminated`.

Transition rules enforced as aggregate methods (each returns `Result` and emits a domain event; invalid transitions return `Result.Fail`, **never** throw `DomainException`):

- `Activate()` — `Trial | Suspended → Active`. Emits `TenantActivatedDomainEvent`.
- `Suspend(reason)` — `Active → Suspended`. Emits `TenantSuspendedDomainEvent`.
- `Archive()` — `Active | Suspended → Archived`. Emits `TenantArchivedDomainEvent`.
- `Terminate()` — `Archived → Terminated`. Emits `TenantTerminatedDomainEvent`. (The hard-delete-with-confirmation flow lands in [P02c-4](../roadmap/p02c-4-operator-portal.md), which builds the operator surface that confirms it; P02c-1 only needs the status transition.)
- `RecordPhoneHome(at)` — sets `last_phone_home_at` (no status change). P02c-6 caller; method shape ships now.

Each status change is a **trigger for entitlement recompute** when it affects the projection's `status`/`tier` (Activate, Suspend) — the handler calls the Entitlements projection service after a successful transition. See [../architecture/entitlement-projection.md](../architecture/entitlement-projection.md).

## Commands + queries (P02c-1)

`Application.Contracts`:

- `CreateTenantCommand { Slug, DisplayName, DeploymentMode, InitialPlanId }` → `Result<TenantCreatedDto>`. Orchestrates: create `LearnStackTenant` (status `Trial`) + create initial `HubSubscription` (Trial) + trigger initial `Entitlement` recompute (generation = 1). The `POST /api/internal/tenants` push to LearnStack core is P02c-3 — in P02c-1 the command stops at Hub-side persistence.
- `ActivateTenantCommand { TenantId }` → `Result`.
- `SuspendTenantCommand { TenantId, Reason }` → `Result`.
- `ArchiveTenantCommand { TenantId }` → `Result`.
- `GetTenantQuery { TenantId }` → `Result<TenantDetailDto>`.
- `ListTenantsQuery { Status?, DeploymentMode?, Cursor, Limit }` → `Result<Page<TenantSummaryDto>>` (cursor pagination).

Validators (FluentValidation): slug format (lowercase, dash-separated, length), display name non-empty, deployment mode in enum.

## DbContext

`TenantLifecycleDbContext` — `hub` schema, table `tenants`. `HasDefaultSchema("hub")`. Registers the `LearnStackTenantId` Vogen converter via the shared `RegisterVogenIds` helper. **No RLS, no global query filter** (see [../architecture/module-topology.md § Hub does NOT use RLS](../architecture/module-topology.md)).

EF config: `id` PK, `ux_tenants_slug` unique index, `ck_tenants_status` + `ck_tenants_deployment_mode` check constraints, `version` mapped as the optimistic-concurrency token.

## Audit coverage (formal matrix lands in P02c-4)

The Hub operator-audit pipeline lands in P02c-4 (`LearnStack.Hub.Modules.Audit`). Until then, the MUST-audit operations are documented here so the matrix is ready:

| Operation                             | Class | Snapshot                     |
| ------------------------------------- | ----- | ---------------------------- |
| `CreateTenantCommand`                 | MUST  | after                        |
| `ActivateTenantCommand`               | MUST  | before/after status          |
| `SuspendTenantCommand`                | MUST  | before/after status + reason |
| `ArchiveTenantCommand`                | MUST  | before/after status          |
| `TerminateTenantCommand`              | MUST  | before/after status          |
| `GetTenantQuery` / `ListTenantsQuery` | MAY   | —                            |

## Out of scope for P02c-1

- The `POST /api/internal/tenants` push to LearnStack core (P02c-3).
- Keycloak `learnstack` realm provisioning of the tenant admin (P02c-3 / Phase 03).
- Hard-delete-with-confirmation termination flow — [P02c-4](../roadmap/p02c-4-operator-portal.md).
- Phone-home `last_phone_home_at` updates from a live caller (P02c-6).
