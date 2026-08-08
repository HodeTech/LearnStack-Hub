# LearnStack Hub Agent Skills

Reusable, task-focused instruction packs ("skills") for AI coding agents working on **LearnStack Hub**. Each subdirectory is one skill; its `SKILL.md` carries YAML frontmatter (`name`, `description`) so the agent runtime can pick or skip it without reading the whole body.

Skills are **project-local** to this repo (`.claude/skills/`). An agent running from the `learnstack-hub` root loads them automatically. They cite LearnStack core's standards / ADRs by sibling path (`../LearnStack/docs/...`) for cross-cutting authority and carry only the **Hub-specific delta** on top — they never duplicate the LearnStack standards corpus.

## The Hub deltas every skill assumes

These are the load-bearing differences from LearnStack core that the `add-hub-*` skills encode. If you only remember five things, remember these:

1. **No RLS.** Hub is operator-administered, not tenant-isolated. No Row-Level Security, no `[TenantOwned]`/`[OrganizationScoped]` markers, no EF global query filters, no `app.tenant_id` GUC, no tenant-context MediatR behavior. (`docs/architecture/module-topology.md`.)
2. **`OperatorId`, never `UserId`.** Hub actors are operators (the `learnstack-hub` Keycloak realm), never tenant users. Audit columns + `CapturedContext` use `OperatorId`.
3. **6-step MediatR pipeline** (not LearnStack's 8): Validation → Logging → AuditLog → Authorization → Transaction → OutboxFlush → Handler. No `TenantContextBehavior`.
4. **`hub` schema in the `learnstack_hub` database.** Every DbContext `HasDefaultSchema("hub")`.
5. **Hub stores no tenant content.** `Hub_NeverStores_TenantData` enforces; never add `Course`/`Lesson`/`Enrollment`/`LiveSession`/`LessonItem`/`MediaAsset`/tenant-`User`.

Plus the environment trap: **.NET 10 SDK is at `~/.dotnet/dotnet`**, not the default PATH (system `dotnet` is .NET 9). Prefix every command or `export PATH="$HOME/.dotnet:$PATH"`.

## How to use

### Which skill runs first?

Pick the entry point matching the user's intent. Only **one** entry point runs per task — it dispatches the rest internally.

| User's intent                                                       | Entry point                                                                           | What it does                                                                                                                                                              |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| "Implement / geliştir / yap / ekle / refactor X" (substantive work) | **[implement-task](implement-task/SKILL.md)**                                         | Default for any non-trivial change. Dispatches `start-task`, then the `add-*` workflow skill(s), runs linter + tests, updates docs, commits, emits a review-agent prompt. |
| "Plan / scope / orient me, don't implement yet"                     | [start-task](start-task/SKILL.md)                                                     | Standalone scoping pass. Reading order + hard-rule walk + which workflow skill to use. Stops at the plan.                                                                 |
| "Review this diff / PR"                                             | [standards-check](standards-check/SKILL.md), then [code-review](code-review/SKILL.md) | Mechanical conformance gate first, then security + bug + optimisation + refactor + Hub-structural review.                                                                 |
| "Explain / what is …"                                               | none — answer directly                                                                | Don't load a skill to chat.                                                                                                                                               |
| One-line typo fix                                                   | none — edit directly                                                                  | Skills are for workflows.                                                                                                                                                 |

## Skill catalogue

### Process

| Skill                                       | When to use                                                                                                               |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| [implement-task](implement-task/SKILL.md)   | The default entry point for substantive work — scope, implement, self-check, test, docs, commit, review prompt.           |
| [start-task](start-task/SKILL.md)           | Lightweight scoping-only entry point. Reading order + alignment check.                                                    |
| [write-adr](write-adr/SKILL.md)             | Capturing a **Hub-internal** decision (`HUB-NNNN` series). Cross-cutting decisions go in `../LearnStack/docs/decisions/`. |
| [update-glossary](update-glossary/SKILL.md) | Introducing a Hub-specific term in `docs/glossary.md`.                                                                    |
| [commit-and-pr](commit-and-pr/SKILL.md)     | Conventional Commit + AI trailer + Hub PR conventions (incl. cross-repo coordination).                                    |

### Review

| Skill                                       | When to use                                                                                                                           |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| [standards-check](standards-check/SKILL.md) | Mechanical conformance pass against the Hub deltas + LearnStack standards + CLAUDE.md hard rules. Run first; faster + narrower.       |
| [code-review](code-review/SKILL.md)         | End-to-end review (security / bugs / optimisation / refactor / Hub-structural). Also composes the review-agent prompt for delegation. |

### Backend — core workflows

| Skill                                                                   | When to use                                                                                                                                                                                                 |
| ----------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [wire-cross-cutting-foundation](wire-cross-cutting-foundation/SKILL.md) | One-time foundation wiring for the Hub host: `HubExceptionHandler`, the **6-step** MediatR pipeline, Serilog + OTel, `IErrorTrackingProvider`, `IProviderResilience<TPort>`, `OperatorId`. P02c-1 keystone. |
| [add-hub-module](add-hub-module/SKILL.md)                               | Scaffolding a new Hub module (`LearnStack.Hub.Modules.<Name>.*` four-package layout).                                                                                                                       |
| [add-hub-aggregate](add-hub-aggregate/SKILL.md)                         | Adding a Hub aggregate (Vogen ID, `Entity`/`AuditableEntity`, **no RLS**, `OperatorId` audit). The Hub analogue of LearnStack's `add-tenant-owned-entity` — minus the entire tenant-isolation layer.        |
| [add-mediatr-handler](add-mediatr-handler/SKILL.md)                     | Adding a command / query handler in the 6-step pipeline (`Result`, FluentValidation).                                                                                                                       |
| [add-ef-migration](add-ef-migration/SKILL.md)                           | EF Core migration in the `hub` schema; forward-only; no RLS.                                                                                                                                                |
| [add-integration-event](add-integration-event/SKILL.md)                 | Publishing / consuming a `learnstack.hub.*` integration event via outbox + Dapr.                                                                                                                            |
| [add-feature-key](add-feature-key/SKILL.md)                             | Adding a `FeatureKey` / `LimitKey` to the Hub registry that plans author into entitlements.                                                                                                                 |

### Tests

| Skill                                                   | When to use                                                                                                                                                  |
| ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| [add-architecture-test](add-architecture-test/SKILL.md) | Encoding a Hub structural rule (no-RLS, no-tenant-content, dependency direction) as a non-skippable test.                                                    |
| [add-integration-test](add-integration-test/SKILL.md)   | Testcontainers integration test against real Postgres. **No tenant-isolation pair** (Hub has no RLS) — this is the key difference from LearnStack's version. |
| [run-tests-locally](run-tests-locally/SKILL.md)         | Running the Hub suites locally (incl. the `~/.dotnet/dotnet` + `LearnStack.Hub.slnx` specifics) and interpreting failures.                                   |

### Operational

| Skill                                       | When to use                                                                                           |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| [local-dev-setup](local-dev-setup/SKILL.md) | Bringing up the Hub dev stack (shared LearnStack backends + Hub Dapr/APISIX) in the right boot order. |

## Skills deferred to their owning packet

These Hub workflows aren't needed yet; create the skill when the packet that needs it starts (mirroring the relevant LearnStack skill, adjusted for the Hub deltas):

| Skill                                        | Owning packet                                                                        | Mirror of                                         |
| -------------------------------------------- | ------------------------------------------------------------------------------------ | ------------------------------------------------- |
| `add-provider-adapter`                       | P02c-2 (`LearnStackApiClient`) / P02c-5 (Let's Encrypt) / Phase 09b (Stripe, Iyzico) | LearnStack `add-provider-adapter`                 |
| `add-hub-permission`                         | P02c-4 (Operators module)                                                            | LearnStack `add-permission` (operator-scope only) |
| `add-audit-coverage`                         | P02c-4 (Audit module)                                                                | LearnStack `add-audit-coverage`                   |
| `add-operator-portal-route` / `add-i18n-key` | P02c-4 (operator portal)                                                             | LearnStack `add-frontend-route` / `add-i18n-key`  |

## Authoring a new skill

Each `SKILL.md` carries YAML frontmatter:

```yaml
---
name: <skill-name> # kebab-case; matches directory name
description: >
  <one-line summary>. USE FOR: <triggers>. DO NOT USE FOR: <anti-triggers>.
---
```

Body structure: **Purpose**, **When to use** / **When not to use**, **Inputs**, **Workflow** (numbered, with checkpoints), **Validation**, **Common pitfalls**. Keep under ~200 lines. **Link to the canonical LearnStack doc** (ADR, standard, architecture) instead of restating it; carry only the Hub delta.

## What skills are not

- **Not duplicates of standards.** A skill is a _workflow_. LearnStack's [Standards corpus](../../../LearnStack/docs/standards/) is the authority; skills cite it.
- **Not decisions.** Decisions live in ADRs (LearnStack `../LearnStack/docs/decisions/` for cross-cutting; this repo's `docs/decisions/` for `HUB-NNNN`).
- **Not scratch space.** Exploratory notes go in `docs/analysis/` (gitignored).
