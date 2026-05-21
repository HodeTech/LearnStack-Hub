# LearnStack Hub — Documentation Catalogue

Hub documentation is intentionally **slim** — most architectural decisions live in the LearnStack core repo (cross-cutting authority) and Hub docs cross-link to them. The Hub-internal `docs/` directory holds only:

1. Hub-specific operational runbooks
2. Hub module deep-dives (Hub-only implementation details)
3. Hub-internal ADRs (decisions that affect only Hub)
4. Hub-specific glossary terms
5. Hub roadmap mirror (per-packet status)

## Directory layout

| Directory                        | Purpose                                                                                               | Numbering              |
| -------------------------------- | ----------------------------------------------------------------------------------------------------- | ---------------------- |
| [`architecture/`](architecture/) | Hub-internal architecture (repository layout, module topology) + contract pointers to LearnStack core | numbered `NN-topic.md` |
| [`decisions/`](decisions/)       | Hub-internal ADRs (Hub-only decisions; `HUB-NNNN` series)                                             | `HUB-NNNN-topic.md`    |
| [`operations/`](operations/)     | Hub operational runbooks (deployment, incident response, cert rotation)                               | unnumbered             |
| [`modules/`](modules/)           | Per-Hub-module deep dives + audit-coverage matrices                                                   | per-module subdirs     |
| [`roadmap/`](roadmap/)           | Phase 02c packet mirror (`P02c-0..P02c-7`)                                                            | unnumbered             |
| [`glossary.md`](glossary.md)     | Hub-specific terms (Plan, HubSubscription, etc.)                                                      | single file            |

## Authoritative cross-cutting docs (LearnStack core, not here)

Hub docs cite these via sibling-relative paths (`../../learnstack/...`):

- [ADR-0019 LearnStack Hub](../../learnstack/docs/decisions/0019-learnstack-hub.md) — the boundary contract
- [ADR-0020 Triple Deployment + Hybrid License](../../learnstack/docs/decisions/0020-triple-deployment-hybrid-license.md)
- [ADR-0021 Feature-Based Entitlement](../../learnstack/docs/decisions/0021-feature-based-entitlement.md)
- [ADR-0022 Custom Domain + TLS](../../learnstack/docs/decisions/0022-custom-domain-tls.md) (Amendment 1)
- [ADR-0004 Authentication Strategy](../../learnstack/docs/decisions/0004-authentication-strategy.md) (Amendment 1 — `learnstack-hub` realm)
- [Architecture 24 LearnStack Hub](../../learnstack/docs/architecture/24-learnstack-hub.md) — deep dive
- [Standards 20 § Hub HTTPS Contract Surface](../../learnstack/docs/standards/20-infrastructure-stack.md)
- [Standards 21 Architecture Tests Catalogue](../../learnstack/docs/standards/21-architecture-tests-catalogue.md)
- [Phase 02c roadmap](../../learnstack/docs/roadmap/phase-02c-hub-foundation.md)

## Engineering standards

Hub follows LearnStack's [Standards corpus](../../learnstack/docs/standards/) by reference. Hub does not maintain its own standards. If a Hub-internal-only rule emerges (e.g. "Stripe webhook idempotency strategy"), it lands as a Hub-internal ADR (`HUB-NNNN`), not as a standards file.

## Skills

Hub repo does not maintain its own skill catalogue. Both Claude Code and Codex consume `../learnstack/.claude/skills/`. The same entry-point selection rules apply (use `implement-task` for substantive work, `start-task` for scoping, `standards-check` + `code-review` for review).
