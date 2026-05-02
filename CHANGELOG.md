# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.4.1] - 2026-04-30

### Breaking

- **Engine query ports (Vault-style):** **`IUserQueryService`**, **`IApiKeyQueryService`**, and **`IApiKeyEntityScopeQueryService`** no longer expose async paged **`Search…Async`** with string filters. They now use synchronous **`Search`** / **`Count`** with optional **`Expression<Func<TDto, bool>>?`** predicates (Linq2Db-translatable), **`skip` / `limit`**, and **`Result`** types—matching the thin-search wiring in **`IdentityService`** / **`ApiKeyService`**. Custom Engine hosts must update call sites and registrations.
- **ACME session persistence:** **`IAcmeSessionStore`**, **`AcmePostgresSessionStore`**, **`AcmeSessionSnapshot`**, and **`AcmeSessionJsonSerializer`** are removed. **`ILetsEncryptService`** now depends on **`IAcmeSessionPersistenceService`** (**`AcmeSessionPersistenceServiceLinq2Db`**) for **`acme_sessions`** JSON load/save.
- **`ICertsFlowDomainService`:** Constructor takes **`IRegistrationCacheDomainService`** instead of **`IRegistrationCachePersistenceService`** (registration cache orchestration moved behind **`RegistrationCacheDomainService`**).

### Added

- **`ExpressionCompose`** (`QueryServices/ExpressionCompose.cs`) for composing nested Linq2Db predicates (Vault parity).
- **`IRegistrationCacheDomainService`** / **`RegistrationCacheDomainService`**, **`RegistrationCachePayloadDocument`**, and **`RegistrationCachePayloadJsonTests`** (Engine unit tests) for registration-cache JSON handling; **`RegistrationCacheDto`** now extends **`DtoDocumentBase<Guid>`** with **`AccountId`** as an alias of **`Id`**; persistence and mapping updates in **`RegistrationCachePersistenceServiceLinq2Db`** / **`CertsLinq2DbMapping`**.
- **`IAcmeSessionPersistenceService`**, **`AcmeSessionPersistenceServiceLinq2Db`**, and **`AcmeSessionPayloadMapper`** for PostgreSQL-backed ACME **`State`** persistence.
- **`ApiKeyEntityScopeDto`** and **`ApiKeyEntityScopeQueryServiceStub`** adjustments for entity-scope search parity.
- **Docs:** **`assets/docs/ARCHITECTURE_LAYERING.md`** (layering, spine flows, Pattern A/B); **[CONTRIBUTING.md](CONTRIBUTING.md)** links to it and documents **`dotnet test`** for **`MaksIT.CertsUI.Engine.Tests`** / **`MaksIT.CertsUI.Tests`**.

### Changed

- **`LetsEncryptService`:** Uses **`IAcmeSessionPersistenceService`**; helper updates in **`LetsEncryptService.Helpers.cs`**.
- **`CertsFlowDomainService`:** **`PurgeStaleHttpChallengesAsync`** (HTTP-01 cleanup); **`AutoRenewal`** calls it before renewal work.
- **`CacheService`:** Thin façade over **`IRegistrationCacheDomainService`** (host API unchanged for callers).
- **`IdentityService`** / **`ApiKeyService`:** Build predicates and call **`Count`** + **`Search`** on **`IUserQueryService`** / **`IApiKeyQueryService`** / **`IApiKeyEntityScopeQueryService`**.
- **Engine:** Dropped **`Newtonsoft.Json`** package reference from **`MaksIT.CertsUI.Engine`** (STJ-only JSON paths).
- **Web UI:** **`axiosConfig`** **`getData`** / **`postData`** (and related helpers) return **`{ payload, status, ok }`** so callers can distinguish HTTP status; forms and slices updated (**`SearchUser`**, **`SearchApiKey`**, **`Utilities`**, **`EditUser`**, **`Home`**, **`FileUploadComponent`**, **`identitySlice`**, etc.).
- **Integration tests:** **`InMemoryUserStore`**, **`CacheServiceTests`**, **`CertsFlowServiceTests`**, **`ApiKeyQueryServiceIntegrationTests`**, **`AccountServicePatchAccountIntegrationTests`** aligned with the new ports.

## [3.4.0] - 2026-04-27

### Breaking

- **HA / interactive ACME:** `CertsFlowDomainService` no longer checks `IPrimaryReplicaWorkload.IsPrimary`. All replicas may run configure-client, init, orders, challenge completion, certificate download, apply, and revoke. The API no longer returns **HTTP 503** with `ProblemDetails.type` **`urn:maksit:certs-ui:primary-replica-required`** for those flows. Clients that retried on that signal (for example the SPA) should treat normal error semantics only.
- **HTTP-01 challenge:** `AcmeChallengeAsync` no longer writes tokens under **`AcmeFolder`** or reads a legacy on-disk file. Challenge text is served from PostgreSQL only; ingress must reach **`GET /.well-known/acme-challenge/{token}`** on this app (or equivalent) rather than a shared volume of token files.
- **Startup:** Removed the shared **`init`** marker file under **`DataFolder`**. Followers wait until the database reports at least one user (same readiness signal, without filesystem coupling).
- **HA / process model:** Removed **`IPrimaryReplicaWorkload`**, **`PrimaryReplicaGate`**, and **`PrimaryReplicaShutdownHostedService`**. There is no long-lived “primary replica” or lease renewal loop in the API process.

### Changed

- **ACME sessions:** Let's Encrypt client **`State`** is persisted in PostgreSQL table **`acme_sessions`** (`session_id`, payload JSON, timestamps) so any replica can continue the same ACME session after load balancing.
- **LetsEncrypt / `HttpClient`:** `ConfigureClient` fetches the ACME directory using an absolute URL derived from staging/production configuration instead of assigning **`BaseAddress`** on the shared **`HttpClient`**.
- **`InitializationHostedService`:** Dropped unused **`IOptions<Configuration>`** from the constructor (DI callers unchanged except the removed parameter).
- **Bootstrap:** **`InitializationHostedService`** acquires **`certs-ui-bootstrap`** (`RuntimeLeaseNames.BootstrapCoordinator`), runs **`CoordinationTableProvisioner`** + optional default admin, **releases** the lease, and exits. Other pods wait until **`users`** exist.
- **Renewal:** **`AutoRenewal`** acquires **`certs-ui-renewal-sweep`** (`RuntimeLeaseNames.RenewalSweep`) for each sweep, runs work, **releases**, then sleeps. Any pod may win the next sweep.
- **Lease names:** Replaced **`certs-ui-primary`** with **`BootstrapCoordinator`** and **`RenewalSweep`** constants (see **`RuntimeLeaseNames`**).
- **Helm (cloud-native defaults):** **`components.server.service.sessionAffinity.enabled`** defaults to **`false`** so the server `Service` uses stateless load balancing (no **`ClientIP`** stickiness). Enable explicitly only when needed.
- **Helm:** **`certsClientRuntime.apiUrl`** default is **`/api`** so the Web UI calls the API on the same browser origin (typical single-ingress / reverse-proxy setup). Override with a full URL when UI and API are on different hosts.

### Removed

- **`CertsFlowPrimaryReplica`**, **`PrimaryReplicaRequiredObjectResult`**, and **`CertsFlowResultExtensions`** / **`ToCertsFlowActionResult`**; **`CertsFlowController`** uses **`ToActionResult()`** like other API controllers.
- **Web UI:** Primary-replica **503** auto-retry logic in **`axiosConfig.ts`**.
- **Configuration / Helm:** **`AcmeFolder`** and **`DataFolder`** settings and the default **server** **acme**/**data** PVC mounts (cloud-native: no app-local disk for ACME or bootstrap markers). **`AddMemoryCache()`** host registration removed (unused).

### Upgrade notes

- **Migrations:** Apply FluentMigrator through **`3.4.0`** (includes **`acme_sessions`** and related coordination entries) before relying on cross-replica ACME sessions.
- **Compose / secrets:** Remove **`acme`** and **`data`** bind mounts from **`docker-compose.override.yml`** if you still have them; they are no longer read by the application.
- **Operations:** If you alert or filter on lease name **`certs-ui-primary`**, retarget to **`certs-ui-bootstrap`** and **`certs-ui-renewal-sweep`**.

## [3.3.22] - 2026-04-27

### Changed

- **Release tooling / frontend image versioning:** `DockerPush` now supports per-image `versionEnvFiles` and temporarily rewrites `VITE_APP_VERSION` in `src/MaksIT.WebUI/.env` and `src/MaksIT.WebUI/.env.prod` to the release semver from `<Version>` (for example `3.3.22`) during docker build/push, then restores original files so placeholders remain unchanged in git.
- **HA / Terms of Service PDF cache:** Replaced pod-filesystem Terms of Service PDF caching with shared PostgreSQL cache table `terms_of_service_cache` (`url`, `url_hash_hex`, `etag`, `last_modified_utc`, `content_type`, `content_bytes`, `fetched_at_utc`, `expires_at_utc`). ToS retrieval now uses cache TTL/HTTP validators and no longer relies on local files.
- **Terms of Service API:** Uses only session-based endpoint `GET /api/certs/{sessionId}/terms-of-service` for interactive ACME flows (stateless `isStaging` variant removed).

## [3.3.21] - 2026-04-26

### Changed

- **FluentMigrator:** Restored FluentMigrator defaults for the migration history table (**`VersionInfo`**, columns **`Version`**, **`AppliedOn`**, **`Description`**). Removed **`CertsFluentMigratorVersionTableMetaData`**, custom **`IVersionTableMetaDataAccessor`** registration, and post-migrate verification against **`version_info`**.

## [3.3.20] - 2026-04-26

### Fixed

- **FluentMigrator DI:** Attempted to force **`version_info`** by replacing **`IVersionTableMetaDataAccessor`** registrations. **Superseded in 3.3.21:** reverted to default **`VersionInfo`** table and standard column names (see **3.3.21**).

## [3.3.19] - 2026-04-26

### Removed

- **Startup migrations:** Removed legacy compatibility paths (EF-era baseline that seeded the version table when **`users`** already existed, **`VersionInfo` → `version_info` rename**, PascalCase → snake_case column repair, and **`RunMigrationsService.BaselineVersion`**).

### Upgrade notes

- **Breaking:** **Recreate the Certs engine database** (or use a new empty database) if you still relied on those removed startup paths; **`MigrateUp`** expects a schema managed only by FluentMigrator migrations in-process.

## [3.3.18] - 2026-04-26

### Changed

- **Docker Compose:** **`docker-compose.override.yml`** sets the same **`ReverseProxy__Clusters__*__Destinations__d1__Address`** environment variables as Kubernetes (**`http://server:5000/`** / **`http://client:5173/`** on the Compose network), so YARP behavior does not depend only on baked-in **`appsettings.json`**.
- **Helm:** Optional **`components.reverseproxy.kubernetesUpstreamHosts`** (default **`true`**) toggles injection of in-cluster upstream URLs; set **`false`** only for custom Service naming. Single-replica and HA clusters use the same DNS pattern.

### Fixed

- **Helm / reverseproxy:** YARP upstreams defaulted to Compose hostnames **`server`** / **`client`**, which do not resolve in Kubernetes. The chart sets **`ReverseProxy__Clusters__*__Destinations__d1__Address`** to **`http://<release-fullname>-server:<port>/`** and **`http://<release-fullname>-client:<port>/`** when **`kubernetesUpstreamHosts`** is enabled (ports from **`components.server.service.port`** and **`components.client.service.port`**).

## [3.3.17] - 2026-04-26

### Changed

- **HA / API:** Non-primary replicas return **`Result.ServiceUnavailable`** with stable marker `urn:maksit:certs-ui:primary-replica-required` for ACME orchestration; the host maps that to **HTTP 503**, **`Retry-After`**, and **RFC 7807 `ProblemDetails`** (replacing ad-hoc 429-style overload semantics for this case).
- **Helm:** Default **`components.server.service.sessionAffinity`** (`ClientIP`, configurable timeout), **`terminationGracePeriodSeconds`**, and a short **`preStop` sleep** so rolling updates drain connections before the primary lease TTL window. Disable or tune under **`components.server`** if your ingress already pins API traffic.

## [3.3.16] - 2026-04-26

### Changed

- **HA / primary replica:** A single elected instance holds Postgres lease `certs-ui-primary` (`RuntimeLeaseNames.PrimaryReplica`), renews it periodically, and is the only instance with `IPrimaryReplicaWorkload.IsPrimary` after startup. It runs coordination DDL, identity bootstrap, **all ACME domain flows** (`CertsFlowDomainService`), and **`AutoRenewal`**. Other replicas serve HTTP (identity, health, etc.) and **`AcmeChallengeAsync`** (HTTP-01 token materialization for ingress). Followers reject ACME orchestration at the domain layer until they become primary after failover.
- **Startup:** Removed separate `certs-ui-bootstrap` lease; primary lease serializes first-time admin creation. `PrimaryReplicaShutdownHostedService` (registered last) releases the primary lease on shutdown.

## [3.3.15] - 2026-04-26

### Fixed

- **Startup / HA:** `InitializationHostedService` no longer takes the bootstrap lease when PostgreSQL already has users. Only the empty-database path waits on the lease (single-writer default admin). Extra replicas used to block on the lease until Kubernetes canceled `StartAsync`, surfacing as `TaskCanceledException` at startup while the first replica held the lease.
- **Startup:** Retry backoff treats `OperationCanceledException` when the host is stopping as shutdown (no misleading “initialization failed” loop); cooperative cancel still ends startup.

## [3.3.14] - 2026-04-26

### Fixed

- **Identity / PostgreSQL:** Removed redundant `users.JwtTokensJson` (historical JSON blob of sessions on the user row). **Server-side session allowlist semantics are unchanged:** issued sessions remain rows in `jwt_tokens` and are validated the same way as in Vault’s persisted `JwtToken` model—only the duplicate JSON encoding was dropped. New `users` inserts no longer hit `23502` on that column. FluentMigrator `DropUsersJwtTokensJson` (`20260426140000`) drops the column when present; the baseline no longer creates it; `JwtTokensTableMigrateFromJson` copies from JSON only if that column still exists (upgrades from older DBs).

### Changed

- **FluentMigrator:** `RestoreUsersJwtTokensJsonIfDropped` (`20260426120000`) is now a no-op (revision kept for databases that already applied it). Session material is stored only in `jwt_tokens`, not duplicated as JSON on `users`.

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

- **Database:** FluentMigrator `RestoreUsersJwtTokensJsonIfDropped` (`20260426120000`) initially re-added `users.JwtTokensJson` with `ADD COLUMN IF NOT EXISTS` for databases that had dropped it under an older `JwtTokensTableMigrateFromJson` revision. **Superseded in 3.3.14:** that revision is a no-op and `DropUsersJwtTokensJson` drops the column; tokens stay in `jwt_tokens` only.
- **Helm / config:** `certsServerConfig.configuration.certsEngineConfiguration.autoSyncSchema` (default `true`) is rendered into server `appsettings.json` so add-only schema sync runs on every startup unless explicitly disabled.

### Changed

- **Startup schema policy:** Documented expand-only expectations — FluentMigrator `Up()` should add tables/columns; avoid dropping renamed columns in routine `Up()` without an explicit follow-up plan. `JwtTokensTableMigrateFromJson` no longer drops `JwtTokensJson` in that revision’s `Up()` (tokens are normalized into `jwt_tokens`). **3.3.14** removes `JwtTokensJson` from the live schema via `DropUsersJwtTokensJson`.
- **Schema sync:** `AutoSyncSchema` defaults to **true** in repo `appsettings.json`; `SchemaSyncService` desired map includes `users.IsActive` and `TwoFactorSharedKey`. **3.3.14** stops treating `JwtTokensJson` as a desired column. Still **ADD COLUMN IF NOT EXISTS** only (no DROP in sync).
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
- **Engine platform expansion:** Added a domain-oriented `MaksIT.CertsUI.Engine` structure (`Domain`, `Dto`, `DomainServices`, `Persistence`, `QueryServices`, `Infrastructure`, `FluentMigrations`) with linq2db mappings and migration services.
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



