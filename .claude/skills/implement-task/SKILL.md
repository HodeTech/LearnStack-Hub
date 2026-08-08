---
name: implement-task
description: >
  Take a substantive LearnStack Hub task from intent to ship: scope, inspect, plan,
  implement against the Hub deltas + LearnStack standards, self-check, run linter +
  tests, update docs, commit, and produce a review-agent prompt. USE FOR: any
  non-trivial change that produces real artefacts and needs verification + docs + a
  clean commit (a new Hub module, an aggregate, the cross-cutting foundation, a
  migration, a Hub-internal ADR + downstream updates). DO NOT USE FOR: a one-line
  typo fix (just edit), an exploratory question (chat), scoping-only sessions (use
  start-task), reviewing someone else's diff (use code-review), or a standards-only
  audit (use standards-check).
---

# Implementing a Hub task end-to-end

## Purpose

Run a substantive Hub task with the discipline the project expects: scope it, understand the affected surface, implement against the **Hub deltas** ([.claude/skills/README.md § The Hub deltas](../README.md)) + LearnStack's standards corpus, prove it works, keep docs synchronised, commit cleanly, and hand the reviewer a ready prompt. This skill is the expansion of the long instruction prompt that would otherwise be repeated every time.

## When to use

- The user handed you a substantive Hub task ("implement", "geliştir", "yap") with no further breakdown.
- The work spans multiple files, needs tests, and needs doc updates.

## When not to use

- A trivial one-line edit → just edit.
- Pure scoping → [start-task](../start-task/SKILL.md), stop at the plan.
- Reviewing work you didn't author → [code-review](../code-review/SKILL.md).
- A standards-conformance walk → [standards-check](../standards-check/SKILL.md).
- A docs-only change → edit + [commit-and-pr](../commit-and-pr/SKILL.md) directly.

## Inputs

| Input                    | Required | Description                                                                                    |
| ------------------------ | -------- | ---------------------------------------------------------------------------------------------- |
| Task description         | Yes      | The user's request.                                                                            |
| Acceptable scope         | No       | If unstated, infer + confirm in the plan step.                                                 |
| Allowed to commit / push | Yes      | Default: local commit on a feature branch. Never `--force` to `main`. Push / PR only if asked. |

## Workflow

The ten steps are mandatory; skipping any is the bug this skill prevents.

### Step 1 — Scope and alignment

Run [start-task](../start-task/SKILL.md): read the right docs in order (Hub docs under `docs/`, then LearnStack authority under `../LearnStack/docs/`), confirm phase fit against `docs/roadmap/README.md`, walk the [CLAUDE.md hard rules](../../../CLAUDE.md), and pick the specific workflow skill(s) you'll invoke ([add-hub-module](../add-hub-module/SKILL.md), [add-hub-aggregate](../add-hub-aggregate/SKILL.md), [add-mediatr-handler](../add-mediatr-handler/SKILL.md), [wire-cross-cutting-foundation](../wire-cross-cutting-foundation/SKILL.md), …). Output: a one-paragraph problem statement in your own words, the packet it belongs to, the standards that govern it, the skill(s) you'll use.

### Step 2 — Inspect and understand

Read every file the change touches **before** editing. Trace one hop out (who calls this, who reads this table, what events flow). Read the relevant Hub design spec (`docs/architecture/*.md`, `docs/modules/*.md`) and the LearnStack-side ADR/standard it derives from. If `git log` shows recent edits, read the commit messages for direction. **Mirror, don't invent:** if a pattern exists in `../LearnStack/backend/src/`, open it and reproduce it (adjusting for the Hub deltas), rather than improvising.

### Step 3 — Plan

State the plan briefly: (1) what you'll do in 1-3 sentences; (2) which files/dirs (paths only); (3) which validation you'll run; (4) any assumption that, if wrong, invalidates the plan. Ask for confirmation **only** when the plan touches: more than one module's `Domain`; an Accepted ADR (either repo); the Hub HTTPS contract surface; a destructive migration; or anything in `../LearnStack`. For routine Hub work, state the plan and continue.

### Step 4 — Implement

Implement against the Hub deltas + LearnStack standards + Clean Code defaults (small functions, descriptive names, no dead code, no commented-out blocks, no undated TODO). **Never** introduce RLS / `[TenantOwned]` / tenant-context machinery (Hub has none). **Always** use `OperatorId` not `UserId`. Don't drift into surrounding refactors. No half-finished implementations (a handler unwired, a migration unapplied, an aggregate without its DbContext config is worse than nothing).

### Step 5 — Self-check

Before running the toolchain, run [standards-check](../standards-check/SKILL.md) over your own diff. Focus: cross-module references (dependency direction); no RLS slipped in; `OperatorId` not `UserId`; no LearnStack-core imports; no tenant-content types; adjacent docs gone stale (module doc, glossary, roadmap, ADR cross-link). Fix anything off **now**, not at review time.

### Step 6 — Linter + tests

Use [run-tests-locally](../run-tests-locally/SKILL.md): `~/.dotnet/dotnet build LearnStack.Hub.slnx`, the architecture suite, the unit suite, and the Testcontainers integration suite where the change warrants it. Frontend changes → `pnpm -r typecheck/lint/build/test`. Architecture tests are non-skippable. A failing test means keep going — fix the root cause.

### Step 7 — Update every related document

Walk the list, leave nothing stale: `docs/roadmap/README.md` (packet status); the affected `docs/modules/<name>.md` / `docs/architecture/*.md`; `docs/glossary.md` (new term → [update-glossary](../update-glossary/SKILL.md)); a Hub-internal ADR if a new rule emerged → [write-adr](../write-adr/SKILL.md); the sibling-link audit (CI's `meta` job pattern — `(\.\./)+learnstack/` links are validated locally only).

### Step 8 — Commit

Run [commit-and-pr](../commit-and-pr/SKILL.md). Conventional Commit `type(scope): subject` with a Hub scope (`hub`, `hub-domain`, `hub-infra`, `hub-portal`, `hub-docs`); imperative ≤ 72 chars; body says _why_; AI co-author trailer. HEREDOC multi-line messages. Local commit only unless asked.

### Step 9 — Summary

Two-minute summary: **what changed** (grouped by area), **why it serves the user in Turkish** (the user reads this), **verification done** (which suites green), **open follow-ups** (linked so they don't drop).

### Step 10 — Review-agent prompt

Compose the review prompt per [code-review § review-agent prompt](../code-review/SKILL.md): Hub context + the Hub deltas, the commit/branch/files under review, instruction to walk security + bugs + optimisation + refactor + Hub-structural conformance, output format (Blocker/Major/Minor/Suggestion), and "read the surrounding docs, don't review in isolation." Deliver as a copy-paste code-fence.

## Validation (Definition of Done)

- [ ] Step 1 problem statement written; packet fit confirmed.
- [ ] Step 4 walked the right workflow skill(s); no Hub-delta violations (RLS/UserId/LearnStack-import/tenant-content).
- [ ] Step 5 self-check green.
- [ ] Step 6 linter + tests green (or failure documented + tracked); architecture suite ran.
- [ ] Step 7 adjacent docs updated; sibling-link audit clean.
- [ ] Step 8 commit on the right branch with the right trailer.
- [ ] Step 9 summary delivered incl. the Turkish "ne işe yaradı".
- [ ] Step 10 review-agent prompt provided as a copy-paste block.

## Common pitfalls

- **Rushing Step 2.** The next eight steps cost an order of magnitude more when inspect is sloppy.
- **Forgetting the Hub deltas.** Copying a LearnStack pattern verbatim drags in RLS / `UserId` / `TenantContextBehavior` that Hub must not have. Adjust as you mirror.
- **Skipping the dotnet PATH fix.** `dotnet` is .NET 9; use `~/.dotnet/dotnet`. #1 time-sink.
- **Touching `../LearnStack` without coordination.** Hub-side packets are Hub-only; LearnStack-side work is a separate coordinated packet.
- **Stale docs / dropped Turkish summary / missing review prompt.** All three are part of "done."
