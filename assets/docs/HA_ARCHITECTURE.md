# High Availability Architecture

This document explains how HA works in `MaksIT.CertsUI` after moving mutable ACME coordination state to PostgreSQL.

## Goals

- Run multiple `server` replicas without ACME race conditions.
- Keep HTTP-01 challenge tokens coherent across replicas.
- Ensure startup/bootstrap and renewal loops do not run in parallel on every pod.
- Expose health endpoints suitable for Kubernetes probes.

## Runtime model

- **Shared source of truth:** PostgreSQL stores ACME challenge rows and runtime leases.
- **Per-instance identity:** each running server process gets one canonical `InstanceId` (`IRuntimeInstanceId` singleton).
- **Lease holder:** mutating ACME paths acquire a PostgreSQL lease row (`app_runtime_leases`) with TTL.
- **Challenge reads:** `/.well-known/acme-challenge/{token}` reads token value from PostgreSQL and materializes a short-lived file in `/acme` for compatibility.
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
- Challenge handler (`AcmeChallengeAsync`) reads token value from DB, writes `/acme/{token}`, and returns the value.
- Fallback: if DB row is missing, legacy on-disk token read remains available for migration compatibility.
- Cleanup: auto-renewal loop calls `DeleteOlderThanAsync(TimeSpan.FromDays(10))`.

## Kubernetes behavior

- `server` can run with `replicaCount >= 2` when your storage/network setup allows it.
- Server readiness and liveness probes are wired to:
  - `GET /health/ready` (DB roundtrip check),
  - `GET /health/live` (process liveness).
- Helm now sets `POD_NAME` from `metadata.name` for stable per-pod identity.

## Current non-goals and boundaries

- **Agent remains single-instance** by design near edge proxy.
- **Only HTTP-01** challenge type is supported currently.
- Optional split of ACME worker into a dedicated workload is not implemented yet.

## Files involved

### Core coordination contracts

- `src/MaksIT.CertsUI.Engine/RuntimeCoordination/IRuntimeInstanceId.cs`
- `src/MaksIT.CertsUI.Engine/RuntimeCoordination/RuntimeLeaseNames.cs`
- `src/MaksIT.CertsUI.Engine/Infrastructure/IRuntimeLeaseService.cs`
- `src/MaksIT.CertsUI.Engine/Persistance/Services/IAcmeHttpChallengePersistenceService.cs`

### PostgreSQL implementation

- `src/MaksIT.CertsUI.Engine/Infrastructure/RuntimeLeaseServiceNpgsql.cs`
- `src/MaksIT.CertsUI.Engine/Persistance/Services/Linq2Db/AcmeHttpChallengePersistenceServiceLinq2Db.cs`
- `src/MaksIT.CertsUI.Engine/Data/CertsLinq2DbMapping.cs`
- `src/MaksIT.CertsUI.Engine/FluentMigrations/20260425130000_AcmeChallengesAndRuntimeLeases.cs`
- `src/MaksIT.CertsUI.Engine/Infrastructure/SchemaSyncService.cs`

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

