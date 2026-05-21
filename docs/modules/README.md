# Hub Module Deep Dives

Per-module documentation lands here as modules ship in P02c-1+ and Phase 09b. P02c-0 ships only this placeholder.

## Module topology

| Module            | Aggregates                                      | Phase     | Doc status |
| ----------------- | ----------------------------------------------- | --------- | ---------- |
| `TenantLifecycle` | `LearnStackTenant` (mirror)                     | P02c-1    | ⏳         |
| `Plans`           | `Plan`, `PlanTier`                              | P02c-1    | ⏳         |
| `Subscriptions`   | `HubSubscription`                               | P02c-1    | ⏳         |
| `Entitlements`    | `Entitlement` (projection)                      | P02c-1    | ⏳         |
| `CustomDomains`   | `CustomDomain`                                  | P02c-5    | ⏳         |
| `Compliance`      | `CompliancePolicy`                              | P02c-5    | ⏳         |
| `Usage`           | `UsageAggregate`                                | P02c-2    | ⏳         |
| `LicenseKeys`     | `LicenseKey`                                    | P02c-6    | ⏳         |
| `Invoicing`       | `HubInvoice`, `HubInvoiceLine`, `WebhookLedger` | Phase 09b | ⏳         |
| `Audit`           | `AuditEntry` (operator audit)                   | P02c-4    | ⏳         |
| `Operators`       | (Keycloak-backed; permission mapping)           | P02c-4    | ⏳         |

## Per-module doc shape

When a module ships, its deep-dive carries:

- **Aggregates + state machines** — entity diagram, allowed state transitions.
- **DbContext + EF configurations** — table layout, indexes, JSONB shapes.
- **MediatR contracts** — commands + queries the module exposes, with idempotency keys where applicable.
- **Integration events** — Dapr topic names (`learnstack.hub.<module>.*`), payload shape, consumer modules.
- **Permission keys** — operator-scope permissions the module declares.
- **Audit-coverage matrix** — MUST / SHOULD / MAY classification per operation (mirrors LearnStack [Standards 18 § Audit Coverage](../../../learnstack/docs/standards/18-audit-coverage.md)).
- **Architecture-test rules** — module-specific NetArchTest assertions.

## Audit-coverage matrices

Each module ships its audit matrix in `docs/modules/<module-name>/audit.md`. The matrix follows the same shape as LearnStack core (per Standards 18). Hub's operator audit pipeline (P02c-4) consumes these matrices automatically.
