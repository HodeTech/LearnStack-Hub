---
name: wire-cross-cutting-foundation
description: >
  Wire the LearnStack Hub cross-cutting foundation in the Hub host (LearnStack.Hub.Api)
  — HubExceptionHandler, the 6-step MediatR pipeline, Result<T>.ToActionResult(),
  Serilog + OTel, IErrorTrackingProvider, IProviderResilience<TPort>, and the Hub
  SharedKernel surface (with OperatorId, HubException). USE FOR: standing up the
  foundation in P02c-1 (one-time wiring), or restoring it after a composition-root
  refactor. DO NOT USE FOR: adding a provider adapter (add-provider-adapter, later),
  a single MediatR handler (add-mediatr-handler), or a new module (add-hub-module).
---

# Wiring the Hub cross-cutting foundation

## Purpose

Stand up the Hub's foundation so domain code programs against the same `Result<T>` / pipeline / exception / observability surface a LearnStack developer knows — adjusted for the Hub deltas. This is the P02c-1 keystone. The full design spec is [docs/architecture/cross-cutting-foundation.md](../../../docs/architecture/cross-cutting-foundation.md); this skill is the execution workflow.

## When to use

- P02c-1: the one-time foundation wiring after the SharedKernel mirror lands.
- Restoring the foundation after a composition-root refactor.

## When not to use

- A provider adapter → `add-provider-adapter` (P02c-2+).
- A single handler → [add-mediatr-handler](../add-mediatr-handler/SKILL.md).
- A new module → [add-hub-module](../add-hub-module/SKILL.md).

## Workflow

### Step 1 — Mirror the Hub SharedKernel

Reproduce `../LearnStack/backend/src/LearnStack.SharedKernel/` into `backend/src/Core/LearnStack.Hub.SharedKernel/`, folder-for-folder, with namespace `LearnStack.Hub.SharedKernel.*` and **two substitutions**:

- **`OperatorId`** replaces `UserId` everywhere (the Vogen ID in `Identifiers/`, the `AuditableEntity<TId>` audit columns, `CapturedContext`). There is no tenant `UserId` in Hub.
- **`HubException`** replaces `LearnStackException` as the base exception (namespace `LearnStack.Hub.SharedKernel.Errors`); `DomainException` / `InfrastructureException` / `ProviderException` derive from it.

Surface to reproduce (see [docs/architecture/cross-cutting-foundation.md § 1](../../../docs/architecture/cross-cutting-foundation.md)): `Results/`, `Localization/`, `Domain/`, `Identifiers/`, `Time/`, `Random/`, `Pagination/`, `Persistence/`, `Errors/`, `Secrets/`, `Observability/`, `Resilience/`, `Hosting/` (`DeploymentMode`), `FeatureFlags/` (`FeatureKey`/`LimitKey` — see [add-feature-key](../add-feature-key/SKILL.md)), and `LearnStackHubVogenDefaults.IdMask`. Open the live LearnStack source for the exact public API shapes — reproduce them verbatim.

`.csproj`: `Vogen` (PrivateAssets="all"), `MediatR`, `Microsoft.EntityFrameworkCore`, `Polly`, `Microsoft.Extensions.Configuration.Abstractions` — match LearnStack's SharedKernel.csproj. Add unit tests mirroring LearnStack's (Result, LocalizedMessage prefix invariant, Entity equality, FixedClock, generation-style invariants).

### Step 2 — Exception handling (`LearnStack.Hub.Api/Common/`)

- `HubExceptionHandler : IExceptionHandler` — mirror `LearnStackExceptionHandler`; reads `CapturedContext` (operator + correlation), captures via `IErrorTrackingProvider`, maps to RFC 7807, logs.
- `ResultExtensions.ToActionResult<T>()` — success → `OkObjectResult`; failure → `ProblemDetailsActionResult(error)`. Explicit at every endpoint; no action filter.
- `ProblemDetailsFactory` + `HttpStatusMap` — problem-type prefix `https://errors.hub.learnstack.dev/`.

### Step 3 — The 6-step MediatR pipeline (`LearnStack.Hub.Application/Pipeline/`)

Register in this exact order (the `MediatR_Pipeline_Order_Matches_Canonical_Sequence` test, if mirrored, asserts it):

1. `ValidationBehavior` — **live**; FluentValidation → `Error.Details`, never throws.
2. `LoggingBehavior` — **live**; scope + `ActivitySource("learnstack.hub.mediatr")` + latency; correlation fields carry `operator.id`.
3. `AuditLogBehavior` — **shell**; try/catch + `ExceptionDispatchInfo` rethrow contract; the operator-audit write lands in P02c-4.
4. `AuthorizationBehavior` — **shell**; operator-permission check lands in P02c-4; returns `next()`.
5. `TransactionBehavior` — **live**; opens UoW on the owning module's DbContext, commits on success-`Result`, rolls back on fail-`Result`/exception. (Live in Hub from P02c-1 because real DbContexts exist.)
6. `OutboxFlushBehavior` — **shell**; `IOutbox` flush lands in P02c-2.

**Do NOT add `TenantContextBehavior`.** Hub has no per-request tenant context / no RLS GUC. This is the load-bearing difference from LearnStack's 8-step pipeline.

### Step 4 — Observability

- Serilog primary logger: `builder.Host.UseSerilog(...)` with `WriteTo.Console` + `WriteTo.OpenTelemetry`. **Do not** also register the OTel `LoggerProvider` (double-export).
- OTel SDK: `AddAspNetCoreInstrumentation` + `AddHttpClientInstrumentation` + `AddEntityFrameworkCoreInstrumentation` + OTLP exporter. No `TenantContextSpanProcessor` (Hub has no tenant context; an `OperatorContextSpanProcessor` is a P02c-4 add).
- `IErrorTrackingProvider`: `NoOpErrorTracker` / `SentryErrorTracker` / `LocalFileErrorTracker`, composition-root branched by `DeploymentMode`; DSN from `ISecretProvider`; modules never reference `Sentry.SentrySdk`.

### Step 5 — Resilience

`IProviderResilience<TPort>` (Polly v8 `ResiliencePipeline`) + `ResilienceOptions` (config shape `appsettings.Resilience:<port-name>:`). No provider adapters consume it until P02c-2 / Phase 09b, but the surface ships now.

### Step 6 — Composition root (`Program.cs`)

Grow `Program.cs` from the minimal `/healthz` host: `UseSerilog` → `AddOpenTelemetry` → `AddExceptionHandler<HubExceptionHandler>()` + `AddProblemDetails()` → MediatR with the 6-step pipeline → register `IClock`/`IGuidFactory`/`IRandom`/`ISecretProvider`/`IErrorTrackingProvider`/`IProviderResilience<>` (DeploymentMode-branched, read **once** here) → each module's `Add<Module>Module(...)`. Keep `/healthz`.

### Step 7 — (Optional) Roslyn analyzer

Mirroring LearnStack's `DomainExceptionThrowAnalyzer` as `LearnStack.Hub.Analyzers` is **optional in P02c-1** — recommended to defer to P02c-2 and rely on code review for the `DomainException`-vs-`Result.Fail` rule. If deferred, note it in `docs/roadmap/README.md`.

## Validation

- `~/.dotnet/dotnet build LearnStack.Hub.slnx` 0/0.
- SharedKernel unit tests green.
- Pipeline registers exactly 6 behaviors in canonical order; no `TenantContextBehavior`.
- `OperatorId` is the only actor id; no `UserId` type exists.
- `HubExceptionHandler` registered; `ToActionResult` used at endpoints; Serilog single log path (no double OTel LoggerProvider).
- `DeploymentMode` read only at the composition root.

## Common pitfalls

- **Copying LearnStack's 8-step pipeline verbatim.** Drop `TenantContextBehavior`; Hub is 6 steps.
- **Leaving `UserId` in the mirror.** Substitute `OperatorId` everywhere.
- **Registering the OTel LoggerProvider alongside Serilog.** Double-exports every log line.
- **Reading `DeploymentMode` inside a module.** Branch once at the composition root.
- **Improvising shapes.** Open `../LearnStack/backend/src/LearnStack.SharedKernel/` + `LearnStack.Api/Common/` + `LearnStack.Application/Pipeline/` and reproduce.
