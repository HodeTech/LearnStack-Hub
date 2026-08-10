# Hub cross-cutting foundation

This document specs the Hub-side cross-cutting foundation that lands in **P02c-1**. It mirrors LearnStack core's Phase 02a Packet 3 ([ADR-0032](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0032-exception-handling-logging-and-observability.md)) — same shapes, Hub-adjusted where Hub's operator-scoped (not tenant-scoped) model demands it.

The intent: when domain code starts landing in Hub modules, it programs against the _same_ `Result<T>` / MediatR-pipeline / exception-handling / observability surface a LearnStack core developer already knows. Patterns are copied, not invented.

> **Why a local copy, not a shared package.** Hub's `LearnStack.Hub.SharedKernel` reproduces LearnStack core's `LearnStack.SharedKernel` patterns but does **not** import LearnStack assemblies — the two repos release independently (see [CLAUDE.md § Hard rules](../../CLAUDE.md) + the "Hub SharedKernel is a local copy" entry in [CONTRIBUTING.md](../../CONTRIBUTING.md)). A shared `LearnStack.Foundation` NuGet is a Phase 11 re-evaluation.

## 1. SharedKernel surface (mirror of LearnStack P02a-2)

`LearnStack.Hub.SharedKernel` ships the following, folder-for-folder matching LearnStack core (only namespace prefix differs: `LearnStack.Hub.SharedKernel.*`):

| Folder           | Types                                                                                                                                 | Notes                                                                                                                                                       |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Results/`       | `IResultBase`, `Result<T>`, `Result`, `Error`, `Unit`                                                                                 | `Result<T>.Ok` throws on null value; `Result.FailFor<TResponse>` reflection factory; `Error.Code` = `Message.Key[LocalizedMessage.RequiredPrefix.Length..]` |
| `Localization/`  | `LocalizedMessage`                                                                                                                    | `RequiredPrefix = "lockey_"`; ctor throws if key lacks prefix                                                                                               |
| `Domain/`        | `Entity<TId>`, `AuditableEntity<TId>`, `IDomainEvent`, `DomainEvent`, `IHasDomainEvents`                                              | `AuditableEntity` audit columns keyed on `OperatorId` (see § OperatorId)                                                                                    |
| `Identifiers/`   | `IStronglyTypedId<TKey>`, `IHasId<TId>`, `IAggregateRoot<TId>`, `OperatorId`, `IGuidFactory`, `SystemGuidFactory`, `FixedGuidFactory` | `OperatorId` is Hub's cross-cutting actor id (NOT `UserId`)                                                                                                 |
| `Time/`          | `IClock`, `SystemClock`, `FixedClock`                                                                                                 |                                                                                                                                                             |
| `Random/`        | `IRandom`, `SystemRandom`, `FixedRandom`                                                                                              | Optional in P02c-1; include for parity                                                                                                                      |
| `Pagination/`    | `CursorPagination`, `Page<T>`, `PageInfo`                                                                                             | `DefaultLimit = 20`, `MaxLimit = 100`                                                                                                                       |
| `Persistence/`   | `ISoftDelete`, `IOptimisticConcurrency`                                                                                               |                                                                                                                                                             |
| `Errors/`        | `HubException` (base), `DomainException`, `InfrastructureException`, `ProviderException`                                              | `HubException` is the Hub analogue of `LearnStackException`; same shape (carries an `Error`)                                                                |
| `Secrets/`       | `ISecretProvider`, `ConfigurationSecretProvider`, `SensitiveTokenCatalog`                                                             |                                                                                                                                                             |
| `Observability/` | `IErrorTrackingProvider`, `CapturedContext`                                                                                           | `CapturedContext` carries `OperatorId?` instead of (or in addition to) `UserId?`                                                                            |
| `Resilience/`    | `IProviderResilience<TPort>`, `ResilienceOptions` (+ Retry/CircuitBreaker/Timeout/Bulkhead)                                           | Polly v8; no provider adapters consume it until P02c-2 / Phase 09b, but the surface ships now                                                               |
| `Hosting/`       | `DeploymentMode`                                                                                                                      | Same five values: Development, SaaS, Dedicated, SelfHostedOnline, SelfHostedAirGapped                                                                       |
| (root)           | `LearnStackHubVogenDefaults.IdMask`                                                                                                   | `EfCoreValueConverter \| SystemTextJson \| TypeConverter`                                                                                                   |

The exact public API shapes are LearnStack core's — reproduce them verbatim (adjusting the namespace + the `OperatorId` substitution). The agent prompt points the implementer at the live LearnStack source under `../LearnStack/backend/src/LearnStack.SharedKernel/` for the canonical signatures.

### `HubException` naming

LearnStack core's base exception is `LearnStackException` (namespace `LearnStack.SharedKernel.Errors`). Hub's base is **`HubException`** (namespace `LearnStack.Hub.SharedKernel.Errors`). The Roslyn `DomainException` analyzer (if mirrored — see § 5) keys on the Hub namespace, not LearnStack's.

### OperatorId, not UserId

This is the load-bearing Hub adjustment. LearnStack core's `AuditableEntity<TId>` audit columns (`CreatedBy`, `UpdatedBy`, `DeletedBy`) are typed `UserId` — a _tenant_ user. Hub's actors are **operators** (LearnStack staff in the `learnstack-hub` Keycloak realm), never tenant users. So:

- `LearnStack.Hub.SharedKernel.Identifiers.OperatorId` is the Vogen `[ValueObject<Guid>]` that replaces `UserId`.
- `AuditableEntity<TId>` audit columns are typed `OperatorId` / `OperatorId?`.
- `CapturedContext` (error tracking) carries `OperatorId?`.
- There is **no** `UserId` type in Hub's SharedKernel. (A tenant `UserId` appearing anywhere in Hub would be a `Hub_NeverStores_TenantData`-adjacent smell — Hub references tenants by `TenantId`, never tenant users by id.)

## 2. MediatR pipeline — Hub's behavior set

LearnStack core registers **seven** pipeline behaviors: Validation → Logging → AuditLog → TenantContext → Authorization → Transaction → OutboxFlush, then the Handler. [ADR-0032 § Sub-decision 2](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0032-exception-handling-logging-and-observability.md) writes that as an eight-step list because it counts the Handler; this document counts behaviors, so the numbers below are behavior counts throughout.

Hub registers **six**. The one behavior that drops out is a tenant-isolation concern Hub does not have:

| #   | Behavior                | P02c-1 state                    | Why                                                                                                                                                                                                                               |
| --- | ----------------------- | ------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | `ValidationBehavior`    | **Live**                        | FluentValidation → `Error.Details`, never throws. Same as LearnStack.                                                                                                                                                             |
| 2   | `LoggingBehavior`       | **Live**                        | Scope + `ActivitySource("learnstack.hub.mediatr")` + latency. Correlation fields carry `operator.id`, not `tenant.id`.                                                                                                            |
| 3   | `AuditLogBehavior`      | **Shell**                       | try/catch + `ExceptionDispatchInfo` rethrow contract wired; the operator-audit write lands in P02c-4 (Audit module).                                                                                                              |
| 4   | `AuthorizationBehavior` | **Shell**                       | Operator permission check lands in P02c-4 (Operators module). Shell returns `next()`.                                                                                                                                             |
| 5   | `TransactionBehavior`   | **Live (per-module DbContext)** | Opens UoW on the owning module's DbContext, commits on success-`Result`, rolls back on fail-`Result` or exception. P02c-1 has real DbContexts so this can be live (unlike LearnStack's Packet 3 shell which waited for Packet 6). |
| 6   | `OutboxFlushBehavior`   | **Shell**                       | `IOutbox` flush lands in P02c-2 with the Dapr publish of `learnstack.hub.entitlement`.                                                                                                                                            |
| 7   | Handler                 | innermost                       |                                                                                                                                                                                                                                   |

**Dropped vs LearnStack core:**

- **`TenantContextBehavior`** — REMOVED. Hub has no per-request tenant context to assert / no RLS GUC to set. Hub requests are operator-scoped; the operator identity rides on the JWT, resolved by an `OperatorContext` (P02c-4), not by a tenant resolver.

The `MediatR_Pipeline_Order_Matches_Canonical_Sequence` architecture test (if mirrored) asserts the six-behavior Hub order, not LearnStack's seven.

## 3. Exception handling

- **L1 handler:** `HubExceptionHandler : IExceptionHandler` in `LearnStack.Hub.Api/Common/` — mirror of `LearnStackExceptionHandler`. Captures via `IErrorTrackingProvider`, reads `CapturedContext` (operator + correlation), maps to RFC 7807 Problem Details, logs through `ILogger`.
- **`Result<T>.ToActionResult()`** in `LearnStack.Hub.Api/Common/ResultExtensions.cs` — explicit at every controller endpoint (no action filter). Success → `OkObjectResult`; failure → `ProblemDetailsActionResult(error)`.
- **`ProblemDetailsFactory` + `HttpStatusMap`** — mirror of LearnStack core's. Problem-type prefix `https://errors.hub.learnstack.dev/` (Hub's own error domain).
- **Exception hierarchy:** `HubException` (base) → `DomainException`, `InfrastructureException`, `ProviderException`. `DomainException` is reserved for programmer errors / aggregate-invariant bugs; expected business-rule violations return `Result.Fail(...)` (mirror of [ADR-0032 § Sub-decision 4](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0032-exception-handling-logging-and-observability.md)).
- **No `ExceptionHandlingBehavior` in the pipeline** — `AuditLogBehavior` (catch + rethrow) plus the L1 `HubExceptionHandler` cover every exception path, same as LearnStack core.

## 4. Observability

- **Serilog primary logger + OTLP sink** — `builder.Host.UseSerilog(...)` with `WriteTo.Console` + `WriteTo.OpenTelemetry`. The OTel `LoggerProvider` is **not** registered alongside (double-export rule, mirror of LearnStack).
- **OpenTelemetry SDK** — `AddAspNetCoreInstrumentation` + `AddHttpClientInstrumentation` + `AddEntityFrameworkCoreInstrumentation` + OTLP exporter. Manual `ActivitySource` per module (`learnstack.hub.<module>`).
- **No `TenantContextSpanProcessor`.** Hub has no tenant context to enrich spans with. If a span-enricher is wanted, it enriches with `operator.id` + `correlation.id` (an `OperatorContextSpanProcessor`) — but that depends on the Operators module (P02c-4). P02c-1 ships the OTel pipeline without the operator-enricher; add it in P02c-4.
- **`IErrorTrackingProvider`** — three impls (`NoOpErrorTracker`, `SentryErrorTracker`, `LocalFileErrorTracker`), composition-root branched by `DeploymentMode`. DSN from `ISecretProvider`. Module code never references `Sentry.SentrySdk`.

## 5. Roslyn analyzer (optional in P02c-1)

LearnStack core ships `LearnStack.Analyzers` (`backend/analyzers/`) carrying `DomainExceptionThrowAnalyzer` (diagnostic `LearnStackException-DomainExceptionThrow`). Hub can either:

- **(a)** mirror it as `LearnStack.Hub.Analyzers` keyed on `LearnStack.Hub.SharedKernel.Errors.DomainException` (diagnostic `LearnStackHubException-DomainExceptionThrow`), or
- **(b)** defer the analyzer to a later packet and rely on code review for the `DomainException`-vs-`Result.Fail` rule in P02c-1.

Recommended: **(b) defer** unless the agent has spare budget. The analyzer is netstandard2.0 Roslyn machinery; copying it is mechanical but not load-bearing for P02c-1's four aggregates. If deferred, note it in the Hub roadmap as a P02c-2 follow-up.

## 6. Composition root (`LearnStack.Hub.Api/Program.cs`)

P02c-1 grows `Program.cs` from the P02c-0 minimal `/healthz` host into the foundation host:

1. `builder.Host.UseSerilog(...)` (console + OTLP).
2. `builder.Services.AddOpenTelemetry()...` (tracing + metrics; no LoggerProvider).
3. `builder.Services.AddExceptionHandler<HubExceptionHandler>()` + `AddProblemDetails()`.
4. MediatR registration with the six-behavior pipeline in canonical order.
5. `IClock` / `IGuidFactory` / `IRandom` / `ISecretProvider` / `IErrorTrackingProvider` / `IProviderResilience<>` registered, `DeploymentMode`-branched at the composition root (modules never read `DeploymentMode`).
6. Each module's `Add<Module>Module(...)` extension (DbContext + handlers + validators).
7. `/healthz` stays; real endpoints arrive in P02c-2.

`DeploymentMode` is read **once** here. The architecture test `Modules_Do_Not_Reference_DeploymentMode` (if mirrored) keeps modules from reading it.
