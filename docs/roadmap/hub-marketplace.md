# Hub Marketplace

> **This is the authoritative plan.** It was migrated out of LearnStack's
> [phase-12-hub-marketplace.md](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-12-hub-marketplace.md).
> The **12** slot stays in LearnStack's phase numbering because the identifier is
> referenced across both repositories; the plan lives here.
>
> ⚠️ **Coordination owed, and this note comes down when it is discharged.** That LearnStack
> file is still the full plan on LearnStack's `main`; its conversion to a pointer is
> prepared but unmerged. **The LearnStack pointer conversion merges first, or in the same
> session as** any Hub-side change that depends on this file being the single source —
> including the ADR-ownership rule and the activation gate below. Until it lands, two
> documents claim the same track and the link above leads to the competing one.
>
> **Status: post-MVP and optional.** The platform works fully without it. If the
> marketplace never ships, no LearnStack feature breaks. The track exists because
> customization-as-data creates an obvious sharing opportunity — not because anything
> depends on it.

## Goal

Let tenants publish and install reusable **tenant customization data**.

A yoga studio that has authored a good asana content type, its level taxonomy, its page
blocks and its completion rules has built something a second yoga studio would pay in time
to skip. Under
[ADR-0018](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0018-tenant-driven-customization-model.md)
all of that is data, so it is transferable in principle: a bundle, a validation pass, an
install.

The track answers whether it is transferable in practice — and whether the Hub is the
right place to hold it, which is a genuinely open question this document does not pretend
to have settled.

## Scope

Everything below is **tentative**. The track is not scheduled and its activation
conditions are in the [Phase Exit Decision](#phase-exit-decision).

### The genericity boundary decides what a bundle may contain

[ADR-0018's 2026-08-08 Amendment](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0018-tenant-driven-customization-model.md)
draws the line the marketplace has to respect. Inside the boundary — **content shape**,
**presentation**, and **pure rule evaluation** — everything is a pure function of tenant
data and already-recorded state, and everything is bundleable:

- `TenantContentType` JSON Schemas.
- `TenantPageBlock` definitions and their composite renderer-key references.
- `TenantLessonItemType` definitions and their player-key references.
- `TenantLevelTaxonomy` definitions.
- `TenantScoringRule` / `TenantCompletionRule` expressions — these are **evaluated**, not
  executed, which is what keeps them inside the boundary.
- `TenantCustomFieldDef` definitions.
- `TenantTemplateLibrary` templates.
- A manifest: required LearnStack version, required feature keys, sample fixtures.

Outside the boundary — and therefore **not extension points and not marketplace
content**:

| Excluded | Why |
|---|---|
| **Stateful entitlement** — credit packs, session quotas, make-up-class allowances | Requires a balance that is decremented, refunded, expired and audited. A JSON Schema declares shape; it cannot declare a ledger |
| **Tenant code execution** — running a learner's submitted code, scoring pronunciation from audio, any external capability invocation | Requires a sandbox, a runtime, a resource budget and a security boundary |

**Tenant code execution is out.** This is the clearest thing in the track and it needs
saying plainly, because a "code challenge runner" bundle is the single most requested
marketplace listing in any education platform. Installing one would mean installing a
sandbox as data. There is no extension point that can carry it, and inventing one would
reopen the plugin model that
[ADR-0011](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0011-extension-points.md) described and
ADR-0018 superseded. A tenant that needs code execution needs a **LearnStack release** —
a platform feature gated by plan — or an adapter to an external provider. It does not need,
and cannot have, a marketplace listing.

### The unresolved collision with ADR-0034

[ADR-0034](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md)'s
first invariant is that **the Hub stores no tenant content**. A marketplace listing is
authored by a tenant, describes that tenant's product, and often carries sample fixtures
drawn from that tenant's data. On the plainest reading, a listing *is* tenant content, and
storing listings in the Hub violates the invariant that the whole Hub/LearnStack
separation rests on.

This is not a detail to be worked out during implementation. It is the design question the
track opens with, and it must be answered by an ADR before any code.

**That ADR is a LearnStack ADR, not a Hub one.** It amends or reinterprets ADR-0034 and it
decides where tenant-authored data may live across the boundary, which makes it
cross-cutting by definition — so it is filed in `../LearnStack/docs/decisions/` under the
next free LearnStack number and owned by the cross-repository decision owner. It is **not**
a `HUB-NNNN` decision: that series is reserved for decisions affecting only the Hub
codebase, and a carve-out to a shared invariant is the opposite of that. Once the ADR is
accepted, both roadmaps reference it by number — this document and LearnStack's
[Phase 12 pointer](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-12-hub-marketplace.md) — so
neither side can activate the track against an unrecorded answer.

Three shapes worth considering, none of them chosen here:

1. **Metadata in the Hub, body outside.** The Hub stores listing metadata — title, author
   tenant, version, review state, install count — and the bundle body lives in object
   storage the Hub references by key and never parses. Whether an opaque blob the Hub owns
   the pointer to counts as "storing" is exactly the question the ADR has to answer.
2. **A separate service.** The marketplace is neither Hub nor LearnStack, with its own
   boundary and its own invariants. Cleanest against ADR-0034, most expensive to operate.
3. **A narrow amendment.** ADR-0034 gains a carve-out for published, tenant-consented,
   schema-only bundles containing no learner data. Cheapest, and the option most likely to
   be regretted, because carve-outs to an invariant are how invariants stop being
   invariants — which is precisely the failure ADR-0034 was written to correct.

### Aggregates (Hub-side, if option 1 or 3 is chosen)

- `MarketplaceListing` — a publishable bundle: name, description, author tenant, licence,
  review state.
- `MarketplaceListingVersion` — a semver-versioned snapshot.
- `MarketplaceInstall` — a record of a tenant installing a listing version.
- `MarketplaceReview` — optional tenant feedback.

### Publisher flow

- A tenant admin exports a subset of their customization data as a draft listing from
  LearnStack's Admin Studio.
- **Sandbox validation** boots a clean tenant fixture and applies the bundle end to end;
  any failure blocks publish. This is the gate that makes a listing trustworthy, and it is
  the most expensive part of the track to build.
- Operator review in the Hub for compliance and content sanity before public listing.
- Approved listings appear in the marketplace.

### Consumer flow

- A tenant admin browses the marketplace from Studio.
- Install pushes the bundle into the tenant's customization aggregates. **The transport for
  this push is part of the ADR above** — it is a crossing that carries tenant content, so
  it cannot be a casual addition to the endpoint set.
- Key collisions (a `key` the tenant already uses) prompt for rename or skip; they are
  never silently overwritten.

### Pricing

**Paid listings are out of the roadmap pending product evidence.** No phase owns them, and
no slot is reserved. The first iteration, if it ships at all, is free-only: no listing
fees, no per-install pricing, no revenue split. If a free marketplace demonstrates real
demand, a pricing model is scoped at that point against what the demand actually looks
like.

## Deliverables

Tentative, and contingent on the ADR above:

- The ADR resolving the ADR-0034 collision — **first, and blocking everything else**.
- Marketplace aggregates and schema, wherever the ADR puts them.
- The sandbox validator: a clean LearnStack tenant fixture, bundle application, and a
  pass/fail report a publisher can act on.
- Operator review queue and approval flow in the
  [operator portal](p02c-4-operator-portal.md).
- Tenant-facing marketplace browser inside LearnStack's Admin Studio.
- The install transport, in whatever form the ADR sanctions.
- A publishing policy covering what a bundle may contain, enforced by the validator rather
  than by review.

## Completion Criteria

If the track is activated, it is complete when:

- A tenant exports a customization bundle, it passes sandbox validation against a clean
  fixture, an operator approves it, and a second tenant installs it and renders the
  result — with no code change on either side.
- An installed bundle collides with an existing key and the consumer is asked, not
  overwritten.
- A bundle containing anything outside the genericity boundary — a stateful-entitlement
  definition, an external-capability invocation, anything that would need a runtime — is
  rejected by the validator, with a message naming why.
- No published bundle contains learner data, tenant user records, or any content entry the
  publisher did not explicitly mark as a sample fixture.
- `Hub_NeverStores_TenantData` is green under whatever storage arrangement the ADR chose —
  which is the mechanical expression of the collision being genuinely resolved rather than
  argued away.

## Risks

- **The invariant collision resolved by convenience.** The cheapest answer is to amend
  ADR-0034 and move on. That is how "closed at four endpoints" ended up tunnelling a
  private key through a cached projection. Mitigated by requiring the ADR to state what
  the invariant still forbids after the amendment — an invariant that forbids nothing new
  is not an invariant.
- **Publisher data leakage.** A bundle's sample fixtures are the natural place for real
  learner data to escape one tenant into every installer. Mitigated by the validator
  refusing any fixture that is not explicitly authored as a fixture, and by treating a
  leak here as a data-protection incident rather than a bug.
- **Bundles as an injection surface.** A bundle is JSON, but JSON that names renderer keys
  and can carry `embed-html` block content is executable-adjacent. Mitigated by the
  sanitisation contract in
  [Architecture 32](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/32-tenant-customization-model.md)
  applying to installed content exactly as it applies to authored content — installation
  is not a trust boundary crossing that grants privileges.
- **Operator review burden at scale.** Manual review is the quality gate and the
  bottleneck; it does not survive volume. Mitigated by pushing everything mechanisable
  into the validator, so review is a judgement call on a small residue.
- **Drift after local customization.** A tenant installs a bundle, edits it, and the
  publisher ships v2. There is no obvious correct merge. This is unsolved and should be
  scoped honestly if the track activates — the likely answer is that installs are
  snapshots and upgrades are re-installs with a diff, not merges.
- **Building it before the demand exists.** The most expensive risk. The sandbox validator
  alone is a substantial system, and a marketplace with four listings is worse than no
  marketplace because it advertises abandonment.

## Phase Exit Decision

This track has **no scheduled entry**. It activates only when two conditions are met
together, and neither is a matter of judgement:

1. **Demonstrated duplication.** At least two tenants in the same domain have
   independently authored substantially equivalent customization data. Until that has
   happened in production, sharing is a hypothesis about a market, not an observation of
   one. [LearnStack Phase 10](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-10-english-learning-mvp.md)
   exercises one tenant in depth; it is the evidence base, not the trigger.
2. **The ADR-0034 collision is resolved by a written, accepted ADR** that says where
   listings live and why that does not make the Hub a store of tenant content.

Until both hold, the roadmap reserves the slot and nothing more. If only the first ever
happens, the answer may still be that tenants share bundles by exporting and emailing
them — which costs nothing to support and is exactly how a real signal would show up
before anyone builds a marketplace to catch it.
