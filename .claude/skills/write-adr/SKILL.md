---
name: write-adr
description: >
  Author a Hub-internal Architecture Decision Record (`HUB-NNNN` series) using the
  Decision Drivers + Considered Options template. USE FOR: a one-time decision that
  affects ONLY the Hub codebase (e.g. Stripe webhook idempotency strategy, Hub
  background-job orchestration, operator-portal-specific pattern). DO NOT USE FOR:
  cross-cutting decisions that touch the Hub↔LearnStack contract, entitlement
  projection shape, custom-domain lifecycle, two-realm boundary, or deployment model
  (those are LearnStack ADRs — filed in the `HodeTech/LearnStack` repo under
  `docs/decisions/`, as a coordinated pull request), editing an
  Accepted ADR's decision section (write a superseding ADR), or day-to-day choices
  (those go in code review / commit messages).
---

# Writing a Hub-internal ADR

## Purpose

Capture a Hub-only architectural decision durably, with its drivers and the options considered, in the `HUB-NNNN` numbering series — disjoint from LearnStack's `0001..` series.

## When to use

- A decision affects the Hub codebase alone and isn't already covered by a cross-cutting LearnStack ADR.
- Examples: Stripe webhook idempotency strategy (Phase 09b), Hub Hangfire-vs-native job orchestration, operator-portal table-virtualisation pattern.

## When not to use

- The decision touches the Hub↔LearnStack contract / entitlement shape / custom-domain lifecycle / two-realm boundary / deployment model → it's a **LearnStack** ADR; it is filed in the `HodeTech/LearnStack` repo under `docs/decisions/` as its own pull request, which **merges before** either code PR opens (coordinated, with user permission). Template and numbering: [https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/).
- Editing an Accepted ADR's Decision section → write a new ADR that supersedes it.
- A routine implementation choice → commit message / code review.

## Workflow

### Step 1 — Confirm it's Hub-internal

Check the "does NOT belong here" list in [docs/decisions/README.md](../../../docs/decisions/README.md). If the decision is cross-cutting, stop and file it on the LearnStack side instead.

### Step 2 — Reserve the next number

Scan `docs/decisions/` for the highest `HUB-NNNN`; the next free integer is yours. Numbers are sequential and never reused.

### Step 3 — Author from the template

Copy [docs/decisions/template.md](../../../docs/decisions/template.md) to `docs/decisions/HUB-NNNN-<kebab-title>.md`. Fill: Status (Draft → Accepted), Date, Decision (present-tense, declarative), Context, Decision drivers (priority-ordered), Considered options (≥2, each with Pros/Cons; mark the chosen one), Decision outcome, Consequences (Positive / Negative / Neutral), Implementation notes (packet ownership), References.

### Step 4 — Cross-link

Link the ADR from the docs it affects (module deep dive, architecture doc) and from `docs/decisions/README.md` if the README keeps an index. Use [update-glossary](../update-glossary/SKILL.md) if the ADR introduces a Hub term.

## Validation

- File is `docs/decisions/HUB-NNNN-<title>.md`; number is the next free integer.
- The decision is genuinely Hub-internal (not cross-cutting).
- Template sections all present; ≥ 2 considered options; chosen option marked.
- Present-tense Decision; cross-links in place.

## Common pitfalls

- **Filing a cross-cutting decision as `HUB-NNNN`.** If LearnStack core's behaviour depends on it, it's a LearnStack ADR.
- **Reusing a number.** Sequential, never reused.
- **Editing an Accepted ADR's Decision.** Supersede instead; Accepted ADRs are immutable except dated Amendments.
