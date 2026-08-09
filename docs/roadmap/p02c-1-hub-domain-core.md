# P02c-1: Hub Domain Core

> **Status: ⏸ Implemented on branch, frozen.** The packet is written and green on `feat/phase-02c-packet-1-hub-domain-core` (seven commits, ~222 files) and is **not merged**. The freeze is an owner decision recorded on 2026-08-08; the condition that lifts it is in [Phase Exit Decision](#phase-exit-decision) below. The design specs the branch implements are merged on `main` and remain the contract.

## Goal

Give the Hub a domain: the four aggregates the control plane is built out of — `LearnStackTenant`, `Plan`, `HubSubscription`, `Entitlement` — plus the Hub-side cross-cutting foundation they run inside, so that every later packet programs against a `Result<T>` / MediatR / exception-handling surface a LearnStack developer already knows.

The load-bearing deliverable is the **entitlement projection**. Everything the Hub exists to do reduces to producing one correct, monotonically versioned projection per tenant; the internal API in [P02c-2](p02c-2-internal-api-and-contract.md) is transport for it, and the operator portal in [P02c-4](p02c-4-operator-portal.md) is an editor for its inputs. If the projection's shape or its `generation` counter is wrong here, every consumer downstream inherits the error.

This packet is also where the Hub's structural difference from LearnStack core becomes code rather than prose: **no Row Level Security, no tenant context, `OperatorId` instead of `UserId`, six pipeline behaviors instead of seven.** Those four deltas are not simplifications — they follow from the Hub being operator-administered and holding no tenant content.

## Scope

### Hub SharedKernel

A mirror of LearnStack's `LearnStack.SharedKernel`, folder for folder, under the `LearnStack.Hub.SharedKernel.*` namespace, with two substitutions: `OperatorId` replaces `UserId`, and `HubException` replaces `LearnStackException` as the base of the exception hierarchy. There is no tenant `UserId` type anywhere in the Hub.

It is a **copy, not a package reference**. The two repositories release independently and the Hub imports no LearnStack assembly; a shared `LearnStack.Foundation` package is a Phase 11 re-evaluation. The full surface — `Results/`, `Localization/`, `Domain/`, `Identifiers/`, `Time/`, `Random/`, `Pagination/`, `Persistence/`, `Errors/`, `Secrets/`, `Observability/`, `Resilience/`, `Hosting/` — is enumerated in [cross-cutting-foundation.md § 1](../architecture/cross-cutting-foundation.md).

Adds `FeatureFlags/` with the `FeatureKey` / `LimitKey` value objects and their registries. The Hub is the **authoring** side of these keys; their wire strings must match LearnStack core's registry exactly or the projection the Hub emits will not line up with what LearnStack reads.

### Cross-cutting foundation

Mirrors LearnStack's Phase 02a Packet 3 surface, Hub-adjusted. Specified in full in [cross-cutting-foundation.md](../architecture/cross-cutting-foundation.md):

- `HubExceptionHandler : IExceptionHandler` as the single L1 handler, `Result<T>.ToActionResult()` explicit at every endpoint, `ProblemDetailsFactory` + `HttpStatusMap` with the `https://errors.hub.learnstack.dev/` problem-type prefix.
- The **six-step MediatR pipeline**: Validation → Logging → AuditLog → Authorization → Transaction → OutboxFlush → Handler. `Validation`, `Logging` and `Transaction` are live; `AuditLog`, `Authorization` and `OutboxFlush` ship as shells whose registration order is the binding part.
- **No `TenantContextBehavior`.** The Hub has no per-request tenant context to assert and no Row Level Security session variable to set. Hub requests are operator-scoped, and the operator identity rides on the `learnstack-hub` realm JWT.
- Serilog as the primary logger with an OTLP sink, and the OpenTelemetry SDK for traces and metrics — with the OTel `LoggerProvider` deliberately not registered alongside, so nothing double-exports. No `TenantContextSpanProcessor`; an `OperatorContextSpanProcessor` waits for the Operators module in [P02c-4](p02c-4-operator-portal.md).
- `IErrorTrackingProvider` with its three implementations, and `IProviderResilience<TPort>`, both branched once at the composition root by `DeploymentMode`. Modules never read `DeploymentMode`.

`TransactionBehavior` is **live** here, unlike LearnStack's Packet 3 equivalent, because this packet ships real per-module `DbContext`s for it to open a unit of work on.

### Four modules

Each follows the four-project layout (`Domain`, `Application.Contracts`, `Application`, `Infrastructure`) with its own `DbContext`. Dependency rules and the cross-module flow are in [module-topology.md](../architecture/module-topology.md); the per-aggregate field lists, state machines, commands and queries are in the module deep dives.

| Module            | Aggregate          | Deep dive                                        |
| ----------------- | ------------------ | ------------------------------------------------ |
| `TenantLifecycle` | `LearnStackTenant` | [tenant-lifecycle.md](../modules/tenant-lifecycle.md) |
| `Plans`           | `Plan`             | [plans.md](../modules/plans.md)                       |
| `Subscriptions`   | `HubSubscription`  | [subscriptions.md](../modules/subscriptions.md)       |
| `Entitlements`    | `Entitlement`      | [entitlements.md](../modules/entitlements.md)         |

Aggregates use Vogen strongly-typed identifiers, inherit `Entity<TId>` or `AuditableEntity<TId>`, express invalid transitions as `Result.Fail(...)` rather than `DomainException`, and raise domain events. Cross-module reads go through `Application.Contracts` only — the Plans fan-out asks Subscriptions for the tenant ids bound to a plan; it never touches the Subscriptions `DbContext`.

### Schema and migrations

One EF migration per module `DbContext`, all in the `hub` schema of the `learnstack_hub` database. Snake-case tables and columns, JSONB for the dictionary-shaped columns, Vogen converters registered per context.

Generated SQL contains **no** `ENABLE ROW LEVEL SECURITY` and **no** `CREATE POLICY`. If it does, the `DbContext` has wrongly applied a tenant filter and the configuration is what gets fixed — not the migration. The reasoning is in [module-topology.md § Database isolation model](../architecture/module-topology.md): the Hub's isolation guarantee is that it stores no tenant content, not that rows are filtered per tenant.

### The entitlement projection

`EntitlementProjectionService` recomputes a tenant's `Entitlement` from its `HubSubscription` and the bound `Plan`, bumping the monotonic `generation` counter by exactly one and replacing the projection fields in the same transaction. The algorithm, the wire shape, the key-shape rules and the recompute triggers are in [entitlement-projection.md](../architecture/entitlement-projection.md).

`compliance_caps` ships as an empty JSONB shape ([P02c-5](p02c-5-custom-domain-lifecycle.md) fills it) and `grace_until` ships null ([P02c-6](p02c-6-license-key.md) fills it). The columns exist now because their absence would be a migration against a table the LearnStack side already mirrors.

The `learnstack.hub.entitlement` publish is a no-op shell here; it lights up with `IOutbox` in [P02c-2](p02c-2-internal-api-and-contract.md).

### Tests and seed data

- **Architecture:** the P02c-0 placeholders become real once module assemblies exist — `Hub_NeverStores_TenantData` scans them, per-module dependency tests assert the direction rules, and the two Hub deltas most likely to erode get their own assertions (no Row Level Security in the Hub schema; the six-step pipeline order).
- **Unit:** aggregate state-machine transitions, validators, and `generation` monotonicity — it starts at 1, only ever increments, and never resets across a downgrade.
- **Contract:** `EntitlementProjection_Shape_IsStable` snapshots the serialised projection against a checked-in `entitlement-v1.schema.json`. The same file is checked into LearnStack and asserted there independently, per [ADR-0034](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md).
- **Seed:** the four illustrative plan tiers, a demo tenant, a trial subscription and its entitlement — as data, not code constants.

Hub integration tests have **no tenant-isolation pair**. There is no Row Level Security to prove, and a test that asserts isolation the Hub does not implement would be misleading.

### Not in this packet

| Capability                                                     | Owning packet                                            |
| -------------------------------------------------------------- | -------------------------------------------------------- |
| Internal API endpoints, outbound client, mTLS + JWT + HMAC     | [P02c-2](p02c-2-internal-api-and-contract.md)            |
| `IOutbox` + the `learnstack.hub.entitlement` publish           | [P02c-2](p02c-2-internal-api-and-contract.md)            |
| `Usage` module                                                 | [P02c-2](p02c-2-internal-api-and-contract.md)            |
| Any change to the LearnStack repository                        | [P02c-3](p02c-3-learnstack-integration.md)               |
| Operator portal, `Operators` and `Audit` modules, live audit writer, real `AuthorizationBehavior` | [P02c-4](p02c-4-operator-portal.md)    |
| `CustomDomains` and `Compliance` modules                       | [P02c-5](p02c-5-custom-domain-lifecycle.md)              |
| `LicenseKeys`, the `.lic` format, `grace_until` semantics       | [P02c-6](p02c-6-license-key.md)                          |
| Stripe / Iyzico, `Invoicing`, dunning, proration                | [Hub Billing](hub-billing.md)                            |

## Deliverables

- `LearnStack.Hub.SharedKernel` mirroring LearnStack's, with `OperatorId` and `HubException`, unit-tested on the load-bearing types.
- The cross-cutting foundation wired in `LearnStack.Hub.Api`: L1 exception handler, six-step pipeline in canonical order, `ToActionResult`, Serilog + OpenTelemetry, `IErrorTrackingProvider`, `IProviderResilience<TPort>`, single-site `DeploymentMode` branching.
- Four modules with aggregates, state machines, commands, queries, validators, `DbContext`s and EF configurations.
- Four EF migrations creating `hub`-schema tables in `learnstack_hub`, applying cleanly, with no Row Level Security in the generated SQL.
- `EntitlementProjectionService` with monotonic `generation`, matching the documented wire shape.
- `entitlement-v1.schema.json` plus the contract snapshot test.
- Architecture, unit and contract suites green; the four plan tiers plus a demo tenant seeded.
- [`P02c-1-implementation-prompt.md`](P02c-1-implementation-prompt.md) — the execution artifact the implementing agent ran against, kept as the record of how the packet was built.

## Completion Criteria

- `dotnet build LearnStack.Hub.slnx` is clean; the unit, architecture and contract suites are green; `dotnet format --verify-no-changes` exits zero; a Leakwatch scan reports nothing.
- Every migration applies to an empty `learnstack_hub` database and produces the `hub` schema with no policy or Row Level Security statement in it.
- Creating a tenant produces a `Trial` subscription and an `Entitlement` at `generation` 1; changing the bound plan produces `generation` 2 with updated fields; no path decrements or resets the counter.
- The serialised projection validates against `entitlement-v1.schema.json`, and the snapshot test fails on any shape change.
- No `UserId` type, no `[TenantOwned]` marker, no EF global query filter, no `app.tenant_id` reference exists anywhere in the repository.
- The pipeline registration asserts six behaviors in canonical order, and `TenantContextBehavior` is absent.
- No module assembly references `LearnStack.SharedKernel`, `LearnStack.Domain`, `LearnStack.Infrastructure` or any `LearnStack.Modules.*` type.

## Risks

- **The mirrored SharedKernel drifts from its original.** It already has. LearnStack's [Phase 02a Packet 3b](https://github.com/cemililik/LearnStack/blob/main/docs/roadmap/phase-02a-kernel-tenancy.md) repairs three defects in the source this copy was taken from: `Results.Unit` collides with `MediatR.Unit` in any file importing both, `Result<T>` carries no `[MemberNotNullWhen]` so the compiler cannot prove `Value` is non-null after an `IsSuccess` check, and `Entity<TId>` overrides `Equals(object?)` without implementing `IEquatable<T>` or `operator ==` so every comparison boxes. All three are in the frozen branch and all three are cheapest to fix before the first handler exists. Reconciling against Packet 3b is the first item on the unfreeze checklist.
- **The projection shape is a cross-repository contract with a single author.** Nothing stops the Hub from changing `EntitlementProjectionDto` and breaking LearnStack silently. The mitigation is the checked-in schema plus a snapshot test in each repository, which converts a silent break into two failing builds.
- **Feature-key and limit-key registries drift.** Each repository keeps its own copy, and a key spelled differently on the two sides produces a projection that parses and means nothing. Recorded in [plans.md § Registry sync](../modules/plans.md); the durable fix is a shared contract package, which is a Phase 11 question.
- **Row Level Security creeps in by habit.** Every LearnStack skill and standard assumes it. A `DbContext` copied from a LearnStack module brings a tenant filter with it, and the filter would silently hide operator rows rather than fail loudly. Caught by reviewing the generated migration SQL and by the architecture test that asserts the Hub schema has no policies.
- **The freeze rots the branch.** Two hundred files that do not merge age against `main`, against the LearnStack SharedKernel they mirror, and against the ADRs written since. Mitigated by keeping the design specs on `main` as the authority, so the unfreeze is a re-implementation against a current spec rather than an archaeology exercise on a stale diff.

## Phase Exit Decision

**The packet is implemented and frozen.** The branch is green against its own definition of done; it is deliberately not merged.

The freeze exists because the Hub is a demand-gated track. LearnStack runs on `NullEntitlementProvider` and resolves hosts from `platform_host_to_tenant`, so nothing in LearnStack is blocked by the Hub's absence — and merging a control plane before it has a consumer means maintaining it through every LearnStack refactor for no user-visible return.

**The unfreeze condition is the Phase 02c trigger in [ADR-0035](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0035-demand-gated-infrastructure.md): a tenant must be billed or plan-gated.** When that becomes true, this packet resumes first — nothing else in the Hub can proceed without it.

The unfreeze checklist, in order:

1. Reconcile the Hub SharedKernel against LearnStack's post-Packet-3b source: the `Unit` rename, the `[MemberNotNullWhen]` annotations, and `Entity<TId>` equality. Also pick up Packet 4's `CursorPagination` binding fix, which returns 400 rather than surfacing an unhandled exception as a 500.
2. Move the `backend-integration` CI activation to [P02c-2](p02c-2-internal-api-and-contract.md), which owns it. The frozen branch flips it here, and the implementation prompt says so; three files on `main` say P02c-2, and P02c-2 is correct.
3. Confirm the projection still matches [ADR-0034](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md)'s read path — in particular that `grace_until` is carried in the projection and honoured durably on the LearnStack side, rather than collapsed into a cache TTL.
4. Note that the operator-audit writer this packet leaves as a shell follows [ADR-0033](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0033-audit-durability-model.md) when [P02c-4](p02c-4-operator-portal.md) lights it up: MUST-class audit is a durable intent inside the business transaction, not a best-effort write after it.

[P02c-2](p02c-2-internal-api-and-contract.md) begins when this packet is merged to `main` with its suites green in CI.
