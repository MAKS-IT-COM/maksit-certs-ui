# High Availability Architecture

This document explains how HA works in `MaksIT.CertsUI` after moving mutable ACME coordination state to PostgreSQL.

## Goals

- Run multiple `server` replicas without ACME race conditions.
- Keep HTTP-01 challenge tokens coherent across replicas.
- Ensure startup/bootstrap and renewal loops do not run in parallel on every pod.
- Expose health endpoints suitable for Kubernetes probes.

## Runtime model

- **Shared source of truth:** PostgreSQL stores ACME sessions, challenge rows, ToS cache, registration caches, and runtime leases.
- **Per-instance identity:** each running server process gets one canonical `InstanceId` (`IRuntimeInstanceId` singleton).
- **Lease holder:** `NewOrderAsync` acquires **AcmeWriter**; startup uses **BootstrapCoordinator**; each renewal sweep uses **RenewalSweep** (see `RuntimeLeaseNames`). All leases are rows in **`app_runtime_leases`** with TTL semantics—no long-lived leader object in the app.
- **Challenge reads:** `/.well-known/acme-challenge/{token}` returns the token value from PostgreSQL (no local ACME directory).
- **Background coordination:** bootstrap and renewal hosted services use named leases to avoid duplicate work.

## Lease design

- Lease table key: `lease_name`.
- Lease owner: `holder_id` (instance id).
- Acquire semantics:
  - insert new row if missing;
  - steal only when expired;
  - renew when current holder matches.
- Release semantics:
  - delete only when `lease_name` and `holder_id` both match.

This is implemented as an optimistic single-statement `INSERT ... ON CONFLICT ... DO UPDATE ... WHERE ...` flow in PostgreSQL.

## HTTP-01 coherence design

- `NewOrderAsync` stores challenge tokens in `acme_http_challenges` via `UpsertAsync`.
- Challenge handler (`AcmeChallengeAsync`) reads the token value from the database and returns it as plain text.
- Cleanup: auto-renewal loop calls `DeleteOlderThanAsync(TimeSpan.FromDays(10))`.

## Kubernetes behavior

- Set `components.server.replicaCount >= 2` with **shared external PostgreSQL** (the Helm chart does not deploy Postgres).
- Set **`certsServerSecrets.certsEngineConfiguration.connectionString`**, **`adminUsername`** / **`adminPassword`**, **`jwtSecret`**, and **`passwordPepper`** (or an existing Secret with `appsecrets.json`).
- Probes: `GET /health/live` (process up), `GET /health/ready` (PostgreSQL + migrations + bootstrap coordination complete), `GET /health/startup` (JSON phase timings for debugging).
- Server pods use a **startupProbe** on `/health/ready` so slow first boot (FluentMigrator, admin bootstrap) does not fail liveness/readiness prematurely.
- Helm sets `POD_NAME` from `metadata.name` for stable per-pod identity.
- No application-data PVC is required (ACME sessions, HTTP-01 tokens, and identity state live in PostgreSQL).

## Startup sequence

1. **PostgreSQL** — accept connections on maintenance DB (`postgres`), then create app database if missing.
2. **FluentMigrator** — `MigrateUp` with retries while Postgres is still initializing.
3. **Coordination DDL** — `app_runtime_leases`.
4. **Schema sync** — optional add-only column sync when `AutoSyncSchema` is enabled.
5. **Bootstrap coordination** — one replica acquires the `certs-ui-bootstrap` lease and seeds the global admin; followers wait until an admin exists.

**Docker Compose (local dev):** bundled `postgres` service with `pg_isready` healthcheck; `server` starts only after `service_healthy`. Connection string comes from mounted `appsecrets.json`, not Helm values.

Phase timings are tracked in **`CertsStartupState`** and exposed at **`GET /health/startup`**.

## Current non-goals and boundaries

- **Agent remains single-instance** by design near edge proxy.
- **Only HTTP-01** challenge type is supported currently.
- Optional split of ACME worker into a dedicated workload is not implemented yet.

## Files involved

### Core coordination contracts

- `src/MaksIT.CertsUI.Engine/RuntimeCoordination/IRuntimeInstanceId.cs`
- `src/MaksIT.CertsUI.Engine/RuntimeCoordination/RuntimeLeaseNames.cs`
- `src/MaksIT.CertsUI.Engine/Infrastructure/IRuntimeLeaseService.cs`
- `src/MaksIT.CertsUI.Engine/Persistence/Services/IAcmeHttpChallengePersistenceService.cs`

### PostgreSQL implementation

- `src/MaksIT.CertsUI.Engine/Infrastructure/RuntimeLeaseServiceNpgsql.cs`
- `src/MaksIT.CertsUI.Engine/Persistence/Services/Linq2Db/AcmeHttpChallengePersistenceServiceLinq2Db.cs`
- `src/MaksIT.CertsUI.Engine/Data/CertsLinq2DbMapping.cs`
- `src/MaksIT.CertsUI.Engine/FluentMigrations/20260425130000_AcmeChallengesAndRuntimeLeases.cs`
- `src/MaksIT.CertsUI.Engine/Infrastructure/SchemaSyncService.cs`

### Startup tracking

- `src/MaksIT.CertsUI/Infrastructure/CertsStartupState.cs`
- `src/MaksIT.CertsUI.Engine/Infrastructure/IDatabaseStartupObserver.cs`
- `src/MaksIT.CertsUI.Engine/Infrastructure/DatabaseStartupPhaseRunner.cs`
- `src/MaksIT.CertsUI.Engine/Infrastructure/RunMigrationsService.cs`

### Runtime usage in app flows

- `src/MaksIT.CertsUI.Engine/DomainServices/CertsFlowDomainService.cs`
- `src/MaksIT.CertsUI/HostedServices/InitializationHostedService.cs`
- `src/MaksIT.CertsUI/HostedServices/AutoRenewal.cs`
- `src/MaksIT.CertsUI/Infrastructure/RuntimeInstanceIdProvider.cs`
- `src/MaksIT.CertsUI/Program.cs`
- `src/MaksIT.CertsUI/Controllers/WellKnownController.cs`
- `src/MaksIT.CertsUI/Services/CertsFlowService.cs`

### Helm and deployment wiring

- `src/helm/values.yaml`
- `src/helm/templates/deployments.yaml`
- `src/helm/templates/poddisruptionbudget.yaml`

### Tests

- `src/MaksIT.CertsUI.Tests/Services/CertsFlowServiceTests.cs`

## Related docs

- [ARCHITECTURE_LAYERING.md](./ARCHITECTURE_LAYERING.md) · [USER_AND_API_KEY_RBAC.md](./USER_AND_API_KEY_RBAC.md) · [REVERSE_PROXY_ROUTING.md](./REVERSE_PROXY_ROUTING.md)
