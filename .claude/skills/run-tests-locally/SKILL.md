---
name: run-tests-locally
description: >
  Run the LearnStack Hub test suites locally — unit, architecture, contract,
  integration (Testcontainers) — plus build + format-verify + leakwatch, and
  interpret common failures. USE FOR: before pushing a commit, debugging a CI failure
  on a fresh checkout, isolating a single failing test. DO NOT USE FOR: writing new
  tests (use add-integration-test / add-architecture-test) or running production
  migrations.
---

# Running the Hub tests locally

## Purpose

Run the right suites for a change and read the failures correctly — including the Hub-specific gotchas (the `~/.dotnet/dotnet` PATH, the `LearnStack.Hub.slnx` name, the integration filter, the Testcontainers Docker dependency).

## When to use

- Before committing / pushing.
- Reproducing a CI failure locally.
- Isolating one failing test.

## The commands

Always fix the PATH first — the system `dotnet` is .NET 9:

```bash
export PATH="$HOME/.dotnet:$PATH"   # .NET 10.0.x; required by global.json pin
cd backend
```

### Build (TreatWarningsAsErrors under CI flag)

```bash
dotnet build LearnStack.Hub.slnx --nologo            # local: warnings allowed
CI=true dotnet build LearnStack.Hub.slnx --nologo    # reproduce CI's strict build
```

### Unit + architecture + contract (fast; no Docker)

```bash
dotnet test LearnStack.Hub.slnx \
  --filter "FullyQualifiedName!~LearnStack.Hub.Tests.Integration" --nologo
```

The filter excludes the Testcontainers suite so the fast suites run without Docker.

### Integration (Testcontainers; needs Docker + shared Postgres up)

```bash
dotnet test tests/LearnStack.Hub.Tests.Integration/LearnStack.Hub.Tests.Integration.csproj --nologo
```

Requires a Docker daemon (OrbStack/Docker Desktop). The tests spin their own Postgres container — they do **not** need `make dev`'s shared Postgres, but the migrations must apply against the container. (Pre-P02c-2 there is no Kafka/Dapr dependency.)

### Format verify (CI gate)

```bash
dotnet format LearnStack.Hub.slnx --verify-no-changes --no-restore   # exit 0 = clean
```

### Secret scan (CI gate)

```bash
cd .. && leakwatch scan fs . --config .leakwatch.yaml --format table --min-severity medium --no-verify
```

### Frontend (if the change touches the operator portal)

```bash
cd frontend && pnpm install && pnpm -r typecheck && pnpm -r lint && pnpm -r build && pnpm -r test
```

### Compose configs parse

```bash
docker compose -f infra/compose/dev.yml config > /dev/null && echo OK
docker compose -f infra/compose/dev.yml -f infra/compose/e2e.yml config > /dev/null && echo OK
```

## Which suites for which change

| Change                       | Run                                                           |
| ---------------------------- | ------------------------------------------------------------- |
| SharedKernel / cross-cutting | build + unit + architecture + format                          |
| New aggregate / module       | build + unit + architecture + integration (the flow) + format |
| Migration                    | build + integration (applies cleanly) + format                |
| Docs only                    | sibling-link audit + leakwatch                                |
| Operator portal              | frontend pipeline                                             |

## Interpreting common failures

- **`A compatible .NET SDK was not found … 10.0.100`** → the PATH trap. `export PATH="$HOME/.dotnet:$PATH"`.
- **Architecture test red** → a structural rule broke (a forbidden dependency, an RLS leak, a `UserId` where `OperatorId` belongs, a tenant-content type name). Fix the code, not the test — architecture tests are non-skippable.
- **`Hub_NeverStores_TenantData` red** → a Hub type name contains a forbidden fragment (`Course`/`Lesson`/…). Rename or remove; Hub holds metadata only.
- **Integration test red on a fresh checkout** → Docker not running, or migrations don't apply. Check the daemon; run `dotnet ef migrations script` to inspect.
- **`dotnet format --verify` red** → run `dotnet format` (no `--verify`), re-stage. The pre-commit hook does this automatically.
- **Build red under `CI=true` only** → a warning that's an error in CI. Fix the warning.

## Validation

- The suites matching the change are green; the architecture suite ran (not accidentally filtered out).
- `format --verify` exit 0; leakwatch 0 findings.

## Common pitfalls

- **Forgetting the PATH.** The #1 local failure. Set it once per shell.
- **Marking a test `[Skip]` to go green.** Architecture tests are non-skippable; a failure is the code being wrong.
- **Running integration tests without Docker.** They need a daemon for Testcontainers.
