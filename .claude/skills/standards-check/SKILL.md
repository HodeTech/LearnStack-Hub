---
name: standards-check
description: >
  Validate a Hub change against the Hub deltas + the LearnStack standards corpus +
  the Hub CLAUDE.md hard rules — the mechanical checklist a careful reviewer runs
  before merge. USE FOR: a pre-commit conformance pass on your own diff, a gap
  analysis between a draft and the corpus, or walking a PR's structural compliance
  before the deeper code-review. DO NOT USE FOR: code-quality / bug / security review
  (use code-review — broader), running tests (use run-tests-locally), or introducing
  a new rule (use write-adr first; this skill enforces existing rules).
---

# Hub standards-conformance check

## Purpose

The fast, mechanical gate: does this diff obey the Hub deltas, the LearnStack standards Hub inherits, and the Hub hard rules? Run before [code-review](../code-review/SKILL.md) (which is broader + slower).

## When to use

- Pre-commit conformance pass on your own diff (Step 5 of [implement-task](../implement-task/SKILL.md)).
- Gap analysis between a draft and the corpus.
- The structural first pass on a PR.

## When not to use

- Bug / security / optimisation review → [code-review](../code-review/SKILL.md).
- Running tests → [run-tests-locally](../run-tests-locally/SKILL.md).
- Adding a new rule → [write-adr](../write-adr/SKILL.md) first.

## The checklist

Walk these against the diff. Each item is pass/fail; a fail blocks merge until fixed.

### Hub deltas (the things LearnStack-trained instincts get wrong)

- [ ] **No RLS.** No `ENABLE ROW LEVEL SECURITY`, no `*_isolation` policy, no `[TenantOwned]`/`[OrganizationScoped]`, no EF global query filter keyed on tenant context, no `app.tenant_id` GUC, no `DbConnectionInterceptor` for session vars.
- [ ] **`OperatorId`, not `UserId`.** Audit columns (`created_by`/`updated_by`/`deleted_by`), `CapturedContext`, any actor reference use `OperatorId`. No tenant `UserId` type anywhere.
- [ ] **6-step MediatR pipeline.** Validation → Logging → AuditLog → Authorization → Transaction → OutboxFlush → Handler. No `TenantContextBehavior`.
- [ ] **`hub` schema.** Every DbContext `HasDefaultSchema("hub")`; tables at `learnstack_hub.hub.<table>`.
- [ ] **No tenant content.** No `Course`/`Lesson`/`Enrollment`/`LiveSession`/`LessonItem`/`MediaAsset`/tenant-`User` types or tables (`Hub_NeverStores_TenantData`).

### Module + dependency structure ([01-architecture-standards.md](../../../../LearnStack/docs/standards/01-architecture-standards.md))

- [ ] Module `Domain` references only `LearnStack.Hub.SharedKernel` (+ analyzer); no cross-module `Domain` refs; no `Application`/`Infrastructure` refs from `Domain`.
- [ ] No imports of `LearnStack.SharedKernel` / `LearnStack.Domain` / `LearnStack.Infrastructure` / `LearnStack.Modules.*` from the sibling repo.
- [ ] Cross-module reads go through `Application.Contracts`, not another module's `Domain` / DbContext.
- [ ] One DbContext per module; cross-module FKs are plain `uuid` columns + index, not EF navigations.

### Backend coding ([02-backend-coding.md](../../../../LearnStack/docs/standards/02-backend-coding.md))

- [ ] Strongly-typed IDs via Vogen (`[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]`); no raw `Guid` on entity surfaces.
- [ ] Handlers return `Result<T>`; business-rule violations → `Result.Fail(...)`, never `throw DomainException`.
- [ ] FluentValidation validators for every command; file-scoped namespaces; records where idiomatic.

### Database ([05-database.md](../../../../LearnStack/docs/standards/05-database.md), Hub-adjusted)

- [ ] `snake_case` plural tables, `snake_case` columns, `id` PK, `<entity>_id` FKs, `ix_`/`ux_` indexes.
- [ ] Migrations forward-only after merge; destructive changes follow the two-step deprecation.
- [ ] (Ignore the RLS / org-isolation rows of Standards 05 — they don't apply to Hub.)

### Contract surface + boundary ([CLAUDE.md hard rules](../../../CLAUDE.md))

- [ ] No fifth Hub HTTPS endpoint without an ADR in `../LearnStack/docs/decisions/`.
- [ ] `/api/internal/*` not internet-exposed (when those endpoints land).
- [ ] No Kubernetes-credential / K8s-state writes to LearnStack's cluster.
- [ ] `learnstack-hub` realm boundary respected.
- [ ] No `Sentry.SentrySdk` in module assemblies (use `IErrorTrackingProvider`).
- [ ] No Stripe/Iyzico SDK types outside the dedicated adapter projects (Phase 09b).

### Docs + corpus hygiene

- [ ] Adjacent docs updated (module deep dive, glossary, roadmap status, ADR cross-link).
- [ ] Sibling-relative links (`(\.\./)+learnstack/...`) resolve locally.
- [ ] English docs; Mermaid diagrams readable as text.

## Output

A pass/fail list. For each fail: the file:line, the rule it breaks, and the one-line fix. If everything passes, say so explicitly and hand off to [code-review](../code-review/SKILL.md) for the deeper pass.

## Common pitfalls

- **Treating Hub like LearnStack core.** Most fails here are RLS / `UserId` / `TenantContextBehavior` reflexes. The Hub deltas exist precisely to catch them.
- **Citing a rule without the file:line.** A finding the author can't locate is a weak finding.
