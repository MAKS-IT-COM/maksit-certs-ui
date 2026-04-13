# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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



