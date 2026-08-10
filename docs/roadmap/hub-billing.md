# Hub Billing and Invoicing

> **This is the authoritative plan.** It was migrated out of LearnStack's
> [phase-09b-hub-billing.md](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-09b-hub-billing.md),
> which is now a pointer at this file. Hub billing is Hub work and belongs in the
> repository that ships it.
>
> Identifier note: the track keeps the **09b** slot in LearnStack's phase numbering
> because that identifier appears in commit messages, branch names and cross-repository
> references. The plan lives here; the slot stays there.

## Goal

Charge tenants for LearnStack.

[P02c-1](p02c-1-hub-domain-core.md) shipped `Plan` and `HubSubscription` as
**provisioning** primitives — enough to bind a tenant to a feature set and project an
entitlement, not enough to take money. A subscription there has a period and a status; it
has no invoice, no payment method, no dunning, and no consequence when a card declines.
This track fills in the commercial half: a ledger, metered usage, payment adapters, and a
grace-and-degradation path that turns non-payment into a graded response rather than an
outage.

It runs in parallel with
[LearnStack Phase 09](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-09-billing-integrations-analytics.md),
which builds a completely different billing system. The two never overlap:

| Concern | Owner |
|---|---|
| A tenant sells courses to learners | **LearnStack Phase 09** — the core `Billing` module, storefront billing |
| LearnStack charges a tenant for their LearnStack subscription | **This track** — Hub-side platform billing |

The trigger that makes this track urgent is simple and worth stating: **a tenant must be
invoiced**. Until then, subscriptions stay in `Trial` and `Active` as provisioning states
and nothing here is missing.

## Scope

### Aggregates

- **`HubInvoice` / `HubInvoiceLine`** — the per-tenant invoice ledger. An issued invoice
  is **immutable**: a correction is a credit note plus a new invoice, never an edit. A
  ledger you can edit is not a ledger, and seven-year retention on a mutable record is
  retention of whatever the last writer decided.
- **`UsageAggregate`** — usage rolled up by `(tenant_id, month, metric)`. Metrics:
  concurrent classroom sessions, total classroom minutes, storage GB, media bandwidth GB,
  learner count, custom-domain count.
- **`DunningPolicy`** — per-plan escalation rules for missed payment: retry schedule,
  grace window, notification cadence, terminal action.
- **`PaymentProviderAccount`** — the vendor's own Stripe / Iyzico / wire-only
  configuration. Not a tenant's payment configuration; that is LearnStack Phase 09's
  concern.
- **`WebhookLedger`** — one row per received provider event, unique on
  `(provider, provider_event_id)`. This is the idempotency mechanism, and bypassing it is
  a hard rule in [CLAUDE.md](../../CLAUDE.md).

### `HubSubscription` billing-state extension

The subscription gains `billing_state`, `dunning_state` and `grace_until`.

It does **not** gain `current_period_start`, `current_period_end` or
`cancel_at_period_end` — those already ship in P02c-1
([subscriptions.md](../modules/subscriptions.md)) because the entitlement projection needs
`current_period_end` to populate `expires_at`. The migrated LearnStack text listed all six
as new; three of them are already there.

`payment_provider` and `provider_subscription_id` ship nullable and empty in P02c-1 and are
populated here.

The `MarkPastDue()` / `Cure()` transitions, which P02c-1 ships as method shells with no
live caller, get their driver here.

### Money representation

Pinned once, in one place, because getting it wrong is expensive and silent:

- Amounts are stored as **integer minor units** with an explicit ISO 4217 currency. No
  floating-point type touches a monetary value at any layer, including JSON payloads.
- An invoice records the currency it was issued in and is **never** re-denominated. FX
  conversion, if it is ever needed, produces a new document, not a recalculated old one.
- The rounding rule is declared once and applied at line level before summation, so an
  invoice's total always equals the sum of its printed lines.

### Usage ingestion and aggregation

- `POST /api/v1/usage/report` — already enumerated in
  [ADR-0034's endpoint set](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md),
  and its handler **will be built by** [P02c-2](p02c-2-internal-api-and-contract.md) —
  produces the raw stream from LearnStack's `IUsageReporter`. This track assumes that
  ingestion exists; it does not build it.
- A Hangfire job rolls raw reports into `UsageAggregate` daily.
- Reports carry an idempotency key. A replayed report **must not double-count**: the
  ingestion path is idempotent on that key, and the daily rollup is recomputed from raw
  rows rather than incremented in place, so a replay corrects itself rather than
  compounding.
- Late-arriving reports for a closed period are recorded against the period they belong to
  and surfaced as an adjustment, not silently dropped or silently folded into the current
  month.
- Soft-limit alerts are **produced by LearnStack**, not by the Hub: the gated call site
  compares current usage against the limit it already holds from the entitlement
  projection and emits `usage.alert.soft_limit_reached` over
  `POST /api/v1/usage/report`
  ([LearnStack Architecture 21 § Soft vs Hard Limits](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/21-feature-flags.md)).
  [P02c-2](p02c-2-internal-api-and-contract.md) will ingest that stream. **This track adds
  the Hub half**: distinguishing an alert row from an ordinary usage row, retaining it,
  surfacing it in the operator portal, and optionally notifying the tenant admin.

### Billing lifecycle

- A tenant created through [P02c-1](p02c-1-hub-domain-core.md)'s flow starts at
  `billing_state = trial`.
- Trial → active generates the first invoice.
- Period end closes the period, generates a `HubInvoice`, and emits
  `learnstack.hub.invoice.generated`.
- A failed payment enters `dunning_state = grace` for the policy's window (default 14
  days). **During grace the entitlement projection is unchanged** — the tenant keeps
  working while a human sorts out a card.
- On grace expiry the projection is **recomputed** to a read-only feature set. This is the
  important design point: the downgrade is an ordinary recompute that bumps `generation`
  and travels the ordinary push path
  ([entitlement-projection.md](../architecture/entitlement-projection.md)). It is not a
  special-case flag, not a side channel, and not a separate endpoint — which means the
  cache-coherency guarantees that hold for a plan upgrade hold for a suspension too.
- Cancellation honours `cancel_at_period_end`; no immediate access loss.

### Payment provider adapters

All behind `IHubPaymentProvider`:

- **Stripe** — cards, ACH, SEPA.
- **Iyzico** — Turkish market.
- **Manual / wire transfer** — an operator marks payment received; the audit entry is the
  receipt.

Adding a fourth is a code edit, not an ADR.

> **Naming.** `IHubPaymentProvider` is deliberately distinct from LearnStack core's
> `IPaymentProvider` (Phase 09, tenant-facing storefront). The two share a shape —
> idempotency key, webhook signature verification, status mapping — but describe different
> billing relationships: `IPaymentProvider` charges *learners* on behalf of a tenant;
> `IHubPaymentProvider` charges *tenants* on behalf of LearnStack. LearnStack adapters live
> in `LearnStack.Infrastructure.Payments.*` in the other repository. Running both in one
> process is forbidden by the codebase separation invariant
> ([ADR-0019](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0019-learnstack-hub.md)).
>
> **Hub adapter project names are fixed by the SDK import boundary** in
> [CLAUDE.md](../../CLAUDE.md), which permits a vendor SDK type only inside the project
> named for that vendor:
>
> | Adapter | Project | SDK |
> |---|---|---|
> | Stripe | `LearnStack.Hub.Infrastructure.Stripe` | Stripe.net — importable here and nowhere else |
> | Iyzico | `LearnStack.Hub.Infrastructure.Iyzico` | Iyzipay — importable here and nowhere else |
> | Manual / wire transfer | `LearnStack.Hub.Infrastructure.Payments.Manual` | none — it is an operator action plus an audit entry, so it carries no vendor dependency and sits outside the SDK boundary |

Provider SDK types never leave their adapter assembly; SDK exceptions are translated into
`ProviderException` at the boundary, and `IProviderResilience<IHubPaymentProvider>` carries
retry, circuit breaker, timeout and bulkhead.

### Operator portal extensions

Built on the [P02c-4](p02c-4-operator-portal.md) shell:

- **Plan editor** — feature toggles, limit inputs, price, billing cycle. Deferred out of
  P02c-4 to here so that a plan's feature payload and its price are edited on one form.
  The editor validates every key against the `FeatureKey` / `LimitKey` registries and
  refuses unknown keys ([plans.md](../modules/plans.md)).
- Per-tenant billing tab: subscription state, current period, recent invoices, payment
  provider, dunning state, grace expiry.
- Invoice viewer and PDF export.
- Plan-change workflow with proration, re-pushing the entitlement projection.
- Usage-metrics screens, per tenant and per metric.
- Bulk invoice export (CSV) for accounting.

### Tenant-facing hook — in LearnStack, not a new Hub crossing

A read-only billing tab in LearnStack's Admin Studio shows a tenant their own subscription
state, recent invoices and next payment due, served by a thin LearnStack-side proxy over
`IEntitlementProvider`'s billing-info extension.

The migrated text justified this as "no new endpoint on the four-endpoint surface". Under
[ADR-0034](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md) the
rule is stated differently and the justification is stronger: the crossing goes through a
**named adapter**, and no type outside `IEntitlementProvider`, `IUsageReporter` and
`IHubTenantSync` holds a Hub client — asserted by
`Hub_Client_Referenced_Only_By_Named_Adapters`. The count was never the point.

### Compliance

- Tax per region — Stripe Tax for the Stripe adapter, a maintained rate table for the
  others.
- Invoice retention of seven years, above the Hub audit retention floor.
- Every operator billing action (plan change, manual invoice, refund, write-off) is a
  MUST-class audit entry with `actor.hubOperator = true`
  ([P02c-4](p02c-4-operator-portal.md)).

## Deliverables

- `LearnStack.Hub.Modules.Invoicing` — `HubInvoice`, `HubInvoiceLine`, `WebhookLedger`,
  with the immutability rule enforced in the aggregate rather than by convention.
- `LearnStack.Hub.Modules.Usage` extended with the daily aggregation job and adjustment
  handling.
- `DunningPolicy` and `PaymentProviderAccount` aggregates; the `HubSubscription`
  billing-state extension and the live `MarkPastDue` / `Cure` drivers.
- Stripe, Iyzico and Manual adapters behind `IHubPaymentProvider`, with webhook receivers
  guarded by `WebhookLedger`.
- Invoice PDF generation.
- Operator portal: plan editor, billing tab, invoice viewer, usage screens, bulk export.
- LearnStack-side proxy endpoint for the Studio billing tab, in a coordinated pull
  request.
- Recompute-driven read-only downgrade on grace expiry, travelling the ordinary
  entitlement push path.

## Completion Criteria

- An operator moves a tenant trial → active → cancelled, and the correct invoices and
  entitlement refreshes exist at each step.
- A failed payment enters dunning; the tenant continues operating unchanged through the
  grace window; on expiry the projection downgrades to read-only with an incremented
  `generation`, and LearnStack's Studio shows the notice banner.
- A provider webhook delivered twice produces exactly one ledger row and one state
  transition. A webhook delivered out of order does not move the subscription backwards.
- A usage report replayed with the same idempotency key does not double-count in
  `UsageAggregate`.
- An issued invoice cannot be edited through any code path; a correction produces a credit
  note and a new invoice, and the two reconcile.
- Every invoice total equals the sum of its printed lines, in integer minor units, in one
  declared currency.
- The tenant's Studio billing tab shows accurate subscription and invoice data through the
  proxy, with no Hub client outside the three named adapters.
- The plan editor refuses an unknown feature or limit key.
- Operator audit captures every billing action with `actor.hubOperator = true`.
- Hub architecture suite green; LearnStack's boundary tests
  (`LearnStack_Modules_DoNotReference_Hub`,
  `Hub_Client_Referenced_Only_By_Named_Adapters`) still green.

## Risks

- **Two-billing confusion.** Tenant admins conflating the money they collect from learners
  with the money they pay LearnStack. Mitigated by hard UI separation — storefront under
  Studio's catalogue, platform subscription under Settings — and by never using the word
  "billing" unqualified in either surface.
- **A mutable ledger.** The pressure to "just fix" a wrong invoice is constant and the fix
  is always one `UPDATE`. Mitigated by making `HubInvoice` immutable after issue at the
  aggregate level, so the correction path is the only path.
- **Webhook replay and reordering.** Providers retry aggressively and deliver out of
  order; a naive handler can charge twice or resurrect a cancelled subscription. Mitigated
  by the `WebhookLedger` unique constraint and by handlers that assert the expected
  current state rather than blindly applying a transition.
- **Payment provider drift.** Stripe and Iyzico ship breaking changes on their own
  schedule. Mitigated by the adapter boundary and by contract tests against recorded
  fixtures, so an API change fails a test rather than a customer's payment.
- **Grace-period gaming.** A tenant cycling through grace repeatedly to avoid paying.
  Mitigated by continuing to report usage through grace and by surfacing serial-grace
  tenants in the operator portal, where a human decides.
- **Money in floating point.** It enters through a DTO, a chart, or a CSV export rather
  than through the ledger. Mitigated by the integer-minor-unit rule applying to every
  layer including serialisation, and by a test that asserts no monetary field on any DTO
  is a floating-point type.

## Phase Exit Decision

This track is complete when the SaaS deployment can charge a real tenant end to end:
operator provisions a tenant → trial → active → invoice generated → payment captured →
next period rolls over — and when a declined payment produces a graded, reversible
degradation rather than an outage.

Self-Hosted tenants do not need this track at all: they pay by purchasing a licence key
([P02c-6](p02c-6-license-key.md)), not by subscription billing. A Self-Hosted-only customer
base is a valid state of the world in which this track never ships, which is why its
trigger is "a tenant must be invoiced" rather than a position in a sequence.
