# Contributing to LearnStack Hub

LearnStack Hub follows the same engineering rigour as [LearnStack core](../learnstack). Most engineering standards are defined once in LearnStack's [Standards corpus](../learnstack/docs/standards/) and apply here by reference.

## Branch protection

Required status checks on `main`:

- `backend` — `dotnet build` + format verify + unit + architecture + contract tests
- `frontend` — pnpm install + typecheck + lint + build + Vitest
- `meta` — `make lint`-style format verification + Markdown link audit
- `secret-scan` — Leakwatch scan (gates per LearnStack Standards 12 § Secrets Management)

`backend-integration` is **deferred** with `if: false` until P02c-2 lands the first Testcontainers-backed test.

## Commit conventions

- Conventional Commits format: `type(scope): subject`
- Subject in imperative mood; ≤ 72 characters
- Hub-specific scopes:
  - `hub` — cross-cutting Hub changes
  - `hub-portal` — operator portal (Next.js)
  - `hub-domain` — Hub domain model
  - `hub-infra` — Hub infrastructure (compose, APISIX, Dapr, Vault)
  - `hub-docs` — Hub documentation
- AI co-author trailer (per [LearnStack AGENTS.md § Trailers](../learnstack/AGENTS.md)):
  - Claude Code: `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>`
  - Codex: `Co-Authored-By: Codex Opus 4.7 (1M context) <noreply@anthropic.com>`

Example commit message:

```
feat(hub-domain): scaffold LearnStackTenant mirror aggregate

P02c-1 brings the Hub-side mirror of LearnStack's Tenant aggregate.
Hub holds only metadata fields (id, slug, display_name, status,
deployment_mode, created_at, last_phone_home_at).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
```

## Cross-repo PRs

When a packet changes both `learnstack` and `learnstack-hub` (e.g. P02c-3 lands `HubEntitlementProvider` on the LearnStack side + Hub-side internal-API endpoints), the two PRs **must be coordinated**:

1. Open the Hub-side PR first; it carries the canonical contract shape.
2. Open the LearnStack-side PR referencing the Hub PR's commit hash.
3. Merge both in the same session — either-side merge alone leaves the contract dangling.

Adding or changing a cross-repo contract endpoint requires a **new ADR in `learnstack/docs/decisions/`** (per the Hub HTTPS Contract Surface rule). See [Standards 20 § Hub HTTPS Contract Surface](../learnstack/docs/standards/20-infrastructure-stack.md).

## Pre-commit hook

```bash
make install   # activates .githooks/pre-commit
```

The hook runs, in order:

1. **Leakwatch** — a single repo-root scan (`leakwatch scan fs .`) whenever any file is staged. The current Leakwatch CLI takes a directory target (not per-file); the `.leakwatch.yaml` + `.leakwatchignore` filter out lock files / build artifacts / dev-only literals so the scan stays fast for everyday commits.
2. **`dotnet format`** on staged `.cs` files.
3. **Prettier `--write`** on staged `.ts/.tsx/.js/.jsx/.mjs/.cjs/.json/.md` files.
4. **ESLint `--fix`** on staged JS-like files.

To bypass locally for an emergency fix: `git commit --no-verify`. CI re-runs every check the hook runs, so a bypassed commit will fail the PR build (except for the ESLint step — see Known DX gaps below).

## Known DX gaps (P02c-0)

These are open trade-offs that the bootstrap deliberately punts to a later packet. Each is documented here so contributors hitting them have a single place to find the rationale.

### ESLint v9 + legacy `.eslintrc.cjs` incompatibility

The pre-commit hook's ESLint `--fix` step **does not work** today. ESLint v9 dropped support for the legacy `.eslintrc.cjs` configuration format that this repo (and LearnStack core) still use. The hook will fail with:

> ESLint couldn't find an eslint.config.(js|mjs|cjs) file.

**Workaround:** Use `git commit --no-verify` for commits that touch staged JS-like files. The hook's other steps (Leakwatch, dotnet format, prettier) still ran before ESLint failed, so format hygiene is preserved.

**Why CI does not catch this:** the `frontend` CI job runs `pnpm -r lint`, which calls `next lint`. `next lint` uses its own ESLint integration with a compat shim and still consumes `.eslintrc.cjs` correctly. Only the hook's direct `pnpm exec eslint --fix` invocation hits the migration gap.

**Tracked for migration:** [`apps/operator-portal/.eslintrc.cjs`](frontend/apps/operator-portal/.eslintrc.cjs) carries a `TODO` comment pointing at the Next 16 codemod (`npx @next/codemod@canary next-lint-to-eslint-cli .`). P02c-4 will migrate every package to flat config (`eslint.config.mjs`) and drop the workaround.

### Hub `SharedKernel` is a local copy of LearnStack patterns

Hub's `LearnStack.Hub.SharedKernel` mirrors LearnStack core's `LearnStack.SharedKernel` pattern (Result&lt;T&gt;, Entity&lt;TId&gt;, LocalizedMessage, IClock, Vogen IdMask) but **does not** import from LearnStack core. This is deliberate — the two repos are released independently, and a shared NuGet package would couple their release cadences.

**Tracked for re-evaluation:** Phase 11 may extract a `LearnStack.Foundation` NuGet package once the divergence cost is measurable.

### Hub Keycloak realm JSON lives in the LearnStack core repo

The `learnstack-hub` realm export (`../learnstack/infra/keycloak/realms/learnstack-hub.json`) physically lives in the sibling repo because LearnStack core's compose stack imports both realms at first boot. See [`infra/keycloak/README.md`](infra/keycloak/README.md) for the operational topology.

## Issue / PR templates

`.github/pull_request_template.md` carries the canonical PR template. Reference the relevant LearnStack ADR or Phase 02c packet in every PR description.
