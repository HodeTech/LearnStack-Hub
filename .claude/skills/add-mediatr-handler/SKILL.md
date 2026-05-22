---
name: add-mediatr-handler
description: >
  Add a MediatR command or query handler to a Hub module that participates in the
  6-step Hub pipeline — Result<T> return type, FluentValidation, the
  Application.Contracts request shape. USE FOR: any new Hub write or read use case in
  a module's Application layer. DO NOT USE FOR: pure domain logic (lives in Domain),
  provider-adapter glue (lives in Infrastructure), or controllers (thin shells over
  MediatR — push logic into a handler).
---

# Adding a Hub MediatR handler

## Purpose

Add a command/query handler that plugs into the Hub 6-step pipeline correctly: returns `Result<T>`, validated by FluentValidation, request shape in `Application.Contracts`, cross-module reads via contracts. Mirrors LearnStack's `add-mediatr-handler`, adjusted for the Hub pipeline (6 steps, no tenant context).

## When to use

- A new Hub use case (write command or read query) in a module's `Application`.

## When not to use

- Pure domain logic → `Domain` aggregate method.
- Provider-adapter glue → `Infrastructure`.
- Controller logic → push it into a handler; controllers are thin shells over `Result<T>.ToActionResult()`.

## Workflow

### Step 1 — Request + DTO (Application.Contracts)

```csharp
public sealed record <Verb><Noun>Command(...) : IRequest<Result<<Dto>>>;   // command
public sealed record Get<Noun>Query(...)     : IRequest<Result<<Dto>>>;    // query
public sealed record <Dto>(...);                                            // response DTO
```

The request lives in `Application.Contracts` so other modules can call it without referencing `Application`. Every handler returns `Result<T>` (or `Result<Unit>`).

### Step 2 — Validator (Application)

```csharp
public sealed class <Verb><Noun>CommandValidator : AbstractValidator<<Verb><Noun>Command>
{
    public <Verb><Noun>CommandValidator() { /* rules; messages are lockey_ keys */ }
}
```

The `ValidationBehavior` (pipeline step 1) aggregates failures into `Error.Details` and returns `Result.Fail(validation_failed)` — it never throws. Cross-module existence checks (e.g. "plan exists") call the other module's `Application.Contracts`, not its `Domain`.

### Step 3 — Handler (Application)

```csharp
public sealed class <Verb><Noun>CommandHandler(<Module>DbContext db, IClock clock, IGuidFactory guids /* + cross-module contracts */)
    : IRequestHandler<<Verb><Noun>Command, Result<<Dto>>>
{
    public async Task<Result<<Dto>>> Handle(<Verb><Noun>Command request, CancellationToken ct)
    {
        // 1. load / validate domain state
        // 2. mutate the aggregate (its methods return Result; short-circuit on failure)
        // 3. persist via the module DbContext (TransactionBehavior commits/rolls back)
        // 4. if the change affects the entitlement projection, call the Entitlements
        //    Application.Contracts recompute (see entitlement-projection.md)
        // 5. return Result.Ok(dto)
    }
}
```

Rules: return `Result.Fail(...)` for business-rule violations (never throw `DomainException`). Use `IClock` / `IGuidFactory` (not `DateTimeOffset.UtcNow` / `Guid.NewGuid()`) for testability. The handler does NOT open its own transaction (the `TransactionBehavior` owns the UoW). Domain events raised by the aggregate dispatch in-process via MediatR.

### Step 4 — Endpoint (Api, when the contract surface needs it)

Controllers/minimal-API endpoints are thin: deserialize → `mediator.Send(request)` → `result.ToActionResult()`. (Most P02c-1 handlers have no HTTP surface yet — the endpoints land in P02c-2. Add the endpoint only when the packet needs it.)

### Step 5 — Tests

- Unit: handler with a `FixedClock` + `FixedGuidFactory` + in-memory or substituted contracts; assert `Result.Ok`/`Result.Fail` + side effects.
- Integration (for cross-module flows): [add-integration-test](../add-integration-test/SKILL.md).

## Validation

- Request in `Application.Contracts`; handler + validator in `Application`.
- Returns `Result<T>`; business-rule failures are `Result.Fail`, not exceptions.
- Uses `IClock`/`IGuidFactory`; no direct `DateTimeOffset.UtcNow`/`Guid.NewGuid()`.
- Cross-module reads via `Application.Contracts`, not another module's `Domain`/DbContext.
- No self-managed transaction (pipeline owns it).

## Common pitfalls

- **Throwing for a business rule.** Return `Result.Fail`.
- **Opening a transaction in the handler.** `TransactionBehavior` already does.
- **Reaching into another module's Domain.** Use its `Application.Contracts`.
- **Direct clock/guid calls.** Inject `IClock`/`IGuidFactory` so tests are deterministic.
- **Forgetting the entitlement recompute.** A subscription/plan change that doesn't trigger recompute leaves a stale projection.
