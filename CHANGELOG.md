# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.3.13] - 2026-04-26

### Fixed

- **HA lease / `42P01`:** Added `CoordinationTableProvisioner` with explicit `public.*` DDL; `InitializationHostedService` calls it immediately before bootstrap lease acquire (idempotent, same as post-migrate repair). `RuntimeLeaseServiceNpgsql` now uses `public.app_runtime_leases` in SQL so a non-default `search_path` cannot miss the table. Post-migrate verification requires `public.app_runtime_leases` plus `users` or `"VersionInfo"`.

### Upgrade notes (Kubernetes / Helm)

- **Pin container tags to the app semver** (e.g. `3.3.13` for server, client, reverseproxy) via `global.image.tag` and/or `components.*.image.tag`. The chart resolves the effective tag with `global.image.tag` when set (see `src/helm/templates/_helpers.tpl`).
- **Do not rely on `latest` + `imagePullPolicy: IfNotPresent` alone** — nodes keep the first pulled digest, so you can run an old server binary while the OCI chart is already `3.3.13`. Use an explicit semver tag and/or `pullPolicy: Always` (or bump `global.rolloutNonce` / `global.rollme` per chart NOTES) when upgrading.
- **Push all three images** for the tag you pin (`certs-ui/server`, `certs-ui/client`, `certs-ui/reverseproxy`) so every deployment can pull successfully.

## [3.3.12] - 2026-04-26

### Fixed

- **FluentMigrator:** Use `.ScanIn(…).For.All()` instead of `.For.Migrations()` so in-process discovery matches FluentMigrator guidance (avoids “no migrations” / incomplete runner behavior in some versions).
- **FluentMigrator:** Throw if the engine connection string is empty when registering the runner — a null/empty `WithGlobalConnectionString` puts the processor in **connectionless/preview** mode (SQL logged, **nothing committed**), which matches reports of empty databases with no errors.
- **Migrations:** Log host/database (no password) and count of `[Migration]` types before `MigrateUp`; after coordination DDL, verify `public.users` or `public."VersionInfo"` exists or fail with an actionable error (wrong `Database=`, permissions, or preview mode).
- **Database bootstrap:** If the role cannot open a maintenance connection to database `postgres` (common for locked-down app users), log a warning and skip automatic `CREATE DATABASE` instead of failing the whole migration step.

## [3.3.11] - 2026-04-26

### Added

- **Database:** FluentMigrator `RestoreUsersJwtTokensJsonIfDropped` (`20260426120000`) restores `users.JwtTokensJson` with `ADD COLUMN IF NOT EXISTS` when an older database had it removed by a prior `JwtTokensTableMigrateFromJson` revision.
- **Helm / config:** `certsServerConfig.configuration.certsUIEngineConfiguration.autoSyncSchema` (default `true`) is rendered into server `appsettings.json` so add-only schema sync runs on every startup unless explicitly disabled.

### Changed

- **Startup schema policy:** Documented expand-only expectations — FluentMigrator `Up()` should add tables/columns; avoid dropping renamed or legacy columns in `Up()`. `JwtTokensTableMigrateFromJson` no longer drops `JwtTokensJson` (tokens remain in `jwt_tokens`; legacy JSON column may remain for audit).
- **Schema sync:** `AutoSyncSchema` defaults to **true** in repo `appsettings.json`; `SchemaSyncService` desired map includes `users.IsActive`, `TwoFactorSharedKey`, and optional `JwtTokensJson` for additive repair. Still **ADD COLUMN IF NOT EXISTS** only (no DROP).
- **ICertsEngineConfiguration / ISchemaSyncService:** Clarified that add-only sync is recommended and describes the no-DROP guarantee.

## [3.3.10] - 2026-04-26

### Fixed

- **Database:** After FluentMigrator `MigrateUp`, `RunMigrationsService` applies idempotent `CREATE TABLE IF NOT EXISTS` / `CREATE INDEX IF NOT EXISTS` for `acme_http_challenges` and `app_runtime_leases`. If `VersionInfo` already records the migration but tables are missing (restore drift, partial apply, manual DB edits), FluentMigrator would skip `Up()` and the bootstrap lease would fail with `42P01`; this repair aligns schema with runtime needs.

## [3.3.9] - 2026-04-26

### Fixed

- **Startup / database:** FluentMigrator (`EnsureCertsEngineMigratedAsync`) now runs in `Program.cs` immediately after `WebApplication.Build()` and before `RunAsync`, so schema (including `app_runtime_leases`) exists before any `IHostedService` starts. `InitializationHostedService` only performs bootstrap lease + identity init.

## [3.3.8] - 2026-04-26

### Fixed

- **Startup / database:** `InitializationHostedService` now runs FluentMigrator (`EnsureCertsEngineMigratedAsync`) before acquiring the bootstrap PostgreSQL lease, so `app_runtime_leases` exists on an empty database (same ordering idea as Vault: migrate first, then coordination).
- **Startup:** While waiting for the bootstrap lease, migrations are not re-run on every poll interval (`migrationsApplied` guard).

### Changed

- **Container image:** `MaksIT.CertsUI` Dockerfile installs `libgssapi-krb5-2` so Npgsql can load GSS/Kerberos support without missing-library warnings on slim `aspnet` images.

## [3.3.7] - 2026-04-25

### Added

- **HA runtime coordination:** Added DB-backed HTTP-01 challenge persistence and runtime lease infrastructure (`acme_http_challenges`, `app_runtime_leases`) plus coordinated startup/renewal execution.
- **Kubernetes readiness model:** Added per-component Helm `replicaCount` + PodDisruptionBudget support and health endpoints (`/health/live`, `/health/ready`) for probes.
- **New backend host:** Added `MaksIT.CertsUI` WebAPI host with controllers, authorization filters (JWT and JWT-or-API-key), hosted services, and mapping/configuration abstractions.
- **Engine platform expansion:** Added a domain-oriented `MaksIT.CertsUI.Engine` structure (`Domain`, `Dto`, `DomainServices`, `Persistance`, `QueryServices`, `Infrastructure`, `FluentMigrations`) with linq2db mappings and migration services.
- **Frontend identity/api-key UX:** Added Users/API Keys pages and forms (`CreateUser`, `EditUser`, `SearchUser`, `CreateApiKey`) with reusable list/filter/paging components.
- **Test suite:** Added `MaksIT.CertsUI.Tests` with service and integration coverage plus shared Postgres/WebAPI fixtures.

### Changed

- **Namespace and solution layout:** Standardized around `MaksIT.CertsUI*` and moved responsibilities into clearer host/engine layers.
- **Engine model organization:** Reorganized ACME and related contracts from legacy top-level `Entities`/`Models` into `Domain` and `Dto`.
- **Helm/runtime behavior:** Updated deployment templates to support `env.valueFrom`, pod-name-based holder identity, and probe wiring for live/ready endpoints.
- **Documentation:** Updated README architecture references and linked HA architecture guidance.
- **WebUI contracts:** Aligned identity/API-key request/response and paged-search models with updated backend endpoints.

### Removed

- **Deprecated host:** Removed legacy `MaksIT.Webapi` project and its old controllers/services/background services.
- **Legacy engine layout:** Removed obsolete top-level engine files (`Entities`, `Models`, previous ACME helper locations, old project `.vscode` files).
- **Old test project:** Removed `MaksIT.Webapi.Tests` in favor of `MaksIT.CertsUI.Tests`.

## [3.3.6] - 2026-04-13

### Added

- **LetsEncrypt:** Per-host ACME rate-limit cooldown on `RegistrationCache` (`AcmeRenewalNotBeforeUtcByHostname`), with HTTP `Retry-After` and problem-detail parsing (`AcmeRetryAfterParser`), structured logging, and `Result.TooManyRequests` when the CA returns `rateLimited`.
- **LetsEncrypt:** `AcmeProblemKind` as an `Enumeration` (RFC 8555 problem `type` URIs) instead of ad hoc strings; `LetsEncrytException` exposes `ProblemKind`, `RetryAfterUtc`, and optional rate-limit hostname.
- **LetsEncrypt:** `AcmeSessionStore` for per-session `State` in memory; `LetsEncryptService` split into partial files (`LetsEncryptService.Helpers.cs`) for HTTP/JWS/error helpers.
- **LetsEncrypt:** `State.TryGetAccountKey` for a single place to validate account key material after `Init`.
- **LetsEncrypt.Tests:** Unit tests for retry parsing, problem-kind resolution, and cooldown JSON round-trip.

### Changed

- **AutoRenewal:** Skips hostnames that are still in an ACME cooldown window (with debug logs for skipped hosts).
- **Certs flow:** Persists registration cache after failed full certificate flows when a session exists so cooldown metadata is saved.
- **LetsEncrypt:** Broader nullable reference annotations on ACME DTOs (`Problem`, `AcmeDirectory`, `AuthorizationChallengeError`, etc.) and explicit null guards in `LetsEncryptService`.

### Fixed

- **LetsEncrypt:** Certificate PEM loading uses `X509Certificate2.CreateFromPem` instead of the obsolete `X509Certificate2(byte[])` constructor (SYSLIB0057).
- **LetsEncrypt:** `RevokeCertificate` now fails correctly on non-success responses (missing `return`), uses the same problem-document handling as other ACME calls, and disposes the HTTP response on successful revoke.
- **LetsEncrypt:** `NewOrder` authorization error log line now logs the authorization status, not the order status.

## [3.3.5] - 2026-04-12

### Changed

- `CachedHostname` now uses a C# 12 primary constructor (same public construction as before).

### Fixed

- `RegistrationCache` loads cached PEM certificates via `X509CertificateLoader.LoadCertificate` and disposes them with `using` where certificates are parsed for expiry and host listing.
- `RegistrationCache.TryGetCachedCertificate` returns `false` when the cached entry has no private key blob, avoiding a null argument when importing key material.

## [3.3.4] - 2026-04-01

### Added

- `MaksIT.Webapi.Tests`: service-level unit tests (settings, cache, identity, agent, account, certs flow) and domain tests for `Settings`.
- Postman collections under `src/Postman` updated to match current `MaksIT.Webapi` routes, JWT flow, and cache endpoints.

### Fixed

- WebUI Terms of Service (Let's Encrypt): PDF viewer loads `pdfjs-dist` worker from a Vite-bundled asset (`pdf.worker.min.mjs?url`) so rendering works in dev and production instead of failing on missing or wrong worker URLs.
- `AccountService.PatchAccountAsync` returns the account built from the cache after reload, not a stale in-memory instance.

## [3.3.3] - 2025-12-20

### Changed

- Relicensed project from GPL-3.0 to Apache-2.0.

## [3.3.2] - 2025-12-20

### Changed

- Minimal Helm chart and documentation improvements.

## [3.3.1] - 2025-11-22

### Changed

- Public release following the v3.3.0 pre-release.

## [3.3.0] - 2025-11-15

### Changed

- Pre-release of the v3.3.x line.

## [3.2.0] - 2025-09-11

### Added

- New WebUI with authentication.

## [3.1.0] - 2024-08-11

### Changed

- Stabilized release following v3.0.0.

## [3.0.0] - 2024-05-31

### Added

- WebAPI and containerization.

## [2.0.0] - 2019-11-01

### Changed

- Dependency injection pattern implementation.

## [1.0.0] - 2019-06-29

### Added

- Initial release.



