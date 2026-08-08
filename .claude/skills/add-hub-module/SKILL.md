---
name: add-hub-module
description: >
  Scaffold a new Hub module under backend/src/Modules/<Name>/ with the four-package
  layout (Domain, Application.Contracts, Application, Infrastructure), the module
  registration extension, its DbContext (hub schema, NO RLS), and its place in the
  solution + architecture-test list. USE FOR: introducing a planned Hub module
  (TenantLifecycle, Plans, Subscriptions, Entitlements in P02c-1; CustomDomains,
  Compliance, Usage, LicenseKeys, Audit, Operators, Invoicing later). DO NOT USE FOR:
  adding an aggregate inside an existing module (use add-hub-aggregate), or naming a
  module after a tenant domain (CEFR, asana — forbidden; Hub is domain-agnostic too).
---

# Scaffolding a Hub module

## Purpose

Stand up a new Hub modular-monolith module that complies with the dependency-direction rules from day one: four packages, the right references, a registration extension, a DbContext in the `hub` schema (no RLS), and architecture-test coverage. Mirrors LearnStack's [add-backend-module](../../../../LearnStack/.claude/skills/add-backend-module/SKILL.md) minus the tenant-isolation layer.

## When to use

- Introducing one of the planned Hub modules from [docs/architecture/module-topology.md](../../../docs/architecture/module-topology.md) / [docs/modules/README.md](../../../docs/modules/README.md).

## When not to use

- An aggregate inside an existing module → [add-hub-aggregate](../add-hub-aggregate/SKILL.md).
- A module named after a tenant domain → forbidden; Hub modules are generic control-plane concerns.

## Inputs

| Input                     | Required | Description                                                                       |
| ------------------------- | -------- | --------------------------------------------------------------------------------- |
| Module name               | Yes      | One of the planned modules in `docs/architecture/module-topology.md`. PascalCase. |
| Aggregate(s) it owns      | Yes      | e.g. TenantLifecycle owns `LearnStackTenant`.                                     |
| Cross-module dependencies | Yes      | Other modules' `Application.Contracts` it references.                             |

## Workflow

### Step 1 — Confirm the name + scope

The module must appear in [docs/architecture/module-topology.md](../../../docs/architecture/module-topology.md) (P02c-1 set) or [docs/modules/README.md](../../../docs/modules/README.md) (later packets). If not, stop — fit the work into an existing module or file a `HUB-NNNN` ADR to add a module.

### Step 2 — Create the four projects

```
backend/src/Modules/<Name>/
  LearnStack.Hub.Modules.<Name>.Domain/
  LearnStack.Hub.Modules.<Name>.Application.Contracts/
  LearnStack.Hub.Modules.<Name>.Application/
  LearnStack.Hub.Modules.<Name>.Infrastructure/
```

Dependency graph (architecture test enforces): `Domain → Hub.SharedKernel` only; `Application.Contracts → Hub.SharedKernel`; `Application → own Domain + own Application.Contracts + Hub.SharedKernel + MediatR + FluentValidation` (+ other modules' `Application.Contracts`); `Infrastructure → own Application + Hub.SharedKernel + EF Core`. Add each project to `backend/LearnStack.Hub.slnx`.

Forbidden (caught by tests): `Domain → Application/Infrastructure`; `Application → Infrastructure`; `Module A → Module B.Domain/Infrastructure`; any reference to a `LearnStack.*` (sibling-repo) assembly.

The `Domain` project needs a transitive `Microsoft.EntityFrameworkCore` reference for the Vogen-emitted EF converter (mirror how LearnStack's module Domain csproj handles it — `Directory.Build.props`-level or per-project).

### Step 3 — Module registration extension

In `Application/<Name>ModuleRegistration.cs`, a static `Add<Name>Module(this IServiceCollection, IConfiguration)` that registers the DbContext + MediatR handlers + validators. (If an `IHubModule` interface exists by the time you add this, implement it; otherwise the static-extension pattern is the baseline — promotable later.) Call it from `LearnStack.Hub.Api/Program.cs`.

### Step 4 — Module DbContext (`Infrastructure/Persistence/<Name>DbContext.cs`)

```csharp
public sealed class <Name>DbContext(DbContextOptions<<Name>DbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("hub");                 // Hub schema
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(<Name>DbContext).Assembly);
        // Register Vogen ID converters (RegisterVogenIds helper over the Domain assembly).
    }
    protected override void ConfigureConventions(ModelConfigurationBuilder b) =>
        b.HaveVogenIdConventions(); // or per-property HaveConversion<Id.EfCoreValueConverter>()
}
```

**No** tenant query filter, **no** RLS, **no** `ITenantContext` injection. Snake-case naming convention. One DbContext per module.

### Step 5 — Architecture-test fixture

Add the module's assemblies to `backend/tests/LearnStack.Hub.Tests.Architecture` so the dependency-direction test and `Hub_NeverStores_TenantData` scan it. See [add-architecture-test](../add-architecture-test/SKILL.md).

### Step 6 — Module doc + migration

- The module deep dive already exists for the P02c-1 set (`docs/modules/<name>.md`). For a later-packet module, write it first (it's the spec). Keep the inline audit matrix.
- Add the initial EF migration → [add-ef-migration](../add-ef-migration/SKILL.md).

## Validation

- `~/.dotnet/dotnet build LearnStack.Hub.slnx` builds all four projects.
- Architecture suite green: dependency direction correct; module scanned by `Hub_NeverStores_TenantData`.
- `dotnet ef migrations script` shows the expected `hub`-schema baseline.
- The module is registered in `Program.cs` and appears in `docs/architecture/module-topology.md`.

## Common pitfalls

- **Copying LearnStack's DbContext verbatim.** It injects `ITenantContext` + applies tenant filters. Hub's doesn't — strip them.
- **EF entities in `Application`.** They live in `Domain`; EF configs in `Infrastructure`.
- **Cross-module EF navigations.** Use `uuid` id columns + index; cross-module reads via `Application.Contracts`.
- **Forgetting the `Program.cs` registration.** Module builds but no handlers run.
- **A `LearnStack.*` (sibling) assembly reference.** Hub is self-contained; mirror patterns by copying source.
