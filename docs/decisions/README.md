# Hub-Internal ADRs

Architecture Decision Records that affect **only** Hub. Cross-cutting decisions (LearnStack ↔ Hub contracts, deployment models, entitlement projection, custom-domain lifecycle) live in the [LearnStack core decisions corpus](../../../LearnStack/docs/decisions/) — not duplicated here.

## Numbering convention

Hub-internal ADRs use the **`HUB-NNNN`** prefix to keep their numbering disjoint from LearnStack core's `0001..0032+` series:

- `HUB-0001-stripe-webhook-idempotency.md`
- `HUB-0002-...`

The two numbering series never share numbers. A reference to "ADR-0019" unambiguously points to LearnStack core; "HUB-0001" unambiguously points here.

## When to write a Hub-internal ADR

Use this directory **only** for decisions that:

1. Affect the Hub codebase alone (no LearnStack core impact).
2. Are not already covered by a cross-cutting LearnStack ADR.

Examples that **belong** here:

- Stripe webhook idempotency strategy (Phase 09b).
- Hub-internal background-job orchestration (Hangfire vs. native).
- Operator-portal-specific UI patterns (e.g. tenant-row table virtualisation).

Examples that **do NOT** belong here (file in LearnStack core's `decisions/` instead):

- Hub HTTPS Contract Surface changes (new endpoint).
- Entitlement projection shape changes (cross-cutting).
- Custom-domain lifecycle changes (cross-cutting).
- Two-realm Keycloak boundary changes.
- Triple deployment model changes.

## Template

[template.md](template.md) — mirrors LearnStack core's ADR template.

## Status

P02c-0 ships zero Hub-internal ADRs. The first Hub-internal ADR is expected in Phase 09b (Stripe webhook idempotency).
