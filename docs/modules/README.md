# Hub Module Deep Dives

Per-module documentation lands here as modules ship in P02c-1+ and Phase 09b. P02c-0 ships only this placeholder.

## Module topology

| Module            | Aggregates                                      | Phase     | Doc status                                           |
| ----------------- | ----------------------------------------------- | --------- | ---------------------------------------------------- |
| `TenantLifecycle` | `LearnStackTenant` (mirror)                     | P02c-1    | ✅ [tenant-lifecycle.md](tenant-lifecycle.md) (spec) |
| `Plans`           | `Plan`, `PlanTier`                              | P02c-1    | ✅ [plans.md](plans.md) (spec)                       |
| `Subscriptions`   | `HubSubscription`                               | P02c-1    | ✅ [subscriptions.md](subscriptions.md) (spec)       |
| `Entitlements`    | `Entitlement` (projection)                      | P02c-1    | ✅ [entitlements.md](entitlements.md) (spec)         |
| `CustomDomains`   | `CustomDomain`                                  | P02c-5    | ⏳                                                   |
| `Compliance`      | `CompliancePolicy`                              | P02c-5    | ⏳                                                   |
| `Usage`           | `UsageAggregate`                                | P02c-2    | ⏳                                                   |
| `LicenseKeys`     | `LicenseKey`                                    | P02c-6    | ⏳                                                   |
| `Invoicing`       | `HubInvoice`, `HubInvoiceLine`, `WebhookLedger` | Phase 09b | ⏳                                                   |
| `Audit`           | `AuditEntry` (operator audit)                   | P02c-4    | ⏳                                                   |
| `Operators`       | (Keycloak-backed; permission mapping)           | P02c-4    | ⏳                                                   |

> **"(spec)" status:** the P02c-1 module docs were authored as the design specification
> _before_ implementation. The P02c-1 agent implements against them. The cross-cutting
> design they depend on lives in [../architecture/module-topology.md](../architecture/module-topology.md),
> [../architecture/cross-cutting-foundation.md](../architecture/cross-cutting-foundation.md),
> and [../architecture/entitlement-projection.md](../architecture/entitlement-projection.md).

## Per-module doc shape

When a module ships, its deep-dive carries:

- **Aggregates + state machines** — entity diagram, allowed state transitions.
- **DbContext + EF configurations** — table layout, indexes, JSONB shapes.
- **MediatR contracts** — commands + queries the module exposes, with idempotency keys where applicable.
- **Integration events** — Dapr topic names (`learnstack.hub.<module>.*`), payload shape, consumer modules.
- **Permission keys** — operator-scope permissions the module declares.
- **Audit-coverage matrix** — MUST / SHOULD / MAY classification per operation (mirrors LearnStack [Standards 18 § Audit Coverage](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/18-audit-coverage.md)).
- **Architecture-test rules** — module-specific NetArchTest assertions.

## Audit-coverage matrices

Until the Hub operator-audit pipeline lands in **P02c-4** (`LearnStack.Hub.Modules.Audit`), each module doc carries its MUST/SHOULD/MAY audit table **inline** (see the "Audit coverage" section in each module's `.md`). When P02c-4 wires the live audit writer, these inline matrices either move to a per-module `audit.md` or are consumed in place — that split is a P02c-4 decision. The matrices follow the same shape as LearnStack core ([Standards 18 § Audit Coverage](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/18-audit-coverage.md)).
