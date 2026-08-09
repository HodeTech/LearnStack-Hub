---
name: add-ef-migration
description: >
  Generate and edit an EF Core migration for a Hub module — hub schema, snake_case
  naming, NO RLS / NO tenant query filter, forward-only / two-step destructive-change
  rules. USE FOR: adding / altering / dropping a Hub table or column, adding indexes,
  JSONB columns. DO NOT USE FOR: hand-editing an applied migration, destructive
  changes without a two-step plan, or adding RLS policies (Hub has none).
---

# Adding a Hub EF migration

## Purpose

Produce a migration that follows Hub's database conventions: `hub` schema, snake_case, the right index/constraint naming, JSONB for dictionaries, **no RLS** (the load-bearing difference from LearnStack's migrations) and the forward-only / two-step destructive rules from [Standards 05](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/05-database.md).

## When to use

- Adding / altering / dropping a Hub table or column; adding indexes; JSONB columns.

## When not to use

- Hand-editing a migration that's already applied to a shared DB → add a new migration.
- A destructive change without a two-step deprecation plan.
- Adding an RLS policy → Hub has none; if you think you need one, you're modelling tenant content, which is forbidden.

## Workflow

### Step 1 — Generate

```bash
export PATH="$HOME/.dotnet:$PATH"
cd backend
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/LearnStack.Hub.Modules.<Module>.Infrastructure \
  --startup-project src/Core/LearnStack.Hub.Api \
  --context <Module>DbContext \
  --output-dir Persistence/Migrations
```

Migration name convention: `<Verb>_<Subject>` e.g. `Create_Tenants`, `Add_Subscription_PaymentProvider`. Migrations live with the owning module.

### Step 2 — Review the generated SQL

`dotnet ef migrations script --context <Module>DbContext` and read it. Confirm:

- Tables in the `hub` schema (`hub.<table>`), snake_case plural.
- `id` PK `uuid` (or tenant id PK for `entitlements`); `<entity>_id` FK columns (plain `uuid`, no cross-context navigation).
- Index names `ix_<table>_<cols>`, unique `ux_<table>_<cols>`, checks `ck_<table>_<rule>`.
- Enum columns stored as text with a `ck_` check constraint listing the allowed values.
- Dictionary fields as `jsonb`.
- Audit columns (`created_at/by`, `updated_at/by`, `deleted_at/by`, `version`) where the aggregate is `AuditableEntity`.
- **NO** `ENABLE ROW LEVEL SECURITY`, **NO** `CREATE POLICY`, **NO** tenant-filter SQL. If the generator emitted any, your DbContext config wrongly applied a tenant filter — fix the config, regenerate.

### Step 3 — Hand-edit only for what EF can't express

PostgreSQL-specific bits EF doesn't generate (a partial index, a generated column, a non-trivial check) go in the migration's `Up`/`Down` via `migrationBuilder.Sql(...)`. Keep `Down` a true inverse.

### Step 4 — Forward-only / destructive rules

- After a migration merges, treat it as immutable; corrections are new migrations.
- Destructive changes (drop column, change type) are two-step: tolerant code + additive migration first, then the strict migration after deploy. Document the two steps.

### Step 5 — Apply + verify

```bash
# shared Postgres + learnstack_hub must be up (local-dev-setup)
dotnet ef database update --context <Module>DbContext --startup-project src/Core/LearnStack.Hub.Api
```

Confirm the tables landed in `learnstack_hub.hub.*`.

## Validation

- Migration in the module's `Persistence/Migrations`; name follows `<Verb>_<Subject>`.
- Script shows `hub`-schema tables, correct naming, JSONB where expected, **no RLS / no policy / no tenant filter**.
- `Down` is a real inverse; `dotnet ef database update` applies + rolls back cleanly.

## Common pitfalls

- **RLS leaking into the migration.** Means the DbContext applied a tenant query filter — Hub must not. Fix the config.
- **Editing an applied migration.** Add a new one instead.
- **Cross-context FK as an EF navigation.** Plain `uuid` + index.
- **Forgetting the dotnet PATH.** `dotnet ef` needs .NET 10 (`~/.dotnet/dotnet`).
- **Missing `Microsoft.EntityFrameworkCore.Design`.** The Infrastructure project needs it for `dotnet ef`.
