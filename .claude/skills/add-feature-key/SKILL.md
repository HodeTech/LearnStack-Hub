---
name: add-feature-key
description: >
  Add a typed FeatureKey or LimitKey to the Hub registry (LearnStack.Hub.SharedKernel.FeatureFlags)
  so plans can author it into their features/limits and it flows into the entitlement
  projection LearnStack core consumes. USE FOR: introducing a new plan-projected
  feature toggle or numeric limit on the Hub authoring side. DO NOT USE FOR: reading
  a flag at runtime (that's LearnStack core's IFeatureFlags, not Hub), per-request
  toggling (forbidden), or domain-flavoured keys (CEFR, asana — forbidden; keys are
  generic platform capabilities).
---

# Adding a Hub FeatureKey / LimitKey

## Purpose

Hub is the **authoring** side of entitlements: operators build plans whose `features` / `limits` dictionaries use these keys, and the entitlement projection carries them to LearnStack core. This skill adds a key to the Hub registry and keeps the wire-format aligned with LearnStack core's registry (drift breaks the projection LearnStack consumes). Mirrors LearnStack's `add-feature-key`, authoring-side.

## When to use

- A new plan-level feature toggle (`FeatureKey`) or numeric limit (`LimitKey`) becomes a pricing dimension operators author.

## When not to use

- Reading a flag at runtime → that's LearnStack core's `IFeatureFlags.IsEnabledAsync`, not Hub. Hub never _reads_ entitlements for gating; it _authors_ them.
- Per-request toggling → forbidden; entitlements are plan-projected.
- A domain-flavoured key (`english.placement`, `yoga.asana`) → forbidden; keys are generic platform capabilities ([ADR-0021](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md)).

## Workflow

### Step 1 — Confirm shape + naming

Per [ADR-0021 Amendment 1](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md) + [entitlement-projection.md § key-shape rules](../../../docs/architecture/entitlement-projection.md):

- `FeatureKey` value: dotted snake_case, **no `.enabled` suffix** (every feature is implicitly boolean). e.g. `classroom.recording`, `tenancy.custom_domain`, `identity.sso.saml`.
- `LimitKey` value: `limits.` prefix. e.g. `limits.max_users`. `-1` = unlimited, `0` = unavailable.

### Step 2 — Add to the Hub registry

In `LearnStack.Hub.SharedKernel/FeatureFlags/FeatureKeys.cs` (or `LimitKeys.cs`):

```csharp
public static readonly FeatureKey <Name> = new("<dotted.snake_case>");
public static readonly LimitKey <Name>   = new("limits.<dotted.snake_case>");
```

### Step 3 — Keep it aligned with LearnStack core

The wire-format string **must** match LearnStack core's `FeatureKeys`/`LimitKeys` registry (`../LearnStack/backend/src/LearnStack.SharedKernel/FeatureFlags/`) exactly — LearnStack core reads the key from the projection by string. A mismatch means LearnStack silently never sees the feature. If you add a key Hub authors but LearnStack core doesn't yet read, note the pending LearnStack-side addition (a cross-repo registry-sync follow-up; the durable fix is a shared `LearnStack.Contracts` package, Phase 11 — see [plans.md § Registry sync](../../../docs/modules/plans.md)).

### Step 4 — Plan validator

The `Plan` validator ([plans.md](../../../docs/modules/plans.md)) rejects an unknown key in `features`/`limits` with `Result.Fail(validation_failed)`. Adding the key to the registry is what makes a plan authoring it valid. Confirm the validator picks up the new registry entry.

### Step 5 — Tests + docs

- Unit: a plan with the new key validates; a typo'd key fails.
- Update [docs/modules/plans.md](../../../docs/modules/plans.md) if the key set documented there changes materially.

## Validation

- Key in the Hub registry with the correct wire-format (no `.enabled`; `limits.` prefix for limits).
- Wire-string matches LearnStack core's registry (or the pending sync is noted).
- Plan validator accepts the new key; rejects typos.

## Common pitfalls

- **A `.enabled` suffix on a FeatureKey.** Dropped in ADR-0021 Amendment 1.
- **Drift from LearnStack core's registry.** The string is the contract; a mismatch is a silent feature-never-enabled bug.
- **A domain-flavoured key.** Keys are generic platform capabilities; domain shapes are tenant customization data on the LearnStack side.
