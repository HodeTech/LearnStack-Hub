# P02c-2: Internal API and Contract Surface

> **Status: ⏳ Not started.** Depends on [P02c-1](p02c-1-hub-domain-core.md) being merged. Unblocked by LearnStack — this packet touches no LearnStack code.

## Goal

Give the Hub a wire. [P02c-1](p02c-1-hub-domain-core.md) produces a correct entitlement projection that nothing outside the Hub can read; this packet builds both halves of the transport that carries it — the handlers for the calls LearnStack makes into the Hub, and the outbound client for the calls the Hub makes into LearnStack — behind the mTLS + signed-JWT + HMAC chain that makes those calls trustworthy.

It also fixes the shape of the contract. Everything [P02c-3](p02c-3-learnstack-integration.md) writes on the LearnStack side is written against what this packet publishes, which is why the Hub pull request opens first in every cross-repo pairing.

## Scope

### The endpoint set

The authoritative list of paths, methods and directions is the table in [ADR-0034 § The endpoint set](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md). It is not restated here — a second copy is a second thing to keep in step, and the corpus has already paid that price once.

Two rules from that ADR govern this packet directly:

- **The Hub stores no tenant content**, and **every LearnStack↔Hub crossing goes through a named adapter.** Those two invariants replace the older "closed at four endpoints" rule. Adding an endpoint still requires an ADR, because both repositories have to agree.
- **Certificate material never rides the entitlement payload.** TLS private keys move between the Hub-owned and LearnStack-owned secret stores by secret-store replication and are referenced by path, never by value, in anything LearnStack caches, logs, audits or mirrors.

This packet ships:

- **Hub-side handlers** for the LearnStack → Hub direction: licence verification, the scheduled refresh, and usage reporting.
- **The outbound `LearnStackApiClient`** for the Hub → LearnStack direction, covering every path in the set. `PUT /api/internal/tenants/{id}/host-mappings` is built here and has **no caller until [P02c-5](p02c-5-custom-domain-lifecycle.md)** — the client is complete so that P02c-5 adds a call site, not a transport.

The Hub's own tenant-facing and operator-facing API (`/api/v1/tenants/*`, `/api/v1/subscriptions/*`, `/api/v1/webhooks/*`) is **not** part of this surface. It is the Hub's public API, governed here, and grows with [P02c-4](p02c-4-operator-portal.md) and [Hub Billing](hub-billing.md).

### The authentication chain

The current authority is [ADR-0034](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md), which carries the endpoint set and the single authentication chain that guards it in both directions; [ADR-0019](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0019-learnstack-hub.md) is where the chain originates. Three independent layers on every call, in both directions:

- **mTLS** with client certificates signed by the LearnStack-internal CA.
- **A signed RS256 JWT** with `aud=learnstack-internal` and an expiry of at most five minutes, replay-protected by a short-TTL inbox keyed on `jti`.
- **An HMAC-SHA256 body signature** in `X-Signature`, using a per-deployment shared secret.

All three secrets — the client certificate and key, the JWT signing key, the HMAC key — are read through `ISecretProvider`. **Production secret material** appears in no configuration file, log line, error message or trace attribute. The `HUB_INTERNAL_API_HMAC_KEY` placeholder in `.env.example` is not an exception to that rule: it is non-secret, development-only fixture data with an obvious placeholder shape, and it never names a production value. Rejecting a request that is missing any one layer is a tested behaviour, not an assumed one.

**Both internal prefixes** bind to an internal listener and are never reachable on the internet-facing one: LearnStack hosts `/api/internal/*` and the Hub hosts `/api/v1/internal/*`. `Internal_API_Endpoints_AreNot_Public` covers **both** patterns — a rule written only for `/api/internal/*` would leave every Hub-hosted endpoint on this surface unguarded, since none of them match that prefix. The Hub's own operator- and tenant-facing API (`/api/v1/tenants/*` and the rest) stays on the public listener; `/api/v1/internal/*` is the segment that does not.

Realm separation is enforced on the same endpoints: a `learnstack` (tenant) realm token is rejected on every Hub `/api/v1/internal/*` route, per the two-realm boundary in [ADR-0004 Amendment 1](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0004-authentication-strategy.md). The mTLS + JWT + HMAC chain and the realm check are independent — passing the transport chain does not make a tenant token acceptable.

### The `Usage` module

`UsageAggregate` and its `DbContext`, populated by the inbound usage-report handler. Usage reports are **idempotent**: a client that retries after a timeout must not double-count. The idempotency key and its retention window are settled here, because a metric that silently double-counts is worse than a metric that is missing.

`GET /api/internal/tenants/{id}/usage` on the LearnStack side is the pull complement; the Hub's client for it lands here and its LearnStack-side handler in [P02c-3](p02c-3-learnstack-integration.md).

### Outbox and the entitlement push

`IOutbox` becomes real and the P02c-1 `OutboxFlushBehavior` shell is promoted to a live flush.

Two things travel when a projection is recomputed, and they are not the same thing:

- **The projection itself**, pushed over `PUT /api/internal/tenants/{id}/entitlements`. This is the contract-bearing path and it is HTTP. It carries the `generation` counter, which is what lets the receiver reject an out-of-order delivery instead of overwriting newer state with older.
- **An eager-invalidation signal**, published as `learnstack.hub.entitlement`. This is an optimisation over waiting for a cache to expire, and it is the first genuine cross-process integration event in either system — which is precisely the trigger [ADR-0035](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0035-demand-gated-infrastructure.md) names for promoting `IEventBus` from its in-process default to a broker-backed adapter. This packet ships the publish behind `IOutbox` and `IEventBus` so that the transport choice stays at the composition root and is settled by that trigger, not by this packet.

**They are two outbox records, not one.** A recompute enqueues one record for the HTTP push and one for the event, each with its own handler, its own idempotency key and its own retry schedule, so neither delivery can be dropped by the other's success and a partial failure retries only the half that failed. Coordinating both behind a single record would make them one delivery with two side effects: a handler that pushed and then failed to publish would either retry the push (duplicating it) or mark the record done (silently losing the event).

Convergence follows from that split, and it is **receiver-side** idempotency that makes it work — "exactly once" is a property of the effect, not of the transport. Both deliveries retry after a lost acknowledgement, because a sender that cannot distinguish "never arrived" from "arrived, ack lost" must retry, and both receivers absorb the repeat:

- **The push** is idempotent on `generation`. The rule LearnStack applies is stated once, in [entitlement-projection.md § The `generation` counter](../architecture/entitlement-projection.md): a payload older than the cached generation is rejected, so a retry or an out-of-order delivery cannot overwrite newer state. A replay at the _current_ generation carries the same bytes the receiver already holds — the Hub increments by exactly one per recompute and is the only writer — so applying it again changes nothing.
- **The event** is idempotent on `(tenant_id, generation)` at the consumer's inbox guard, so a redelivery is a no-op.

Correctness does not depend on the signal arriving at all: if the event is lost after its retries are exhausted, the projection is still authoritative and the receiver still converges on the next read.

> **Open contract question, owned by ADR-0034, not by this packet.** The rule above says nothing about a payload that arrives at the _current_ generation with _different_ bytes. That should be impossible — one writer, +1 per recompute — so it is a symptom, not a case to converge on, and the two defensible answers are "reject with a conflict" and "protect the receiver with a durable idempotency key on the delivery rather than on the generation". Choosing between them changes observable behaviour on the LearnStack side of a two-repository contract, so it is settled by an ADR in `../LearnStack/docs/decisions/` amending [ADR-0034](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md) and landed in both repositories — not by this document, and not by whichever handler is written first. [P02c-7](p02c-7-exit-gate.md)'s Gate 2 gains the divergent-replay case when that ADR exists.

### OpenAPI and SDK generation

- An OpenAPI document generated from code, covering the endpoints that exist at this point.
- The `@learnstack-hub/sdk` package generated from it, which [P02c-4](p02c-4-operator-portal.md)'s operator portal consumes instead of hand-rolled `fetch` calls.
- A contract test asserting the document does not change silently.

### CI

**The `backend-integration` CI job is already active.** It was gated `if: false` from [P02c-0](p02c-0-repository-bootstrap.md) because there was nothing for it to run, and [P02c-1](p02c-1-hub-domain-core.md) activated it along with the first Testcontainers-backed tests — the entitlement-rebuild round trip. An earlier draft of this document assigned the activation here; that was written when P02c-1 was expected to ship without integration tests. This packet adds its own cases to a running job.

### Documentation

`docs/architecture/learnstack-api-client.md` lands with this packet: client-certificate provisioning, JWT issuance from the `learnstack-hub` realm service account, the HMAC body signer, and the retry and backoff posture.

## Deliverables

- Hub-side handlers for every LearnStack → Hub path in the ADR-0034 set, with the full three-layer authentication chain enforced.
- `LearnStackApiClient` covering every Hub → LearnStack path, decorated with `IProviderResilience<TPort>` for retry, circuit breaking, timeout and bulkhead.
- `Usage` module with `UsageAggregate`, its `DbContext`, its migration, and idempotent report ingestion.
- Real `IOutbox` with the `OutboxFlushBehavior` promoted from shell to live, and the `learnstack.hub.entitlement` publish behind `IEventBus`.
- Internal listener binding, with `Internal_API_Endpoints_AreNot_Public` green.
- OpenAPI document, generated `@learnstack-hub/sdk`, and a contract test over the document.
- Testcontainers-backed integration tests and the activated `backend-integration` CI job.
- `docs/architecture/learnstack-api-client.md`.

## Completion Criteria

- A request missing the client certificate, the JWT, or the HMAC signature is rejected — each layer independently, each covered by a test. A request with a JWT past its five-minute expiry, or replaying a `jti` already seen, is rejected.
- No secret used by the chain is readable from configuration, a log line, an error response, or a trace attribute.
- `/api/internal/*` returns nothing on the internet-facing listener.
- Reporting the same usage metric twice with the same idempotency key records it once.
- Recomputing an entitlement enqueues exactly two outbox rows — the `PUT .../entitlements` push and the `learnstack.hub.entitlement` publish — each carrying the current `generation`. Failing one leaves the other's row untouched and retryable. "Exactly one" is measured at the receiver, in **effective applications**, not in transport attempts: a delivery retried after a lost acknowledgement is required behaviour, and the test asserts that replaying it leaves the receiver's state and its `generation` unchanged.
- The OpenAPI document generates an SDK that typechecks, and the contract test fails on an unannounced change.
- `backend-integration` runs on every pull request and is a required check.

## Risks

- **Three security layers, three ways to be accidentally optional.** A misconfigured listener, a validation branch that returns early, or a test harness that stubs the chain all produce a surface that looks guarded and is not. Mitigated by testing each layer's rejection independently rather than testing only the happy path, and by making the internal-listener binding an architecture-test assertion rather than a deployment convention.
- **Secret rotation is an outage waiting to happen.** Three secrets, two repositories, and no dual-key window means a rotation is a synchronised deploy. The rotation procedure and its dual-key overlap are designed here even though the first rotation is far away, because retrofitting a rotation window into a live contract is worse.
- **Retry semantics that corrupt rather than repeat.** A retried entitlement push that arrives out of order overwrites newer state; a retried usage report double-counts. Both are mitigated in the payload — the monotonic `generation` for the push, the idempotency key for the report — rather than by hoping the transport is exactly-once.
- **The contract drifts because only one side compiles against it.** The Hub can change a payload and stay green. Mitigated by the OpenAPI contract test here, by `entitlement-v1.schema.json` asserted in both repositories, and by the coordination protocol in [P02c-3](p02c-3-learnstack-integration.md).
- **The invalidation signal is treated as load-bearing.** If any behaviour comes to depend on `learnstack.hub.entitlement` arriving, a lost message becomes a correctness bug rather than a latency one. The push is the contract; the event is an optimisation, and reviews hold that line.

## Phase Exit Decision

P02c-2 is complete when a caller holding a valid certificate, a valid JWT and a valid body signature can verify a licence, refresh it, and report usage against a running Hub — and when the Hub can, over the same chain, exercise every Hub → LearnStack path against a contract double, including the host-mapping push that has no production caller yet.

Concretely: every layer of the chain rejects independently when removed, the internal listener is not publicly reachable, a repeated usage report counts once, an out-of-order projection push is rejected by its `generation`, the generated SDK typechecks, and `backend-integration` is green as a required check.

[P02c-3](p02c-3-learnstack-integration.md) begins from that state — it is the packet that replaces the contract double with the real LearnStack.
