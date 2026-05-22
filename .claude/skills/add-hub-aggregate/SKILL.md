---
name: add-hub-aggregate
description: >
  Add a Hub aggregate (or entity) — Vogen strongly-typed id, Entity<TId> or
  AuditableEntity<TId> base, EF configuration, NO RLS, OperatorId audit columns — to
  an existing Hub module. USE FOR: any new Hub metadata aggregate (LearnStackTenant,
  Plan, HubSubscription, Entitlement, CustomDomain, CompliancePolicy, LicenseKey,
  UsageAggregate, HubInvoice). This is the Hub analogue of LearnStack's
  add-tenant-owned-entity — MINUS the entire tenant-isolation layer (no [TenantOwned],
  no RLS, no query filter). DO NOT USE FOR: a new module (use add-hub-module), a pure
  value object with no table, or anything that would store tenant content (forbidden).
---

# Adding a Hub aggregate

## Purpose

Add a Hub metadata aggregate the right way: strongly-typed Vogen id, the correct base class, an EF configuration in the `hub` schema, **no RLS / no tenant-isolation machinery**, and `OperatorId`-typed audit columns where mutable. This is where the Hub-vs-LearnStack delta bites hardest — LearnStack's `add-tenant-owned-entity` wires four isolation layers; Hub wires **none** of them.

## When to use

- Adding any new Hub aggregate from a module deep dive (`docs/modules/<name>.md`).

## When not to use

- A new module → [add-hub-module](../add-hub-module/SKILL.md).
- A pure value object → just add the Vogen `[ValueObject<T>]` type; no aggregate machinery.
- Anything that would hold tenant content (`Course`, `Lesson`, tenant `User`, …) → forbidden; `Hub_NeverStores_TenantData` rejects it.

## Inputs

| Input                | Required | Description                                                                                                                 |
| -------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------- |
| Aggregate name       | Yes      | PascalCase. Matches the module deep dive.                                                                                   |
| Owning module        | Yes      | Determines namespace, DbContext, migration project.                                                                         |
| Base class           | Yes      | `AuditableEntity<TId>` (mutable, audited) or `Entity<TId>` (append-mostly, e.g. `Entitlement` which is replaced wholesale). |
| Relates to a tenant? | Yes      | If yes, carry a `LearnStackTenantId` column as an **ordinary FK** — NOT an isolation boundary.                              |

## Workflow

### Step 1 — Strongly-typed id (Domain)

```csharp
[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]
public readonly partial record struct <Name>Id : IStronglyTypedId<Guid>;
```

Mint app-side via `IGuidFactory.NewUuidV7()` when the aggregate needs its id before flush (e.g. to raise a domain event referencing it). Names end in `Id`.

### Step 2 — Aggregate (Domain)

```csharp
public sealed class <Name> : AuditableEntity<<Name>Id>   // or Entity<<Name>Id>
{
    public LearnStackTenantId TenantId { get; private set; }   // ordinary FK if tenant-related; NOT an RLS boundary
    // ... domain fields ...

    private <Name>() { }   // EF
    public static <Name> Create(...) { /* factory + invariants; raise domain event */ }
    // state-machine methods return Result, emit domain events, never throw DomainException for business rules
}
```

Rules: no `[TenantOwned]` / `[OrganizationScoped]` markers (those don't exist in Hub). Audit columns come from `AuditableEntity` and are typed `OperatorId`. Invalid state transitions return `Result.Fail`, never throw.

### Step 3 — EF configuration (Infrastructure)

`Persistence/Configurations/<Name>Configuration.cs` (`IEntityTypeConfiguration<<Name>>`):

- Table `<plural_snake_case>`, `hub` schema (inherited from `HasDefaultSchema("hub")`).
- `id` PK (or the tenant id as PK for `Entitlement`); `ix_`/`ux_` indexes; `ck_` check constraints for enum columns.
- JSONB columns for dictionary fields (`features`, `limits`, `compliance_caps`).
- Map `version` as the optimistic-concurrency token (`IsRowVersion` / `xmin` per the EF+Npgsql convention LearnStack uses).
- **No** `HasQueryFilter` for tenant isolation. **No** RLS policy in the migration.
- Register the Vogen id converter (covered by the DbContext's `RegisterVogenIds`).

### Step 4 — Migration

Add the EF migration → [add-ef-migration](../add-ef-migration/SKILL.md). It creates the `hub.<table>` with the columns + indexes + check constraints — and **no** `ENABLE ROW LEVEL SECURITY`, **no** `CREATE POLICY`.

### Step 5 — Tests

- Unit: factory invariants + every state-machine transition (valid → `Result.Ok` + event; invalid → `Result.Fail`). For `Entitlement`: `generation` strictly increases, starts at 1.
- Architecture: `Hub_NeverStores_TenantData` already scans the module — confirm the new type name doesn't contain a forbidden fragment. `Aggregate_Roots_Use_StronglyTypedId` covers the Vogen id.
- Integration (where the aggregate participates in a cross-module flow, e.g. the entitlement rebuild) → [add-integration-test](../add-integration-test/SKILL.md).

## Validation

- Build 0/0; unit + architecture tests green.
- Migration creates `hub.<table>` with NO RLS / NO policy / NO tenant query filter.
- Audit columns typed `OperatorId`; no `UserId`.
- The module deep dive (`docs/modules/<name>.md`) matches the implemented fields.

## Common pitfalls

- **Reflexively adding RLS / `[TenantOwned]` / a query filter.** Hub has none. The `tenant_id` column (if present) is a plain FK.
- **Using `UserId` for audit columns.** Hub uses `OperatorId`.
- **Throwing `DomainException` for a business rule.** Return `Result.Fail`. `DomainException` is for programmer errors / aggregate-invariant bugs only.
- **Cross-module EF navigation to another module's aggregate.** Use the id column + an `Application.Contracts` call.
- **`Entitlement` as `AuditableEntity`.** It's `Entity<LearnStackTenantId>` (PK = tenant id, replaced wholesale, `generation`-versioned) — not soft-deletable / audit-columned.
