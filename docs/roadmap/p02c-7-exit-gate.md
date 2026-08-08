# P02c-7: End-to-End Exit Gate

## Goal

Decide, on evidence, whether Phase 02c is done.

Packets [P02c-1](p02c-1-hub-domain-core.md) through
[P02c-6](p02c-6-license-key.md) each close on their own criteria, and each one is
reviewed against its own scope. That is exactly how a control plane ends up with six
green packets and a boundary that does not hold: the interesting failures in a two-repository,
two-realm, mutually-authenticated system live *between* the packets, and nobody owns
between.

P02c-7 owns it. It writes no new features. It runs seven scenarios across both
repositories against a clean environment and states, as a single answer, whether the Hub
and LearnStack are actually integrated or merely both compiling.

## Scope

Seven gates. Each is an observable a reviewer can watch, not a claim they have to trust.

### Gate 1 — Provisioning round trip

An operator creates a tenant in the Hub operator portal, and it exists in LearnStack.

- `CreateTenantCommand` persists `LearnStackTenant` (status `Trial`), the initial
  `HubSubscription` bound to the chosen plan, and the initial `Entitlement` at
  `generation = 1`.
- `POST /api/internal/tenants` reaches LearnStack, which creates the tenant **and its
  default organization** — organization is not optional, and a tenant without one is a
  tenant no `[OrganizationScoped]` write can target.
- The tenant carries the **same UUID on both sides**. The Hub mints it with
  `Guid.CreateVersion7()` through `IGuidFactory` before flush, precisely so the two
  systems never have to reconcile two identities for one tenant.
- LearnStack's `platform_entitlement_cache` holds the projection at generation 1, with the
  plan's `features` and `limits` and an empty `compliance.caps`.

### Gate 2 — Entitlement propagation

An operator flips one feature on a plan, and LearnStack answers differently within
seconds.

- `UpdatePlanCommand` persists the plan change and fans out a recompute over every
  subscription bound to that plan.
- Each affected `Entitlement` increments `generation` by exactly one.
- `PUT /api/internal/tenants/{id}/entitlements` pushes the new projection; LearnStack
  accepts it only if `received.generation >= cached.generation`, so a late or out-of-order
  delivery cannot overwrite newer state.
- A feature check on the LearnStack side returns the new value **within the push round
  trip** — not within a cache TTL. The push is what makes propagation seconds rather than
  minutes; a system that only converges when the 15-minute TTL expires has not passed this
  gate, it has merely waited.
- The L1 and L2 caches are invalidated by the push, and the durable
  `platform_entitlement_cache` row is updated, in the order
  [ADR-0034 § The entitlement read path](../../../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md)
  makes normative.

### Gate 3 — Custom domain resolves to the right tenant

A domain goes from submitted to resolving, without an operator editing infrastructure.

- The domain completes a DNS-01 challenge against an ACME staging directory and reaches
  `Active`.
- `learnstack.hub.custom-domain.activated` and
  `PUT /api/internal/tenants/{id}/host-mappings` land a `platform_host_to_tenant` row.
- A request carrying that `Host` header resolves to that tenant, and a request carrying a
  different tenant's host resolves to the other one — both asserted, because a resolver
  that always returns the same tenant passes a single-host test.
- **The resolver does not call the Hub.** The Hub is stopped for this assertion; host
  resolution keeps working. An anonymous page load must not depend on the control plane.
- The entitlement payload for that tenant contains **no** certificate material and **no**
  host fields, per [P02c-5](p02c-5-custom-domain-lifecycle.md).

### Gate 4 — The internal API rejects a request missing any leg of the auth chain

Both directions, three negative tests each.

The chain is mTLS + RS256 JWT (`aud=learnstack-internal`, five-minute expiry, `jti` replay
protection) + HMAC body signature, applied to every endpoint in
[ADR-0034's set](../../../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md).

| Removed | Expected |
|---|---|
| Client certificate | Connection refused at the TLS layer; no application handler runs |
| JWT (absent, expired, wrong audience, or replayed `jti`) | `401`, request not processed |
| HMAC body signature (absent or over a tampered body) | `401`, request not processed |

Run against **both** internal surfaces: LearnStack's `/api/internal/*` (Hub → LearnStack)
and the Hub's `/api/v1/internal/*` (LearnStack → Hub). Each rejection is logged with the
correlation id and the failed check; the response body says nothing about which leg
failed, because an attacker probing the chain should learn nothing from the shape of the
refusal.

`Internal_API_Endpoints_AreNot_Public` stays green: neither internal surface is bound to
an internet-facing listener.

### Gate 5 — The realm boundary holds in both directions

- A `learnstack` (tenant) realm token presented to the operator portal or to a Hub
  operator-facing endpoint is **rejected**.
- A `learnstack-hub` (operator) realm token presented to a LearnStack tenant-facing
  endpoint is **rejected**.

Both directions, both asserted at runtime and both backed by
`Hub_Operator_JWT_NeverAccepted_On_LearnStack_Routes` and its Hub-side mirror. A token
that is valid, unexpired and correctly signed by the wrong realm is the interesting case —
signature validity is not authorisation, and an issuer check that is absent looks
identical to one that passes until the day it matters.

### Gate 6 — Both architecture suites green

`LearnStack.Hub.Tests.Architecture`:

- `Hub_NeverStores_TenantData`
- `Hub_Modules_DoNotReference_LearnStack_Internals`
- `Internal_API_Endpoints_AreNot_Public`
- `Frontend_Has_Only_The_OperatorPortal_App`
- `No_Source_Folder_Named_Verticals`
- `Meta_NetArchTest_DetectsAPlantedViolation` — the suite proves it can still fail

`LearnStack.Tests.Architecture`, on the boundary rules:

- `LearnStack_Modules_DoNotReference_Hub`
- `Hub_Client_Referenced_Only_By_Named_Adapters` — no type outside
  `IEntitlementProvider`, `IUsageReporter` and `IHubTenantSync` holds a Hub client
- `IEntitlementProvider_Implementations_Are_Three`
- `NullEntitlementProvider_NotRegistered_OutsideDevelopment`
- `Modules_Do_Not_Read_Entitlement_Cache_Directly`
- `Modules_Do_Not_Reference_DeploymentMode`
- `CustomDomain_TenantId_NeverReadFrom_RequestBody` and
  `Cert_PrivateKey_NeverLeavesVault_To_Logs` from
  [ADR-0022](../../../LearnStack/docs/decisions/0022-custom-domain-tls.md)
- `LicenseKey_Validation_Is_Pinned_RSA2048` from
  [ADR-0020](../../../LearnStack/docs/decisions/0020-triple-deployment-hybrid-license.md)

Names follow the reconciled canonical identifiers in
[Standards 21](../../../LearnStack/docs/standards/21-architecture-tests-catalogue.md);
Hub-side rules are registered there rather than existing only in this repository.

### Gate 7 — One contract, two repositories

- `entitlement-v1.schema.json` is byte-identical in both repositories, and each side's
  snapshot test asserts its own serialiser against it. `additionalProperties: false`, so a
  field cannot be added on one side alone.
- `license-payload-v1.schema.json` likewise, with `license_id` required
  ([P02c-6](p02c-6-license-key.md)).
- The endpoint set implemented on both sides matches
  [ADR-0034](../../../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md)
  exactly — no extra path, no missing one.
- The `FeatureKey` / `LimitKey` registries agree. The two repositories each keep their own
  copy ([plans.md § Registry sync](../modules/plans.md)), so a reconciliation check runs
  here and fails on drift rather than discovering it in a customer's projection.

### What this packet does not do

No new aggregates, no new endpoints, no new screens. Anything the gates expose is fixed in
the packet that owns it, and that packet's status is reopened. A defect found here is not
"P02c-7 work" — it is evidence that an earlier packet exited early.

## Deliverables

- A cross-repository end-to-end suite that boots both stacks (Testcontainers for Postgres,
  the ACME staging directory, and both Keycloak realms) and runs Gates 1–5 as executable
  scenarios.
- Negative-path tests for the auth chain, both directions, all three legs.
- The registry-reconciliation check for `FeatureKey` / `LimitKey`.
- A short runbook for running the gate locally, in
  [docs/operations/](../operations/README.md).
- Hub-side architecture test registrations added to
  [Standards 21](../../../LearnStack/docs/standards/21-architecture-tests-catalogue.md)
  under canonical names.
- A dated Phase 02c status entry in [the Hub roadmap index](README.md), and the matching
  entry on the LearnStack side in
  [Phase 02c](../../../LearnStack/docs/roadmap/phase-02c-hub-foundation.md).

## Completion Criteria

Phase 02c is done when all of the following are observable, in this order, against a
freshly provisioned environment:

1. An operator creates a tenant in the Hub, and it appears in LearnStack with its default
   organization and an entitlement projection at generation 1, under the same UUID.
2. Flipping a feature on that tenant's plan propagates to a LearnStack feature check
   within seconds, with `generation` incremented and stale-overwrite protection proven by
   replaying an older generation and watching it be rejected.
3. A custom domain completes its DNS challenge, reaches `Active`, and a request carrying
   that host resolves to that tenant — with the Hub stopped.
4. A request to either internal API missing mTLS, missing or invalid JWT, or missing or
   invalid HMAC is rejected, and the rejection reveals nothing about which leg failed.
5. The operator portal is unreachable with a `learnstack` realm token, and LearnStack's
   tenant surface is unreachable with a `learnstack-hub` realm token.
6. Both architecture suites are green, including the meta-test that proves the Hub suite
   can still detect a planted violation.
7. Both schema snapshot tests pass against identical schema files, and the feature-key
   registries reconcile.

## Risks

- **Gate theatre.** A suite that only walks the happy path certifies nothing; the negative
  cases in Gates 3, 4 and 5 are the ones that carry the weight. Mitigated by making each
  negative case an explicit, separately named test, so deleting one is visible in the diff.
- **The gate becoming the only place the boundary is exercised.** If the auth chain is
  tested nowhere else, a change breaks it and nothing notices until the full suite runs.
  Mitigated by keeping the per-packet integration tests that cover their own half, with
  this suite covering only the seam.
- **Two-repository drift after the gate.** The gate is a point-in-time proof; the schemas
  and the endpoint set can diverge the next week. Mitigated by the snapshot tests and the
  registry reconciliation running in **both** CI pipelines, not only in this suite.
- **Environment-dependent green.** A gate that passes because a developer's Keycloak still
  holds yesterday's client configuration proves nothing. Mitigated by running against a
  clean environment built from the checked-in realm export and compose files, twice in a
  row.
- **Reopening an earlier packet feels like regression.** It is not — a gate finding is the
  system working as designed. The alternative is closing Phase 02c over a known break.

## Phase Exit Decision

**Phase 02c is complete when the seven gates pass in CI, against a clean environment,
twice in succession, and the two repositories' status records agree on what shipped.**

That is the whole claim. Restated in one sentence for anyone deciding whether to depend on
it: *the Hub can provision, entitle, domain-name and licence a LearnStack tenant across a
mutually authenticated boundary that refuses every request missing any leg of its auth
chain, refuses each realm's token on the other realm's surface, and never carries tenant
content or private keys across.*

What Phase 02c explicitly does **not** deliver, with owners:

| Not delivered | Owner |
|---|---|
| Invoicing, payment capture, dunning, usage aggregation, plan editor | [hub-billing.md](hub-billing.md) |
| Marketplace listings and installs | [hub-marketplace.md](hub-marketplace.md) |
| Self-Hosted as a *supported* deployment mode — rotation, revocation distribution, hot-reload runbook | [LearnStack Phase 11](../../../LearnStack/docs/roadmap/phase-11-production-hardening.md) |
| Custom-domain TLS termination at the LearnStack edge | [LearnStack Phase 11](../../../LearnStack/docs/roadmap/phase-11-production-hardening.md) |
| Hub production deployment, HA, backup and restore | [LearnStack Phase 11](../../../LearnStack/docs/roadmap/phase-11-production-hardening.md) |

LearnStack does not wait on any of it. Per
[ADR-0035](../../../LearnStack/docs/decisions/0035-demand-gated-infrastructure.md) the
core runs on `NullEntitlementProvider` until a tenant must be billed or plan-gated, which
is the trigger that makes this phase urgent rather than the calendar.
