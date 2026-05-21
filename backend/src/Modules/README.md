# Hub Modules

This directory is intentionally **empty** in P02c-0. Hub-side module aggregates land in subsequent packets.

## Planned module topology

The 11 Hub modules ([ADR-0019 § Hub data model](../../../../learnstack/docs/decisions/0019-learnstack-hub.md)) arrive across multiple Phase 02c packets:

| Module                                   | Lands in      | Aggregates                                                                   |
| ---------------------------------------- | ------------- | ---------------------------------------------------------------------------- |
| `LearnStack.Hub.Modules.TenantLifecycle` | **P02c-1**    | `LearnStackTenant` (mirror)                                                  |
| `LearnStack.Hub.Modules.Plans`           | **P02c-1**    | `Plan`, `PlanTier`, plan defaults (features / limits / compliance)           |
| `LearnStack.Hub.Modules.Subscriptions`   | **P02c-1**    | `HubSubscription` (lifecycle state machine)                                  |
| `LearnStack.Hub.Modules.Entitlements`    | **P02c-1**    | `Entitlement` (projection from plan + subscription + compliance)             |
| `LearnStack.Hub.Modules.CustomDomains`   | **P02c-5**    | `CustomDomain` (state machine, DNS + TLS lifecycle)                          |
| `LearnStack.Hub.Modules.Compliance`      | **P02c-5**    | `CompliancePolicy` (per-tenant cap overrides)                                |
| `LearnStack.Hub.Modules.Usage`           | **P02c-2**    | `UsageAggregate` (rolled-up tenant metrics from `POST /api/v1/usage/report`) |
| `LearnStack.Hub.Modules.LicenseKeys`     | **P02c-6**    | `LicenseKey` (RSA-2048 signed `.lic` file metadata)                          |
| `LearnStack.Hub.Modules.Invoicing`       | **Phase 09b** | `HubInvoice`, `HubInvoiceLine`, `WebhookLedger`                              |
| `LearnStack.Hub.Modules.Audit`           | **P02c-4**    | `AuditEntry` (operator audit log; separate from LearnStack tenant audit)     |
| `LearnStack.Hub.Modules.Operators`       | **P02c-4**    | Operator role + permission mapping (Keycloak `learnstack-hub` realm-backed)  |

Each module follows the same four-layer pattern as LearnStack core (`Application.Contracts`, `Application`, `Domain`, `Infrastructure`).

## Architecture tests that gate module additions

- `Hub_NeverStores_TenantData` — no `course`, `lesson`, `user` (tenant), `enrollment`, `live_session`, `lesson_item`, `media_asset` entity types may appear under `src/Modules/**`.
- `Hub_Modules_DoNotReference_LearnStack_Internals` — Hub modules may reference local DTO copies of `LearnStack.Application.Contracts` shapes but never `LearnStack.Domain` or `LearnStack.Infrastructure` types.
- `ModuleDomain_DoesNotDependOn_OtherModuleDomain` — cross-module Domain references are forbidden; cross-module communication goes through Application.Contracts or integration events.
- `ModuleDomain_DoesNotDependOn_AnyApplicationOrInfrastructure` — Domain may only reference SharedKernel.

P02c-0 ships only the meta-test (`NetArchTest_DetectsAPlantedViolation`) and placeholder versions of the rules above. They become real once P02c-1 lands aggregates to test against.
