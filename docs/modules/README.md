# Hub Module Deep Dives

One deep dive per module. The four [P02c-1](../roadmap/p02c-1-hub-domain-core.md) modules — `TenantLifecycle`, `Plans`, `Subscriptions`, `Entitlements` — are implemented on `main` under `backend/src/Modules/` and documented here. The remaining seven land with their owning packets.

## Module topology

| Module            | Aggregates                                      | Phase     | Doc status                                    |
| ----------------- | ----------------------------------------------- | --------- | --------------------------------------------- |
| `TenantLifecycle` | `LearnStackTenant` (mirror)                     | P02c-1    | ✅ [tenant-lifecycle.md](tenant-lifecycle.md) |
| `Plans`           | `Plan`                                          | P02c-1    | ✅ [plans.md](plans.md)                       |
| `Subscriptions`   | `HubSubscription`                               | P02c-1    | ✅ [subscriptions.md](subscriptions.md)       |
| `Entitlements`    | `Entitlement` (projection)                      | P02c-1    | ✅ [entitlements.md](entitlements.md)         |
| `CustomDomains`   | `CustomDomain`                                  | P02c-5    | ⏳                                            |
| `Compliance`      | `CompliancePolicy`                              | P02c-5    | ⏳                                            |
| `Usage`           | `UsageAggregate`                                | P02c-2    | ⏳                                            |
| `LicenseKeys`     | `LicenseKey`                                    | P02c-6    | ⏳                                            |
| `Invoicing`       | `HubInvoice`, `HubInvoiceLine`, `WebhookLedger` | Phase 09b | ⏳                                            |
| `Audit`           | `AuditEntry` (operator audit)                   | P02c-4    | ⏳                                            |
| `Operators`       | (Keycloak-backed; permission mapping)           | P02c-4    | ⏳                                            |

> **On the four P02c-1 docs:** they were authored as the design specification _before_
> implementation, and the P02c-1 agent implemented against them. Since P02c-1 merged
> (2026-08-09) they are the living description of code on `main`, and a change to the
> code changes them. The cross-cutting design they depend on lives in
> [../architecture/module-topology.md](../architecture/module-topology.md),
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
