---
name: code-review
description: >
  Perform a thorough code review of a Hub change across security, bugs (and potential
  bugs), optimisation, refactor opportunities, and Hub-specific structural rules — or
  compose a detailed review-agent prompt that a separate agent will execute. USE FOR:
  self-review of your own diff before commit, end-to-end PR review, post-implementation
  gate before declaring implement-task done, or producing the review-agent prompt to
  dispatch a second-opinion agent. DO NOT USE FOR: standards-conformance only (use
  standards-check — narrower + faster, run it first), running tests (use
  run-tests-locally), or writing new tests (use add-integration-test / add-architecture-test).
---

# Reviewing a Hub change

## Purpose

The deep review pass: beyond mechanical conformance ([standards-check](../standards-check/SKILL.md)), look for security holes, real + latent bugs, performance traps, refactor opportunities, and Hub-structural violations. Either review directly, or compose the prompt for a delegated review agent.

## When to use

- Self-review before commit (Step 5/10 of [implement-task](../implement-task/SKILL.md)).
- End-to-end PR review.
- Composing a review-agent prompt for a second opinion.

## When not to use

- Mechanical conformance only → [standards-check](../standards-check/SKILL.md) (run it first).
- Running tests → [run-tests-locally](../run-tests-locally/SKILL.md).

## Review lenses

Walk all five. Cite `file:line` for every finding; classify Blocker / Major / Minor / Suggestion.

### 1. Security

- Secrets: nothing real in source (`.env.example` placeholders only; Leakwatch clean). Dev cert/key material stays outside the tree.
- The mTLS + RS256 JWT + HMAC chain (when it lands, P02c-2) is verified before any work; HMAC body-signature input is canonical + agreed with LearnStack core.
- `/api/internal/*` bound to the internal listener, never internet-exposed.
- `learnstack-hub` realm boundary: Hub never accepts a `learnstack` realm token; tenant surfaces never accept a `learnstack-hub` token.
- Webhook receivers (Phase 09b) verify the provider HMAC before any work + dedupe via `WebhookLedger`.

### 2. Bugs + potential bugs

- `generation` strictly monotonic (starts at 1, only +1, never resets) — the cache-coherency primitive; a reset is a silent stale-overwrite bug.
- State-machine transitions reject invalid moves with `Result.Fail`, not by throwing.
- Recompute fires on every trigger that changes the projection (subscription change, plan change); the Plans fan-out recompute hits **every** bound subscription.
- Cross-module calls go through `Application.Contracts`; no reach into another module's `Domain`/DbContext.
- EF: no N+1 in list queries; cursor pagination correct; JSONB columns round-trip; optimistic-concurrency `version` mapped.
- Nullability: no `!` suppressions hiding a real null path.

### 3. Optimisation

- List queries are cursor-paginated + indexed (`ix_`/`ux_`), not load-all-then-filter.
- The plan-change fan-out is bounded (note the Hangfire migration when volume warrants; an unbounded in-process loop is fine only at P02c-1 volumes).
- No per-request `ISecretProvider.GetAsync` on hot paths (bind at startup via `IOptions`).

### 4. Refactor / Clean Code

- Small single-responsibility methods; descriptive names; no dead code / commented-out blocks; no undated TODO.
- No premature abstraction (three similar lines beat a premature helper); no half-finished implementation.
- The change mirrors the LearnStack pattern it's based on (open `../LearnStack/backend/src/...` and compare) rather than improvising a parallel shape.

### 5. Hub-structural (the delta lens)

- Run the full [standards-check](../standards-check/SKILL.md) checklist as the structural backbone: no RLS, `OperatorId` not `UserId`, 6-step pipeline, `hub` schema, no tenant content, no LearnStack-core imports, closed four-endpoint surface.
- Architecture tests cover the new structure (`Hub_NeverStores_TenantData` scans the new module; dependency-direction test includes it).
- Adjacent docs updated (module deep dive, glossary, roadmap, ADR).

## Generating a review-agent prompt

When delegating to a second agent, compose a self-contained prompt that:

- Sets Hub context + **the five Hub deltas** ([../README.md](../README.md)).
- Names the commit / branch / file list under review (`git log --oneline`, `git show --stat`).
- Points at the LearnStack-side authority (`../LearnStack/docs/...`) the change derives from.
- Tells the agent to walk all five lenses above.
- Defines the output: verdict + findings as Blocker / Major / Minor / Suggestion with `file:line` + recommendation.
- Insists on reading the surrounding Hub docs (`docs/architecture/`, `docs/modules/`) — don't review in isolation.
- Reminds it to verify locally (`~/.dotnet/dotnet build/test`, leakwatch, compose config) — claims aren't proof.

Deliver the prompt as a copy-paste code-fence.

## Validation

- All five lenses walked; every finding carries `file:line` + a classification + a fix.
- The structural backbone (standards-check) is green or its fails are listed as Blockers.
- If delegating: the review-agent prompt is self-contained + copy-paste-ready.

## Common pitfalls

- **Skipping standards-check first.** It catches the mechanical fails fast so the deep pass focuses on judgment.
- **Reviewing in isolation.** Hub's correctness depends on the LearnStack-side contract; read the cited ADR/standard before judging.
- **Politeness over usefulness.** If something is broken, say Blocker. A "ship + 3 bugs" verdict that buries the bugs helps no one.
