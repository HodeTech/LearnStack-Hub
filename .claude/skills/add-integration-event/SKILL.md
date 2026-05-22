---
name: add-integration-event
description: >
  Publish or consume a cross-module / cross-repo integration event in Hub using the
  outbox pattern + IEventBus (Dapr pub/sub → Kafka) under the learnstack.hub.* topic
  prefix, with per-module inbox idempotency for consumers. USE FOR: declaring a
  versioned learnstack.hub.<aggregate> event (e.g. learnstack.hub.entitlement,
  learnstack.hub.custom-domain.activated), wiring a module to publish it via
  IOutbox.EnqueueAsync, or consuming a LearnStack-emitted event (learnstack.tenancy.*).
  DO NOT USE FOR: intra-module domain events (plain MediatR INotification, in-process),
  the four HTTPS contract endpoints (those are request/response, not pub/sub), or
  adding a fifth HTTPS contract endpoint (needs an ADR).
---

# Adding a Hub integration event

## Purpose

Wire a cross-boundary event the right way: outbox-written in the same transaction as the aggregate change, dispatched via Dapr pub/sub on a `learnstack.hub.*` topic, idempotent on the consumer via a per-module inbox. Mirrors LearnStack's `add-integration-event` + `wire-dapr-pubsub`, scoped to Hub's topic namespace.

> **Phasing note.** In P02c-1 the outbox + Dapr publish are **shells** (`OutboxFlushBehavior` is a no-op; the entitlement recompute triggers an in-process call, not a published event). The real `IOutbox` + Dapr publish land in **P02c-2** alongside the outbound `LearnStackApiClient`. Use this skill from P02c-2 onward; in P02c-1, only the event _shape_ + the no-op shell exist.

## When to use

- Declaring a new versioned `learnstack.hub.<aggregate>` event (e.g. `learnstack.hub.entitlement`, `learnstack.hub.custom-domain.activated/.deactivated/.renewed`).
- Wiring a Hub module to publish via `IOutbox.EnqueueAsync`.
- Consuming a LearnStack-emitted event Hub subscribes to (e.g. `learnstack.tenancy.tenant.renamed`).

## When not to use

- Intra-module notification → plain MediatR `INotification` (`IDomainEvent`), in-process, same transaction. No outbox.
- A request/response contract → that's one of the four HTTPS endpoints (a fifth needs an ADR in `../learnstack/docs/decisions/`).

## Workflow

### Step 1 — Declare the versioned event (Application.Contracts)

```csharp
public sealed record <Aggregate><PastTenseVerb>IntegrationEventV1(...)  // e.g. EntitlementRecomputedIntegrationEventV1
{
    public const string Topic = "learnstack.hub.<aggregate>";           // e.g. "learnstack.hub.entitlement"
}
```

Topic convention: `learnstack.hub.<aggregate>` (Hub's prefix; LearnStack core uses `learnstack.{module}.*` without `.hub.`). Version the record (`V1`, `V2`); a breaking payload change is a new version, not an edit. Keep the payload minimal — for `learnstack.hub.entitlement` it's `{ tenant_id, generation, expires_at }` ([entitlement-projection.md](../../../docs/architecture/entitlement-projection.md)).

### Step 2 — Publish via the outbox (producer)

In the handler, after persisting the aggregate change, enqueue to the outbox **in the same DbContext transaction** (the `OutboxFlushBehavior` + `TransactionBehavior` commit them atomically):

```csharp
await outbox.EnqueueAsync(new EntitlementRecomputedIntegrationEventV1(...), ct);
```

Never call `IEventBus.PublishAsync` directly from a handler — only the `OutboxProcessor` publishes (at-least-once, after commit). Never use a raw `KafkaProducer`.

### Step 3 — Dapr component

The topic flows through Hub's own Dapr sidecar (`infra/dapr/components/pubsub-kafka.yaml`, consumer group `learnstack-hub-api`, brokers `host.docker.internal:9092`). New topics need no new component — the existing pub/sub component carries all `learnstack.hub.*` topics. Confirm Kafka topic-ACL expectations (Hub publishes only under `learnstack.hub.*`).

### Step 4 — Consume (when Hub subscribes)

For a LearnStack-emitted event Hub consumes (e.g. `learnstack.tenancy.tenant.renamed`): implement `IIntegrationEventHandler<TEvent>`; **first** call `IInboxGuard.IsAlreadyProcessedAsync` before any business logic; mark processed in the same business transaction so the inbox marker + the write commit atomically.

### Step 5 — Tests

- Contract: the event payload JSON is snapshot-stable (a breaking change forces a `V2`).
- Integration (Testcontainers + Dapr): producer enqueues → outbox row written in the same tx; consumer is idempotent (re-delivery is a no-op via the inbox guard).

## Validation

- Event versioned (`V1`), `Topic` = `learnstack.hub.<aggregate>`.
- Producer writes to the outbox in the aggregate's transaction; no direct `IEventBus`/Kafka.
- Consumer (if any) checks `IInboxGuard` before business logic.
- No new Dapr component needed; topic under the `learnstack.hub.*` prefix.

## Common pitfalls

- **Publishing directly from a handler.** Outbox-only; the processor publishes after commit.
- **An unversioned event.** Breaking changes need `V2`.
- **Wrong topic prefix.** Hub publishes under `learnstack.hub.*`; never under bare `learnstack.*`.
- **A consumer without the inbox guard.** Re-delivery duplicates the side effect.
- **Reaching for a fifth HTTPS endpoint instead of an event.** If it's fire-and-forget cross-boundary state, it's an event; a new request/response endpoint needs an ADR.
