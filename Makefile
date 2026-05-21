# LearnStack Hub — repo-root orchestrator.
#
# Run `make help` for the target list. Every recipe runs from the repo root
# so `${VAR:-default}` interpolation in `infra/compose/dev.yml` reads the
# repo-root `.env` (the developer's copy of `.env.example`).
#
# This Makefile mirrors the LearnStack core Makefile shape — same target
# names, same color helpers, same compose layering. The differences:
#   - Hub compose stack is minimal (Dapr sidecar + APISIX + Postgres init);
#     Postgres / Valkey / Vault / Kafka / Keycloak are shared with LearnStack
#     core's compose, reached via host.docker.internal.
#   - Backend solution is `LearnStack.Hub.slnx`.
#   - Frontend app is `apps/operator-portal` (not apps/web).
#
# Hub does NOT run its own Postgres / Valkey / Vault / Kafka / Keycloak in
# dev. The expectation is that LearnStack core's compose is already up.

SHELL := /usr/bin/env bash
.SHELLFLAGS := -eu -o pipefail -c
.DEFAULT_GOAL := help
.ONESHELL:

# Compose layering — dev.yml is always the base; e2e.yml overlays for the
# end-to-end test suite.
COMPOSE_DEV  := docker compose -f infra/compose/dev.yml
COMPOSE_E2E  := docker compose -f infra/compose/dev.yml -f infra/compose/e2e.yml

# Colour helpers (no-op when stdout is not a TTY).
ifeq ($(shell test -t 1 && echo 1),1)
  CYAN  := \033[36m
  RESET := \033[0m
else
  CYAN  :=
  RESET :=
endif

# ─── Help ─────────────────────────────────────────────────────────────────
.PHONY: help
help: ## Show this help, listing every target and its one-line description.
	@printf "LearnStack Hub Makefile — common targets:\n\n"
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z0-9_.-]+:.*?## / {printf "  $(CYAN)%-18s$(RESET) %s\n", $$1, $$2}' $(MAKEFILE_LIST)

# ─── Dev infrastructure ───────────────────────────────────────────────────
.PHONY: dev
dev: .env ## Bring the Hub-side dev stack up (Dapr sidecar, APISIX, Postgres init). Requires LearnStack core compose to already be running.
	$(COMPOSE_DEV) up -d
	@printf "\n$(CYAN)Hub stack up.$(RESET) Tail logs with: make logs\n"
	@printf "$(CYAN)Reminder:$(RESET) Hub depends on LearnStack core compose (postgres, keycloak, vault, kafka, valkey, mailpit) on host.docker.internal.\n"

.PHONY: down
down: ## Stop the Hub dev stack (preserves volumes).
	$(COMPOSE_DEV) down

.PHONY: clean
clean: ## Stop the Hub dev stack AND drop named volumes (destructive — wipes Hub data).
	$(COMPOSE_DEV) down -v

.PHONY: logs
logs: ## Tail Hub compose logs (Ctrl+C to detach).
	$(COMPOSE_DEV) logs -f --tail=100

.PHONY: ps
ps: ## Show Hub service health summary.
	$(COMPOSE_DEV) ps

.PHONY: e2e-up
e2e-up: .env ## Bring the Hub stack up with the e2e overlay (tmpfs volumes — ephemeral).
	$(COMPOSE_E2E) up -d
	@printf "\n$(CYAN)Hub E2E stack up.$(RESET) Data is ephemeral — every restart wipes state.\n"

.PHONY: e2e-down
e2e-down: ## Stop the Hub e2e overlay.
	$(COMPOSE_E2E) down

# ─── Build ────────────────────────────────────────────────────────────────
.PHONY: build
build: build-backend build-frontend ## Build backend + frontend.

# Multi-line recipes that `cd` into different subdirs MUST wrap each `cd` in
# a subshell (`(cd X && …)`), because `.ONESHELL:` keeps every line of the
# recipe in the SAME shell — without subshells the cwd of line 1 leaks into
# line 2 and the second `cd <relative-path>` blows up.

.PHONY: build-backend
build-backend: ## `dotnet build` the Hub solution.
	(cd backend && dotnet build LearnStack.Hub.slnx --nologo)

.PHONY: build-frontend
build-frontend: ## `pnpm -r build` the frontend monorepo.
	(cd frontend && pnpm -r build)

# ─── Tests ────────────────────────────────────────────────────────────────
.PHONY: test
test: test-backend test-frontend ## Run all test suites (backend + frontend).

.PHONY: test-backend
test-backend: ## `dotnet test` (unit + architecture + contract; integration skipped — see test-integration).
	(cd backend && dotnet test LearnStack.Hub.slnx \
	  --filter "FullyQualifiedName!~LearnStack.Hub.Tests.Integration" \
	  --nologo)

.PHONY: test-integration
test-integration: ## Testcontainers-backed integration tests (P02c-2+; requires Docker).
	(cd backend && dotnet test tests/LearnStack.Hub.Tests.Integration/LearnStack.Hub.Tests.Integration.csproj --nologo)

.PHONY: test-frontend
test-frontend: ## `pnpm -r test` (Vitest component + lib tests).
	(cd frontend && pnpm -r test)

# ─── Lint / format ────────────────────────────────────────────────────────
.PHONY: lint
lint: lint-backend lint-frontend ## Run linters (backend dotnet-format check + frontend ESLint).

.PHONY: lint-backend
lint-backend: ## `dotnet format` verify (no changes — fails on diff).
	(cd backend && dotnet format LearnStack.Hub.slnx --verify-no-changes --no-restore)

.PHONY: lint-frontend
lint-frontend: ## `pnpm -r lint` (Next/ESLint).
	(cd frontend && pnpm -r lint)

.PHONY: format
format: ## Apply formatters in place (backend dotnet-format + frontend prettier).
	(cd backend && dotnet format LearnStack.Hub.slnx --no-restore)
	(cd frontend && pnpm -r exec prettier --write .)

# ─── Typecheck (frontend) ─────────────────────────────────────────────────
.PHONY: typecheck
typecheck: ## `pnpm -r typecheck` (tsc --noEmit across the monorepo).
	(cd frontend && pnpm -r typecheck)

# ─── Seed ─────────────────────────────────────────────────────────────────
.PHONY: seed
seed: dev ## Bring the Hub stack up and seed demo data (idempotent; P02c-1+ adds real seed content).
	./scripts/seed.sh

# ─── Bootstrap ────────────────────────────────────────────────────────────
.PHONY: install
install: .env hooks ## Restore backend NuGet + frontend pnpm deps + activate git hooks.
	(cd backend && dotnet restore LearnStack.Hub.slnx)
	(cd frontend && pnpm install --frozen-lockfile)

.PHONY: hooks
hooks: ## Activate the repo's pre-commit hook (.githooks/pre-commit).
	@git config core.hooksPath .githooks
	@printf "$(CYAN)git hooks → .githooks/ (pre-commit: dotnet format + prettier + eslint + leakwatch if available)$(RESET)\n"

# ─── Env scaffolding ──────────────────────────────────────────────────────
.env: .env.example
	@cp -n .env.example .env
	@touch .env
	@printf "$(CYAN).env ready (copied from .env.example if missing).$(RESET)\n"
