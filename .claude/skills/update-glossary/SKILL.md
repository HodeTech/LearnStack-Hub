---
name: update-glossary
description: >
  Add or refine a Hub-specific term in docs/glossary.md. USE FOR: introducing a
  Hub-specific term (Plan, HubSubscription, Entitlement, generation, LearnStackApiClient,
  Hub APISIX, learnstack-hub realm, etc.) that other Hub docs cite; fixing a stale
  entry; removing a deprecated term. DO NOT USE FOR: cross-cutting terms shared with
  LearnStack core (Tenant, Organization, IModule, DeploymentMode — those live in
  https://github.com/HodeTech/LearnStack/blob/main/docs/glossary.md; link to them), general programming terms, or terms
  only used inside docs/analysis/ (gitignored).
---

# Updating the Hub glossary

## Purpose

Keep `docs/glossary.md` the single source of truth for Hub-specific terminology. New Hub terms go here first, then get used elsewhere.

## When to use

- A new Hub-specific concept appears in a doc, aggregate, or skill and other docs will cite it.
- An existing entry is stale or a term is deprecated.

## When not to use

- The term is cross-cutting (shared with LearnStack core) → it lives in [LearnStack's glossary](https://github.com/HodeTech/LearnStack/blob/main/docs/glossary.md); link to it, don't duplicate.
- General programming term → link to its canonical source.
- Term used only in `docs/analysis/` → not glossary-worthy.

## Workflow

### Step 1 — Confirm it's Hub-specific

If LearnStack core also uses the term with the same meaning, it belongs in LearnStack's glossary. Hub's glossary holds only terms that are Hub-only (`Plan`, `HubSubscription`, `Entitlement`, `generation`, `LicenseKey`, `CustomDomain`, `CompliancePolicy`, `WebhookLedger`, `UsageAggregate`, `LearnStackApiClient`, `Hub APISIX`, `learnstack-hub` realm, Hub Vault namespace, `learnstack.hub.*` prefix, `OperatorId`, …).

### Step 2 — Add / refine the entry

Open [docs/glossary.md](../../../docs/glossary.md). Add the term in the right section (Hub aggregates / Hub infrastructure terms), alphabetically within the section. One tight definition; cite the authoritative source (an ADR, an architecture doc) with a relative link. Cross-link related Hub terms.

### Step 3 — Don't redefine elsewhere

Per single-source-of-truth, other docs **link** to the glossary entry; they don't restate the definition.

## Validation

- Entry is in `docs/glossary.md`, in the right section, alphabetical.
- Definition is one tight paragraph with a source link.
- The term is genuinely Hub-specific (not a cross-cutting term that belongs in LearnStack's glossary).

## Common pitfalls

- **Duplicating a cross-cutting term.** `Tenant`, `Organization`, `IModule` live in LearnStack's glossary; Hub links to them.
- **Restating the definition in another doc.** Link, don't copy.
