# P02c-3: LearnStack Integration

> **Status: ⏳ Not started.** The first and largest of three packets that change both repositories; [P02c-5](p02c-5-custom-domain-lifecycle.md) and [P02c-6](p02c-6-license-key.md) each carry a smaller coordinated LearnStack-side half. It is the packet where the two-PR protocol matters most, and it states that protocol in full below. Depends on [P02c-2](p02c-2-internal-api-and-contract.md) Hub-side, and on LearnStack's Phase 02a packets 5, 6, 7 and 9 on the other side — see [the cross-repo blocking table](README.md#cross-repo-blocking).

## Goal

Connect the two systems. [P02c-2](p02c-2-internal-api-and-contract.md) builds the Hub's half of the contract against a test double; this packet replaces the double with the real LearnStack and proves the round trip: an operator creates a tenant in the Hub, and a tenant administrator logs into LearnStack and sees the feature set their plan grants.

It is deliberately a single packet across two repositories rather than two packets that reference each other. A contract half-landed is worse than a contract not landed — one side ships an endpoint nothing calls, the other ships an adapter that 404s, and both look green.

## Scope

### Hub side

The Hub-side work is small by design, because [P02c-2](p02c-2-internal-api-and-contract.md) already built the transport. What lands here is the **call sites**:

- `CreateTenantCommand` completes its flow: after Hub-side persistence it pushes `POST /api/internal/tenants` so LearnStack creates the tenant and its default organization with the **same** tenant id the Hub minted.
- The status transitions in `TenantLifecycle` — activate, suspend, archive, terminate — push to their LearnStack counterparts.
- `EntitlementProjectionService` pushes the projection after every recompute, carrying the monotonic `generation`.
- `IUsageReporter`'s Hub-side counterpart pulls aggregated usage where the push model is not enough.

Failures on these pushes are retried through `IProviderResilience<TPort>` and are **not** allowed to roll back the Hub-side transaction. The Hub's state is authoritative for plan data; a LearnStack that has not caught up yet is a convergence problem, not a consistency one.

### LearnStack side

Described in full in [LearnStack Phase 02c](https://github.com/cemililik/LearnStack/blob/main/docs/roadmap/phase-02c-hub-foundation.md). Summarised here only so this document is readable on its own — that file is the authority for it:

- Handlers for every Hub → LearnStack path in the [ADR-0034](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md) set, behind the same three-layer authentication chain.
- The `HubEntitlementProvider`, `IUsageReporter` and `IHubTenantSync` adapters. These three are the **only** types in the LearnStack codebase permitted to hold a Hub client.
- The entitlement read path in ADR-0034's normative order: in-process cache, then distributed cache, then the durable `platform_entitlement_cache` row with its `valid_until` and `grace_until`, then the Hub. A Hub outage on a cold cache falls through to the durable row and honours the recorded grace window — it does not throw out of a feature-flag check.
- `entitlement-v1.schema.json` copied verbatim from the Hub and asserted by a snapshot test on the LearnStack side too.
- The architecture tests the boundary needs: `LearnStack_Modules_DoNotReference_Hub`, `Hub_Client_Referenced_Only_By_Named_Adapters`, `IEntitlementProvider_Implementations_Are_Three`, and `NullEntitlementProvider_NotRegistered_OutsideDevelopment` — which is vacuous until this packet gives it a second implementation to be exclusive against.

### A boundary this packet must not cross

**Host resolution never calls the Hub.** `IHostToTenantResolver` reads `platform_host_to_tenant` and nothing else. An anonymous page load must not depend on a control plane being reachable, or a Hub outage takes tenant marketing sites down. `IHubClient.LookupHostAsync` does not exist; [ADR-0034](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md) deleted it.

### Pull-request coordination protocol

Two pull requests, one session:

1. **The Hub pull request opens first.** It carries the canonical contract shape — payload schemas, the authentication chain's expectations, and `entitlement-v1.schema.json`. Whatever it says is what the contract is.
2. **The LearnStack pull request opens second and references the Hub pull request's commit hash** in its body, so a reviewer on either side can find the other half.
3. **Both merge in the same session.** A one-sided merge leaves the contract dangling: an unreachable endpoint or an adapter with no server.
4. **If review forces a contract change, the Hub pull request is amended first** and the LearnStack pull request is re-pointed at the new hash. Never the reverse — a contract with two authors has no author.
5. **`entitlement-v1.schema.json` is byte-identical in both repositories.** A divergence blocks both merges.

Adding or changing an endpoint in the contract surface requires a new ADR in LearnStack's `docs/decisions/`, not here, because the surface is a cross-repository agreement and LearnStack owns the decision corpus.

## Deliverables

- Hub-side call sites wired: tenant provisioning, status transitions, entitlement push, usage pull — each retried, none able to roll back Hub-side state.
- LearnStack-side handlers, the three named adapters, and the ADR-0034 entitlement read path, landed in the LearnStack repository.
- `entitlement-v1.schema.json` identical in both repositories, snapshot-tested in each.
- The boundary architecture tests green on the LearnStack side.
- An end-to-end integration test spanning both systems: create a tenant in the Hub, assert it exists in LearnStack with a default organization and a populated projection.
- A degraded-mode test: with the Hub unreachable and the in-process and distributed caches cold, LearnStack serves the durable projection and honours `grace_until`.
- Two merged pull requests referencing each other.

## Completion Criteria

- An operator creates a tenant in the Hub and, within seconds, the tenant exists in LearnStack with its default organization and its entitlement projection populated — same tenant id on both sides, and a request carrying `Host: {slug}.{platform-domain}` resolves to that tenant.
- Flipping a feature on the tenant's plan in the Hub reaches LearnStack's feature-flag reads within seconds.
- A projection push carrying an older `generation` than the one already stored is rejected rather than applied.
- With the Hub down and every cache cold, a feature-flag check returns an answer from `platform_entitlement_cache` and respects its grace window. Past the grace window, the documented degraded behaviour applies — no unhandled exception at any point.
- With the Hub down, an anonymous public page load is unaffected. Nothing on that path touches a Hub client.
- `NullEntitlementProvider` is not registered outside `Development`, and the test that says so is no longer vacuous.
- Only `IEntitlementProvider`, `IUsageReporter` and `IHubTenantSync` hold a Hub client anywhere in the LearnStack codebase.
- The internal API rejects a request missing any one of mTLS, the signed JWT, or the HMAC body signature — asserted against the real LearnStack, not a double.

## Risks

- **Split-brain merge.** One side merges, the other is delayed by review, and `main` in one repository describes a contract the other does not implement. Mitigated by the protocol above, and by the fact that neither half has a user until both land — there is no pressure to ship one early.
- **The blocking chain is long.** This packet waits on four LearnStack packets. If any of them changes shape — particularly `IEntitlementProvider` in P02a-9 or `platform_entitlement_cache` in P02a-6 — the adapters are written against a moving target. Mitigated by treating the LearnStack-side sockets as the contract and starting the Hub-side call sites only once they are merged.
- **The Hub becomes a single point of failure for tenants.** The mitigation is architectural, not operational: the durable `platform_entitlement_cache` row with its own grace window, plus host resolution that never consults the Hub. Both are testable, and both are in the completion criteria above so they cannot be quietly skipped.
- **Tenant id divergence.** The Hub mints the tenant id and LearnStack must adopt it. If either side generates its own, every subsequent push targets a tenant that does not exist. Mitigated by the Hub minting a version-7 UUID before flush and by an end-to-end test asserting the same id on both sides.
- **Provisioning partially applied.** The Hub commits and the LearnStack push fails, leaving a tenant with a plan and no product. Mitigated by retry plus a reconciliation path: `IHubTenantSync` can re-push a tenant that LearnStack does not have, and the projection push is idempotent under its `generation` guard.
- **The three-adapter rule erodes.** The easiest way to call the Hub is to inject its client where you need it. Mitigated by `Hub_Client_Referenced_Only_By_Named_Adapters`, which is the mechanical form of ADR-0034's second invariant.

## Phase Exit Decision

P02c-3 is complete when both pull requests are merged, referencing each other, and a reviewer on a clean checkout of both repositories can run one end-to-end scenario: create a tenant in the Hub, watch it appear in LearnStack with its default organization and projection, flip a plan feature and watch the flag change, then stop the Hub and confirm that public pages still render and feature checks still answer from the durable cache within their grace window.

[P02c-4](p02c-4-operator-portal.md) can proceed in parallel with this packet — it depends on [P02c-1](p02c-1-hub-domain-core.md) and [P02c-2](p02c-2-internal-api-and-contract.md), not on the LearnStack side of the boundary. Both are prerequisites of [P02c-7](p02c-7-exit-gate.md).
