---
name: add-integration-test
description: >
  Write a Testcontainers-backed integration test in LearnStack.Hub.Tests.Integration
  that exercises a real Postgres (+ optionally Dapr) and asserts Hub behaviour. USE
  FOR: the entitlement-rebuild round trip (tenant create → trial subscription →
  recompute → assert generation + projection fields), outbox → consumer round trips
  (P02c-2+), migration-applies-cleanly checks. NOTE: unlike LearnStack's version,
  Hub integration tests have NO tenant/organization isolation pair — Hub has no RLS.
  DO NOT USE FOR: pure unit tests (Unit), architecture tests (Architecture), or
  performance tests.
---

# Adding a Hub integration test

## Purpose

Exercise real persistence + cross-module flow against a Testcontainers Postgres. The headline P02c-1 integration test is the **entitlement-rebuild round trip**. Mirrors LearnStack's `add-integration-test` — **minus the mandatory tenant/organization isolation pair**, because Hub has no RLS to test.

> **The key difference from LearnStack.** LearnStack's integration tests MUST include a cross-tenant + cross-org isolation pair (`Tenant_A_cannot_read_Tenant_B_data`, etc.) for every tenant-owned entity, because RLS is the thing under test. **Hub has no RLS and no tenant isolation** — there is no such pair to write. Don't add one; it would test a guarantee Hub doesn't make.

## When to use

- A cross-module flow needs a real DB (the entitlement rebuild; later, outbox → consumer round trips).
- Asserting a migration applies cleanly + the schema is shaped as expected.

## When not to use

- Logic with no DB → `LearnStack.Hub.Tests.Unit`.
- Structural rules → `LearnStack.Hub.Tests.Architecture`.

## Workflow

### Step 1 — Activate the suite + CI job (first test only)

P02c-0 left `LearnStack.Hub.Tests.Integration` with zero tests and the `backend-integration` CI job gated `if: false`. The **first** integration test flips this on:

- Add the test (below).
- In `.github/workflows/ci.yml`, change the `backend-integration` job from `if: false` to a real trigger (mirror the `backend` job's checkout + setup-dotnet + restore/build, then `dotnet test` the integration project), and remove the placeholder step. Without this, the integration tests never run in CI.

### Step 2 — Test fixture (Testcontainers Postgres)

Use `Testcontainers.PostgreSql` to spin a real Postgres 18, apply the module migrations against it (into the `hub` schema), and `Respawn` between tests for a clean state. No Valkey/Kafka needed for P02c-1 (the entitlement publish is a no-op shell); add the Dapr/Kafka container in P02c-2 for outbox round trips.

### Step 3 — The entitlement-rebuild round trip (P02c-1 headline)

```
1. Create a Plan (Growth tier, known features/limits).
2. CreateTenantCommand → LearnStackTenant (Trial) + HubSubscription (Trial) + initial Entitlement.
   Assert: entitlements row exists, PK = tenant id, generation == 1,
           tier/features/limits match the plan, compliance_caps == {}.
3. ChangePlanCommand (to Scale) → recompute.
   Assert: same row, generation == 2, tier/features/limits updated.
4. (Optional) GetEntitlementQuery → serialised DTO matches the wire-shape
   in entitlement-projection.md (overlaps the contract test).
```

Assert the **monotonic generation** explicitly — it's the cache-coherency primitive.

### Step 4 — Determinism

Inject `FixedClock` + `FixedGuidFactory` via the test host so timestamps + ids are deterministic. Assert on `updated_at` from the fixed clock.

### Step 5 — Keep it focused

One behaviour per test; clear Arrange/Act/Assert; no shared mutable state across tests (Respawn resets). Name `<Flow>_<Condition>_<Expectation>`.

## Validation

- Test in `LearnStack.Hub.Tests.Integration`; green against a real Testcontainers Postgres.
- `backend-integration` CI job activated (no longer `if: false`).
- The entitlement round trip asserts `generation` 1 → 2 + correct projection fields.
- **No** tenant/organization isolation pair (Hub has no RLS).

## Common pitfalls

- **Copying LearnStack's isolation-pair requirement.** Hub has no RLS; there's nothing to isolate. Don't write `Tenant_A_cannot_read_Tenant_B`.
- **Forgetting the CI-job flip.** The first integration test must un-gate `backend-integration`.
- **Non-deterministic clock/guid.** Use `FixedClock`/`FixedGuidFactory`.
- **Skipping the generation assertion.** It's the contract-critical invariant.
- **Forgetting the dotnet PATH.** Testcontainers tests run under .NET 10 (`~/.dotnet/dotnet`).
