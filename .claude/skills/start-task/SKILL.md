---
name: start-task
description: >
  Lightweight, scoping-only pass on a LearnStack Hub task — read the right docs in
  the right order, check packet fit, walk the Hub deltas + hard rules, and identify
  which workflow skill the eventual implementation will need. Produces a plan, not
  code. USE FOR: planning before knowing what to build, orientation on an unfamiliar
  area, a "what would this involve?" question, or as the Step 1 pass dispatched by
  implement-task (which does this automatically — don't double-invoke). DO NOT USE
  FOR: substantive implementation (use implement-task), trivial one-line fixes, or
  follow-up turns inside a task already in flight.
---

# Scoping a Hub task

## Purpose

Orient correctly before writing code: read the right docs, confirm the work fits the current packet, walk the Hub deltas + hard rules, and name the workflow skill the implementation will use. Stops at the plan.

## When to use

- "Plan / scope / orient me on X, don't implement yet."
- The first pass [implement-task](../implement-task/SKILL.md) runs internally (don't invoke both — implement-task dispatches this).

## When not to use

- You're ready to implement → [implement-task](../implement-task/SKILL.md).
- A trivial edit → just edit.

## Workflow

### Step 1 — Read, in order

1. [README.md](../../../README.md) — direction at a glance.
2. The relevant Hub design spec: `docs/architecture/module-topology.md`, `docs/architecture/cross-cutting-foundation.md`, `docs/architecture/entitlement-projection.md`, and the `docs/modules/<name>.md` for the module(s) in scope.
3. The LearnStack-side authority the spec derives from: the cited ADR(s) under `../LearnStack/docs/decisions/` and standard(s) under `../LearnStack/docs/standards/`.
4. [docs/roadmap/README.md](../../../docs/roadmap/README.md) — which packet owns this work.
5. [docs/glossary.md](../../../docs/glossary.md) — terms.

Read, don't skim. If `git log` shows recent edits to the surface, read the commit messages.

### Step 2 — Check packet fit

Confirm the task belongs to the current/next packet (`docs/roadmap/README.md`). If it belongs to a later packet, say so and stop — don't pull future work forward. If it requires LearnStack-side changes, flag it as a cross-repo coordinated packet (it's not Hub-only).

### Step 3 — Walk the Hub deltas + hard rules

Confirm the planned change honours [the Hub deltas](../README.md) and [CLAUDE.md § Hard rules](../../../CLAUDE.md): no RLS, `OperatorId` not `UserId`, 6-step pipeline, `hub` schema, no tenant content, no LearnStack-core imports, the two ADR-0034 contract invariants. Name any rule the change brushes against.

### Step 4 — Name the workflow skill(s)

Identify which `add-*` / `wire-*` skill the implementation will invoke (e.g. [add-hub-aggregate](../add-hub-aggregate/SKILL.md) + [add-ef-migration](../add-ef-migration/SKILL.md) + [add-mediatr-handler](../add-mediatr-handler/SKILL.md)). If none fits, fall back to LearnStack's [Standards index](../../../../LearnStack/docs/standards/README.md) + the relevant Hub architecture doc.

### Step 5 — Produce the plan

A short plan: problem statement (your words), the packet, the governing standards/specs, the files/dirs the work will touch, the workflow skill(s), and any open assumption. Stop here unless the user asked to implement.

## Validation

- The plan names the packet, the governing docs, the workflow skill(s), and the files in scope.
- Any Hub-delta or cross-repo concern is surfaced.

## Common pitfalls

- **Skimming instead of reading.** The implementation cost of a sloppy scope is paid back tenfold.
- **Pulling future-packet work forward.** Respect the packet boundary.
- **Missing the cross-repo flag.** A task that needs `../LearnStack` changes is not a Hub-only packet.
