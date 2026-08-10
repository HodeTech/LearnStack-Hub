# LearnStack Hub — Documentation Catalogue

Hub documentation is intentionally **slim**. Cross-cutting architectural decisions live in the LearnStack core repository and Hub docs link to them rather than restating them. What lives here is what only the Hub can own:

1. The **Hub roadmap** — the plan for this repository, one document per packet
2. Hub-internal architecture (repository layout, module topology, the contract pointers)
3. Hub module deep-dives (Hub-only implementation details)
4. Hub-internal ADRs (decisions that affect only the Hub)
5. Hub operational runbooks
6. Hub-specific glossary terms

## Directory layout

| Directory                        | Purpose                                                                                                                                                              | Numbering              |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------- |
| [`roadmap/`](roadmap/README.md)   | **The authoritative Hub plan.** One document per packet (`P02c-0` … `P02c-7`) plus the post-02c billing and marketplace tracks. Not a mirror of a LearnStack file — LearnStack's `phase-02c` covers only its own side of the boundary and links here for the rest. | `pNNc-N-topic.md`      |
| [`architecture/`](architecture/README.md) | Hub-internal architecture — repository layout, module topology, cross-cutting foundation, entitlement projection — plus the contract pointers to LearnStack core | flat, unnumbered       |
| [`decisions/`](decisions/)       | Hub-internal ADRs (Hub-only decisions; `HUB-NNNN` series so they never collide with LearnStack ADR numbers)                                                            | `HUB-NNNN-topic.md`    |
| [`operations/`](operations/)     | Hub operational runbooks (deployment, incident response, cert rotation)                                                                                               | unnumbered             |
| [`modules/`](modules/)           | Per-Hub-module deep dives + audit-coverage matrices                                                                                                                   | per-module files       |
| [`glossary.md`](glossary.md)     | Hub-specific terms (`Plan`, `HubSubscription`, `Entitlement`, `Hub Operator`)                                                                                         | single file            |

Every roadmap packet document carries the same six sections as a LearnStack phase document: Goal, Scope, Deliverables, Completion Criteria, Risks, Phase Exit Decision.

## Authoritative cross-cutting docs (LearnStack core, not here)

Hub docs cite these by absolute URL (`https://github.com/HodeTech/LearnStack/blob/main/docs/...`), per [Documentation Standards § Layout](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/13-documentation.md). Relative paths do not cross a repository boundary on github.com and depend on a sibling checkout being present and identically capitalised.

- [ADR-0019 LearnStack Hub](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0019-learnstack-hub.md) — why the Hub is a separate repository
- [ADR-0034 Hub Contract Surface Invariant](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md) — the two invariants that replaced the "closed at four endpoints" rule, and the authoritative endpoint table
- [ADR-0035 Demand-Gated Infrastructure](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0035-demand-gated-infrastructure.md) — why the Hub integration waits on a written trigger
- [ADR-0033 Audit Durability Model](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0033-audit-durability-model.md) — MUST-class audit as durable intent (supersedes ADR-0016)
- [ADR-0020 Triple Deployment + Hybrid License](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md)
- [ADR-0021 Feature-Based Entitlement](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0021-feature-based-entitlement.md)
- [ADR-0022 Custom Domain + TLS](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0022-custom-domain-tls.md) (Amendment 1; its cert-delivery step is superseded by ADR-0034)
- [ADR-0004 Authentication Strategy](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0004-authentication-strategy.md) (Amendment 1 — `learnstack-hub` realm)
- [Architecture 24 LearnStack Hub](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md) — the deep dive
- [Standards 20 Infrastructure Stack](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/20-infrastructure-stack.md)
- [Standards 21 Architecture Tests Catalogue](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/21-architecture-tests-catalogue.md)
- [Phase 02c — LearnStack side](https://github.com/HodeTech/LearnStack/blob/main/docs/roadmap/phase-02c-hub-foundation.md)

## Engineering standards

Hub follows LearnStack's [Standards corpus](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/) by reference. Hub does not maintain its own standards. If a Hub-internal-only rule emerges (for example a Stripe webhook idempotency strategy), it lands as a Hub-internal ADR (`HUB-NNNN`), not as a standards file.

## Skills

**This repository maintains its own skill catalogue** at [`.claude/skills/`](../.claude/skills/README.md) — 18 skills, git-tracked through an un-ignore rule in `.gitignore`. Agents running from the Hub repository root load them from there, not from the sibling LearnStack repo.

The catalogue is Hub-tailored: the `add-hub-*` workflows encode Hub's deltas from LearnStack core — no Row Level Security, `OperatorId` rather than `UserId`, a six-step MediatR pipeline rather than eight, the `hub` schema, the `learnstack_hub` database. Entry-point selection is unchanged: `implement-task` for substantive work, `start-task` for scoping, `standards-check` then `code-review` for review.
