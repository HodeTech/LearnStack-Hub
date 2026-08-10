---
name: commit-and-pr
description: >
  Format LearnStack Hub commits and pull requests to the project's conventions —
  Conventional Commits + AI co-author trailer + Hub scopes + cross-repo coordination
  notes. USE FOR: preparing a commit, opening a PR, picking the right Hub scope,
  handling a cross-repo (Hub + LearnStack) packet's paired PRs. DO NOT USE FOR:
  deciding whether a change is ready (that's code-review), force-pushing, or amending
  an Accepted ADR (write a new one).
---

# Committing + opening a PR in Hub

## Purpose

Produce commits + PRs that match Hub's conventions and surface cross-repo coordination where a packet spans both repos.

## When to use

- Preparing a commit for staged Hub changes.
- Opening a PR (only when the user asked).
- A packet that changes both Hub and LearnStack and needs paired PRs.

## When not to use

- Deciding readiness → [code-review](../code-review/SKILL.md) / [standards-check](../standards-check/SKILL.md).
- Force-push / amend on `main` → forbidden.

## Workflow

### Step 1 — Pre-commit sanity

Run the verification from [run-tests-locally](../run-tests-locally/SKILL.md) (build + tests + format verify + leakwatch) before committing. The pre-commit hook re-runs Leakwatch + `dotnet format` + prettier; let it reformat, re-stage, re-commit. (Backend-only commits don't trip the ESLint step.)

### Step 2 — Commit message

Conventional Commits `type(scope): subject`:

- **type:** `feat` | `fix` | `docs` | `refactor` | `test` | `chore` | `ci`.
- **Hub scopes:** `hub` (cross-cutting), `hub-domain` (aggregates / modules), `hub-infra` (compose / APISIX / Dapr / Vault / EF), `hub-portal` (operator portal), `hub-docs` (documentation).
- **subject:** imperative, ≤ 72 chars.
- **body:** one short paragraph saying _why_ (the diff is _what_). For a packet, name the packet (`P02c-1`).
- **trailer:** one `Co-Authored-By:` line per agent that **materially contributed**, each naming that agent's own identity — never a hard-coded string for a session some other agent ran. [CLAUDE.md § Commit conventions](../../../CLAUDE.md#commit-conventions) carries the per-runtime strings; LearnStack's [Git Workflow Standards § Trailers](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/14-git-workflow.md#trailers) is the authority if the two disagree. HEREDOC every multi-line message.

```bash
git commit -m "$(cat <<'EOF'
feat(hub-domain): P02c-1 — LearnStackTenant aggregate + DbContext + migration

<why, 1-3 sentences>

Co-Authored-By: <this session's agent identity>
EOF
)"
```

### Step 3 — Branch + push (only if asked)

Feature branch `feat/phase-02c-packet-N-<slug>`. Push with `-u` only when the user asks. Never `--force` to `main`.

### Step 4 — PR (only if asked)

`gh pr create` with the [pull_request_template.md](../../../.github/pull_request_template.md) shape: summary, the Phase 02c packet, ADRs/standards touched, the cross-repo coordination block, the test-plan checklist.

### Step 5 — Cross-repo coordination (if the packet spans both repos)

Per [CLAUDE.md § Cross-repo coordination](../../../CLAUDE.md): the Hub-side PR opens first (it carries the canonical contract shape); the LearnStack-side PR references the Hub PR's commit hash; both merge in the same session. Adding/changing a contract endpoint requires a new ADR in `../LearnStack/docs/decisions/` first. **Do not** push or merge anything in `../LearnStack` without explicit user permission — another agent may be active there.

## Validation

- Commit subject imperative ≤ 72 chars with a valid Hub scope.
- Body explains _why_; AI co-author trailer present.
- Multi-line message via HEREDOC.
- Cross-repo packets: paired-PR plan stated; no unilateral `../LearnStack` push.

## Common pitfalls

- **Pushing without being asked.** Default is local commit.
- **Amending / force-pushing `main`.** Forbidden — new commits only.
- **A contract change without an ADR.** Adding or reshaping an endpoint on the contract surface needs an ADR in `../LearnStack/docs/decisions/` first — it is a cross-repository agreement (ADR-0034).
- **Touching `../LearnStack` branch state from a Hub session.** Coordinate; don't interfere with a parallel agent.
