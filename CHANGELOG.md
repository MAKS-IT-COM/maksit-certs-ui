# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.5.2] - 2026-06-04

**Release status:** **3.3.4** is the last published release. **3.5.2** is a patch on **3.5.1** (documentation consolidation, dependency bumps).

### Changed

- **Documentation:** Removed **`assets/docs/`** topic guides (layering, HA, auth, RBAC, proxy, patch, PowerShell); README and CONTRIBUTING point contributors at **[AGENTS.md](AGENTS.md)** (homelab `common/maksit-*` skills); operator-facing health and HA notes stay in README and Helm **`NOTES.txt`**.
- **Helm:** **`NOTES.txt`** health probe note is self-contained (no link to removed HA doc).
- **Dependencies:** **MaksIT.Core** **1.6.7**, **MaksIT.Results** **2.0.2** (host, Engine, Client.Tests).

## [3.5.1] - 2026-06-02

**Release status:** **3.3.4** is the last published release. **3.5.1** is a patch on **3.5.0** (startup health, Helm secrets alignment, RBAC docs, Web UI token refresh).

### Breaking

- **Helm secrets (`certsServerSecrets`):** Renamed **`authSecret`** → **`jwtSecret`**, **`authPepper`** → **`passwordPepper`**. Bootstrap admin username moved from ConfigMap to secrets as **`adminUsername`** (removed **`certsServerConfig.configuration.certsEngineConfiguration.admin.username`**). Update custom values and external secret templates before upgrade.

### Added

- **`GET /health/startup`:** JSON snapshot of phased database and bootstrap startup (`CertsStartupState`, `IDatabaseStartupObserver`, `DatabaseStartupPhaseRunner`).
- **Web UI:** **`webUiAuthRefresh.ts`** — single in-flight JWT refresh shared by axios (and **`resolveWebUiAccessToken`** for future SignalR hubs).
- **Docs:** **`assets/docs/RBAC_REFERENCE.md`**, **`assets/docs/USER_AND_API_KEY_RBAC.md`**; README RBAC overview and table-of-contents links.
- **E2E:** **`src/e2e-tests/README.md`** — PowerShell API-key E2E credentials and compose URL guidance (replaces **`MaksIT.CertsUI.Client.Tests/README.E2E.md`**).

### Changed

- **`GET /health/ready`:** Returns **HTTP 503** until bootstrap coordination completes (`CertsStartupState.IsApplicationReady`), then checks PostgreSQL as before.
- **`RunMigrationsService`:** Reports phased startup; waits on maintenance database **`postgres`** before **`EnsureDatabaseExists`** and on the application database before **`MigrateUp`**.
- **`SchemaSyncService`:** Reports **`schema_sync`** phase via **`IDatabaseStartupObserver`** (no-op timing when **`AutoSyncSchema`** is false).
- **`InitializationHostedService`:** Records bootstrap coordination phase success/failure in **`CertsStartupState`**.
- **`CertsEngineConfiguration.ConnectionString`:** **`required`** in host configuration (no empty default).
- **`IdentityDomainService`:** Configuration field and error messages use **`CertsEngineConfiguration`** naming (not Vault-era **`VaultEngineConfiguration`**).
- **Helm:** Server **`startupProbe`** on **`/health/ready`** (up to ~5 minutes); readiness **`initialDelaySeconds`** reduced to **5**; **`NOTES.txt`** documents **`/health/startup`** and external Postgres.
- **README / Compose examples:** Admin username in **`appsecrets.json`** only; JWT/pepper placeholder names aligned with Helm.
- **Dependencies:** linq2db **6.3.0**, Microsoft.Extensions.\* **10.0.8**, Npgsql **10.0.3**, Swashbuckle **10.2.1**, Testcontainers.PostgreSql **4.12.0**, **Microsoft.NET.Test.Sdk** **18.6.0**, PowerShell **System.Management.Automation** **7.6.2**.

### Removed

- **`MaksIT.CertsUI.Client.Tests`:** **`CertsUiApiKeyE2ETests`** and **`README.E2E.md`** (API-key E2E remains under **`src/e2e-tests/`** and PowerShell scripts).

### Upgrade notes (from 3.5.0)

- **Helm:** Rename secret keys in **`certsServerSecrets`** and set **`adminUsername`**; remove admin username from non-secret ConfigMap overrides if you duplicated it there.
- **Probes:** Use **`/health/startup`** for startup diagnostics; keep load balancers on **`/health/ready`** (503 until the cluster has a bootstrapped user).

## [3.5.0] - 2026-05-24

**Release status:** **3.3.4** is the last published release. **3.5.0** consolidates all changes since **3.3.4** (HA, Engine/Vault alignment, client libraries, Web UI shared packages).

### Breaking

- **HA / interactive ACME:** Removed **`IPrimaryReplicaWorkload`**, **`PrimaryReplicaGate`**, **`PrimaryReplicaShutdownHostedService`**, and **`CertsFlowPrimaryReplica`**. All replicas may run configure-client, init, orders, challenge completion, certificate download, apply, and revoke. The API no longer returns **HTTP 503** with **`urn:maksit:certs-ui:primary-replica-required`**. Clients that retried on that signal should use normal error semantics only.
- **HTTP-01 challenge:** **`AcmeChallengeAsync`** no longer writes tokens under **`AcmeFolder`** or reads legacy on-disk files. Challenge text is served from PostgreSQL only; ingress must reach **`GET /.well-known/acme-challenge/{token}`** on this app (or equivalent).
- **Startup / filesystem:** Removed the shared **`init`** marker under **`DataFolder`**, **`AcmeFolder`**, and **`DataFolder`** settings, and default server **acme**/**data** PVC mounts. Followers wait until the database reports at least one user.
- **Configuration:** **`CertsUIEngineConfiguration`** renamed to **`CertsEngineConfiguration`** in appsettings, Helm **`values.yaml`**, and secrets templates.
- **Engine layout:** **`Persistance`** → **`Persistence`**; **`CertsLinq2DbMapping`** → **`CertsUILinq2DbMapping`**.
- **Engine query ports (Vault-style):** **`IUserQueryService`**, **`IApiKeyQueryService`**, and **`IApiKeyEntityScopeQueryService`** use synchronous **`Search`** / **`Count`** with optional **`Expression<Func<TDto, bool>>?`** predicates, **`skip` / `limit`**, and **`Result`** types — not async paged **`Search…Async`** with string filters.
- **ACME session persistence:** **`IAcmeSessionStore`**, **`AcmePostgresSessionStore`**, **`AcmeSessionSnapshot`**, and **`AcmeSessionJsonSerializer`** removed. **`ILetsEncryptService`** depends on **`IAcmeSessionPersistenceService`** (**`AcmeSessionPersistenceServiceLinq2Db`**) for **`acme_sessions`** JSON load/save.
- **`ICertsFlowDomainService`:** Constructor takes **`IRegistrationCacheDomainService`** instead of **`IRegistrationCachePersistenceService`**.
- **Web UI:** Removed the in-repo shared UI stack (layout, DataTable, form editors, deep/enum helpers, local toast). The SPA depends on **`@maks-it.com/webui-core`**, **`webui-components`**, and **`webui-contracts`** (private npm registry; **`src/MaksIT.WebUI/.npmrc`**).
- **E2E tests:** **`CertsUiApiKeyE2ETests`** removed from **`MaksIT.CertsUI.Tests`**; API-key E2E lives in **`MaksIT.CertsUI.Client.Tests`** (`Category=E2E`).
- **Terms of Service API:** Interactive ACME uses only **`GET /api/certs/{sessionId}/terms-of-service`** (stateless **`isStaging`** variant removed).
- **Startup migrations:** Removed legacy EF-era baseline, **`VersionInfo` → `version_info`** rename repair, PascalCase → snake_case column repair, and **`RunMigrationsService.BaselineVersion`**. **`MigrateUp`** expects schema managed only by in-process FluentMigrator migrations.
- **Database:** **`users.JwtTokensJson`** removed; sessions live in **`jwt_tokens`** only (no duplicate JSON on **`users`**).
- **Deprecated host:** Removed legacy **`MaksIT.Webapi`** and **`MaksIT.Webapi.Tests`** in favor of **`MaksIT.CertsUI`** / **`MaksIT.CertsUI.Tests`**.

### Added

- **`MaksIT.CertsUI.Client`:** HTTP client library with API key auth (**`ICertsUIClient`**, **`CertsUIClient`**, **`ServiceCollectionExtensions`**, **`CertsUIApiException`**).
- **`MaksIT.CertsUI.Contracts`:** Shared wire types (**`AccountResponse`**, **`HostnameResponse`**, **`RuntimeInstanceIdResponse`**).
- **`MaksIT.CertsUI.Client.PowerShell`:** Binary module (**`Connect-CertsUI`**, **`Get-CertsUIAccounts`**, **`Invoke-CertsUICreateAccount`**, etc.; requires **PowerShell 7** on **.NET 10**).
- **`MaksIT.CertsUI.Client.Tests`:** Unit tests plus relocated API-key E2E suite.
- **PowerShell E2E:** **`src/e2e-tests/`** scenarios and **`Test-CertsUiApiKeyE2E.ps1`** / **`.bat`** (Vault-style **`CERTSUI_E2E_CREDENTIALS`**).
- **Docs:** **`assets/docs/POWERSHELL_CLIENT_MODULE.md`**, **`assets/docs/ARCHITECTURE_LAYERING.md`**, HA and login architecture docs; **[CONTRIBUTING.md](CONTRIBUTING.md)** links layering doc and **`dotnet test`** guidance.
- **New backend host:** **`MaksIT.CertsUI`** WebAPI with controllers, JWT and JWT-or-API-key authorization, hosted services, and mapping/configuration abstractions.
- **Engine platform:** Domain-oriented **`MaksIT.CertsUI.Engine`** (`Domain`, `Dto`, `DomainServices`, `Persistence`, `QueryServices`, `Infrastructure`, `FluentMigrations`) with Linq2Db mappings and migration services.
- **Engine (Vault parity):** **`EntityScopeBase`**; **`UserAuthorization`** / **`ApiKeyAuthorization`** aggregates; split **`UserEntityScope`** / **`ApiKeyEntityScope`**; **`IdentityDomainService`** / **`ApiKeyDomainService`** refactor aligned with MaksIT.Vault.
- **`ExpressionCompose`** for composing nested Linq2Db predicates.
- **`IRegistrationCacheDomainService`** / **`RegistrationCacheDomainService`**, **`RegistrationCachePayloadDocument`**, and **`RegistrationCachePayloadJsonTests`**.
- **`IAcmeSessionPersistenceService`**, **`AcmeSessionPersistenceServiceLinq2Db`**, and **`AcmeSessionPayloadMapper`** for PostgreSQL-backed ACME **`State`**.
- **`PostgresStartupWait`** — waits for PostgreSQL and retries FluentMigrator **`MigrateUp`** on transient startup failures.
- **API / RBAC:** **`GetActingJwtTokenData`** maps API keys to a synthetic **`JwtTokenData`** principal; **`RbacHelpers.EnsureActorMayAssignGlobalAdmin`** / **`EnsureActorMayPatchGlobalAdminFlag`**.
- **HA runtime coordination:** DB-backed HTTP-01 challenge persistence and runtime leases (**`acme_http_challenges`**, **`app_runtime_leases`**, **`acme_sessions`**, **`terms_of_service_cache`**); coordinated bootstrap and renewal execution.
- **Kubernetes:** Per-component Helm **`replicaCount`**, PodDisruptionBudget support, health endpoints (**`/health/live`**, **`/health/ready`**), optional **`kubernetesUpstreamHosts`** for in-cluster YARP upstreams.
- **LetsEncrypt:** Per-host ACME rate-limit cooldown on **`RegistrationCache`**; **`AcmeProblemKind`** enumeration; in-memory **`AcmeSessionStore`** (later superseded by PostgreSQL persistence); partial **`LetsEncryptService`** files; **`State.TryGetAccountKey`**; **`LetsEncrypt.Tests`** for retry parsing and cooldown JSON.
- **Frontend:** Users/API Keys pages and forms; identity/API-key UX with list/filter/paging (later migrated to **`webui-*`** packages).
- **Test suite:** **`MaksIT.CertsUI.Tests`** and **`MaksIT.CertsUI.Engine.Tests`** with Postgres/WebAPI fixtures.
- **Release tooling:** **`DotNetDockerPush`** per-image **`versionEnvFiles`** (temporary **`VITE_APP_VERSION`** rewrite) and optional per-image **`contextPath`** (upstreamed to **maksit-repoutils** 1.0.11).
- **Helm / config:** **`certsEngineConfiguration.autoSyncSchema`** (default **`true`**) for add-only column sync on startup.

### Changed

- **Identity / API key controllers:** Use **`GetActingJwtTokenData`** instead of JWT-only **`GetJwtTokenData`** for user and API-key CRUD/search.
- **IdentityService** / **ApiKeyService:** Build predicates and call **`Count`** + **`Search`** on query services; thin-search wiring (Pattern B).
- **Web UI:** Migrated to **`@maks-it.com/webui-*`** packages; **`createWebUiHttpClient`**; **`apiRoutes.ts`**; pages under **`src/pages/`**; **`webUi/dataSources.ts`**; models reorganized under domain folders aligned with **`webui-contracts`**. Earlier step: **`axiosConfig`** helpers return **`{ payload, status, ok }`**.
- **ACME sessions:** Let's Encrypt client **`State`** persisted in PostgreSQL **`acme_sessions`** so any replica can continue after load balancing.
- **LetsEncrypt / `HttpClient`:** **`ConfigureClient`** uses absolute ACME directory URL instead of assigning **`BaseAddress`** on the shared client.
- **`CertsFlowDomainService`:** **`PurgeStaleHttpChallengesAsync`** (HTTP-01 cleanup); **`AutoRenewal`** calls it before renewal work; skips hostnames in ACME cooldown window.
- **`LetsEncryptService`:** Uses **`IAcmeSessionPersistenceService`**; dropped **`Newtonsoft.Json`** from Engine (STJ-only JSON paths).
- **`CacheService`:** Thin façade over **`IRegistrationCacheDomainService`**.
- **Bootstrap / renewal:** **`InitializationHostedService`** acquires **`certs-ui-bootstrap`** (**`RuntimeLeaseNames.BootstrapCoordinator`**) for empty-database admin creation, then releases. **`AutoRenewal`** acquires **`certs-ui-renewal-sweep`** (**`RuntimeLeaseNames.RenewalSweep`**) per sweep. Lease name **`certs-ui-primary`** replaced by bootstrap and renewal sweep constants.
- **HA / ToS cache:** Terms of Service PDF caching moved from pod filesystem to PostgreSQL **`terms_of_service_cache`** with TTL/HTTP validators.
- **FluentMigrator:** Standard **`VersionInfo`** table and columns; **`.ScanIn(…).For.All()`** discovery; migrations run in **`Program.cs`** after **`Build()`** before hosted services; logged host/database and migration count; coordination DDL repair after **`MigrateUp`** when tables missing despite applied version.
- **Schema sync:** **`AutoSyncSchema`** add-only (**`ADD COLUMN IF NOT EXISTS`**; no DROP); desired map includes **`users.IsActive`**, **`TwoFactorSharedKey`**.
- **Docker Compose / Helm:** YARP upstream env vars for in-cluster vs Compose hostnames; **`components.server.service.sessionAffinity`** defaults to **`false`** (stateless LB); **`certsClientRuntime.apiUrl`** defaults to **`/api`**; optional **`preStop`** sleep and **`terminationGracePeriodSeconds`** for rolling updates.
- **Container image:** **`MaksIT.CertsUI`** Dockerfile installs **`libgssapi-krb5-2`** for Npgsql GSS support on slim images.
- **Namespace and solution layout:** Standardized around **`MaksIT.CertsUI*`** host/engine split; ACME contracts moved from legacy **`Entities`/`Models`** into **`Domain`** / **`Dto`**.
- **LetsEncrypt:** Broader nullable annotations; **`CachedHostname`** primary constructor; certificate loading via **`X509Certificate2.CreateFromPem`** / **`X509CertificateLoader.LoadCertificate`**.
- **Integration tests:** **`InMemoryUserStore`**, **`CacheServiceTests`**, **`CertsFlowServiceTests`**, **`ApiKeyQueryServiceIntegrationTests`**, **`AccountServicePatchAccountIntegrationTests`** aligned with new ports.
- **README:** Architecture references, HA guidance, E2E instructions for dotnet test and PowerShell scenarios.

### Fixed

- **Startup / HA:** Bootstrap lease no longer blocks extra replicas when users already exist; cooperative cancel on host shutdown; **`CoordinationTableProvisioner`** with explicit **`public.*`** DDL; **`RuntimeLeaseServiceNpgsql`** uses **`public.app_runtime_leases`**; post-migrate verification for coordination tables.
- **FluentMigrator:** Empty connection string no longer puts runner in connectionless/preview mode; throw when engine connection string missing; maintenance DB bootstrap warns instead of failing when role cannot **`CREATE DATABASE`**.
- **Identity / PostgreSQL:** **`users.JwtTokensJson`** column dropped; new inserts no longer hit **`23502`**; token rows normalized into **`jwt_tokens`**.
- **Helm / reverseproxy:** YARP upstreams no longer default to unresolvable Compose hostnames in Kubernetes when **`kubernetesUpstreamHosts`** is enabled.
- **LetsEncrypt:** **`RevokeCertificate`** fails correctly on non-success; disposes HTTP response on success; **`NewOrder`** logs authorization status; **`TryGetCachedCertificate`** returns **`false`** when private key blob missing.
- **`AccountService.PatchAccountAsync`:** Returns account built from cache after reload, not stale in-memory instance.
- **WebUI Terms of Service:** PDF worker loaded from Vite-bundled asset (**`pdf.worker.min.mjs?url`**) for dev and production.

### Removed

- **`CertsFlowPrimaryReplica`**, **`PrimaryReplicaRequiredObjectResult`**, **`CertsFlowResultExtensions`** / **`ToCertsFlowActionResult`**; **`CertsFlowController`** uses **`ToActionResult()`**.
- **Web UI (local):** **`DataTable`**, **`FormLayout`**, **`Layout`**, **`LazyLoadTable`**, editor components, **`Toast`**, **`Offcanvas`**, **`useFormState`**, **`localStorage/identity`**, legacy **`functions/`** / **`models/`** trees superseded by **`webui-*`** packages.
- **Web UI:** Primary-replica **503** auto-retry in **`axiosConfig.ts`**.
- **Configuration / Helm:** **`AcmeFolder`**, **`DataFolder`**, default **acme**/**data** PVC mounts; **`AddMemoryCache()`** host registration (unused).
- **Legacy engine layout:** Obsolete top-level engine files, old **`.vscode`** project files.

### Upgrade notes (from 3.3.4)

**3.3.4** is the last published release. **3.5.0** uses a new host, PostgreSQL-backed registration cache, and FluentMigrator schema — there is no in-place migration of Let's Encrypt account material from **3.3.4**. Use backup → install → restore:

1. **On 3.3.4 (before upgrade):** Sign in to the Web UI, open **Utilities**, and **Download cache files** (full registration-cache ZIP). Store the file safely — it contains your Let's Encrypt account keys and cached certificate data. Optionally export Postman/API backups of any other settings you rely on.
2. **Prepare 3.5.0:** Provision a **new empty PostgreSQL database** (or drop and recreate the engine database). Update Helm/secrets: rename **`certsUIEngineConfiguration`** → **`certsEngineConfiguration`**, set **`ConnectionString`**, and remove legacy **`acme`** / **`data`** bind mounts from **`docker-compose.override.yml`** if present.
3. **Deploy 3.5.0:** Install the new server, client, and reverse-proxy images (pin tags to **`3.5.0`**, not **`latest`**). On first start, FluentMigrator creates the schema; bootstrap creates the default admin when the database has no users.
4. **Restore accounts:** Sign in on **3.5.0**, open **Utilities**, and **Upload cache files** with the ZIP from step 1. Verify accounts and certificates in the UI before decommissioning **3.3.4**.
5. **Re-create operators:** Users, API keys, and JWT sessions from **3.3.4** are not migrated automatically — recreate users/API keys in **3.5.0** as needed.

**Also check when upgrading:**

- **Operations:** Retarget alerts from lease **`certs-ui-primary`** to **`certs-ui-bootstrap`** and **`certs-ui-renewal-sweep`**.
- **Web UI build:** Configure npm auth for **`@maks-it.com/*`** (see **`src/MaksIT.WebUI/.npmrc`**) when building the client image locally.
- **E2E:** **`dotnet test`** on **`MaksIT.CertsUI.Client.Tests`** with **`Category=E2E`**; set **`CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES=2`** for HA (PowerShell E2E defaults to **1** for Docker Compose).
- **Repo utils:** **`utils/`** refreshed from **maksit-repoutils** 1.0.14 (`engines/`, `plugins/`, `modules/`, `tools/`). Run **`utils\Invoke-ReleasePackage.bat`** for release; **`utils\Invoke-TestEngine.bat`** for tests and coverage badges; refresh via **`utils\Update-RepoUtils.bat`** (set **`dryRun`: false** in **`utils/tools/Update-RepoUtils/scriptSettings.json`**).

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
