# User (JWT) and API Key RBAC

This document explains how **role-based access control** differs when the caller is a **logged-in user** (JWT) versus an **API key**, how those principals are loaded, and where the rules live in code.

**Audience:** Backend developers, security reviewers, and anyone automating against the CertsUI API (WebUI, PowerShell module, `MaksIT.CertsUI.Client`).

**Related:** [Login and refresh token architecture](./LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md) (how JWTs are issued). [RBAC reference](./RBAC_REFERENCE.md) has scope flags and permission matrices. **This** file covers JWT vs API key principals, routes, `GetActingJwtTokenData`, and code references.

---

## 1. Two authentication mechanisms

| Mechanism | Header | Typical use |
|-----------|--------|-------------|
| **User (JWT)** | `Authorization: Bearer <access_token>` | Browser (WebUI), interactive tools, services that can refresh tokens |
| **API key** | `X-API-KEY: <secret>` | Scripts, CI, PowerShell (`Connect-CertsUI`), `MaksIT.CertsUI.Client`; long-lived secret shown once at creation |

**Request resolution:** `CertsUIAuthorizationFilter` (`src/MaksIT.CertsUI/Authorization/Filters/CertsUIAuthorizationFilter.cs`) checks **`X-API-KEY` first**. If that header is absent or invalid, it falls back to **Bearer JWT**. If neither yields a valid principal, the request gets **403 Forbidden** (`Authorization required`).

**Unlike MaksIT.Vault:** Vault uses **`JwtAuthorizationFilter`** on `/api/identity/...` and `/api/apikey/...` (JWT only) and **`VaultAuthorizationFilter`** on `/api/vault/...` (JWT or API key). In **CertsUI**, every protected controller action listed below uses **`CertsUIAuthorizationFilter`** — the same dual path for **identity**, **API keys**, **accounts**, **ACME flow**, and **cache**.

| Route family | Filter | JWT | `X-API-KEY` |
|--------------|--------|-----|-------------|
| `/api/identity/...` (except login / refresh) | `CertsUIAuthorizationFilter` | Yes | Yes |
| `/api/apikey/...` | `CertsUIAuthorizationFilter` | Yes | Yes |
| `/api/account/...`, `/api/certs/...`, `/api/cache/...`, `/api/agent/...`, `/api/debug/...` | `CertsUIAuthorizationFilter` | Yes | Yes |

**Exceptions (no JWT / API key on the action):**

- `POST /api/identity/login` and `POST /api/identity/refresh` — anonymous.
- `GET /.well-known/acme-challenge/{token}` (`WellKnownController`) — no authorization filter; public HTTP-01 challenge responses (see [REVERSE_PROXY_ROUTING.md](./REVERSE_PROXY_ROUTING.md)).

---

## 2. Two principal types (what RBAC sees)

After authentication, the filter stores **`CertsUIAuthorizationData`** on `HttpContext` (`HttpContextValue.CertsUIAuthorizationData`).

| Principal | Type | Where it comes from |
|-----------|------|---------------------|
| **User** | `JwtTokenData` | Validated JWT; includes `UserId`, `IsGlobalAdmin`, and **entity scopes** from the user record |
| **API key** | `ApiKeyData` | Key row by secret + **`ReadApiKeyAuthorization`**; includes `ApiKeyId`, `IsGlobalAdmin` **for that key**, and **entity scopes** stored for the key |

Services that manage users or API keys do not take `CertsUIAuthorizationData` directly on every method. Controllers call **`GetActingJwtTokenData()`**, which:

- Returns the real **`JwtTokenData`** for Bearer sessions.
- Maps an API key to a **synthetic** `JwtTokenData` (`UserId = Guid.Empty`, `Username = apikey:{id}`, scopes and `IsGlobalAdmin` from the key).

So identity and API-key **admin** code paths are written against **`JwtTokenData`**, but the acting principal may be a **user** or an **API key** after mapping.

### 2.1 Global administrator: user vs key (easy to confuse)

- **`JwtTokenData.IsGlobalAdmin`** — the **signed-in user** is a global administrator (or the key was mapped with this flag).
- **`ApiKeyData.IsGlobalAdmin`** — **this API key** was created (or patched) with the global-administrator flag.

These are **independent**. A global-admin **user** who creates an API key does **not** automatically create a global-admin key unless the create request sets **`IsGlobalAdmin: true`** on the key (UI: separate checkbox). Automation that must bypass scoped limits on **accounts / ACME** needs a **global-admin API key**, not merely a key created by an admin user.

**Who may set `IsGlobalAdmin` on a key?** Only a principal that is already a **global administrator** after `GetActingJwtTokenData()` may **create** or **patch** a user or API key with `IsGlobalAdmin` enabled (`RbacHelpers.EnsureActorMayAssignGlobalAdmin` / `EnsureActorMayPatchGlobalAdminFlag` in `IdentityService` / `ApiKeyService`).

### 2.2 Loading API key authorization

For API key requests, `IsGlobalAdmin` and scopes come from persisted authorization data. If **`ReadApiKeyAuthorization`** fails after the key itself was found, the filter **propagates that failure** (it does **not** continue with `IsGlobalAdmin: false`), so misconfiguration or storage errors are not silently downgraded to “non-admin key” behavior.

Expired keys (`ExpiresAt <= UtcNow`) are rejected in the filter before authorization is loaded.

---

## 3. Shared RBAC helpers (`ServiceBase`)

App services inherit from `ServiceBase` (`src/MaksIT.CertsUI/Abstractions/Services/ServiceBase.cs`), which implements:

- **`RBACWrapper`** — If the request used JWT, runs **`RBACWrapperJwtToken`** with `userRules`; if API key, runs **`RBACWrapperApiKey`** with `apiKeyRules`.
- **`RBACWrapperJwtToken` / `RBACWrapperApiKey`** — If `IsGlobalAdmin` is **true** for that principal, return **success immediately** (full access for that wrapper). Otherwise run the supplied rules delegate, or **403** if no rules were supplied.

So: **`userRules` / `apiKeyRules` = null** means **only global administrators** (JWT or API key, respectively) pass. Non-admins always get **forbidden** with no per-scope check.

Variants **`RBACWrapper<T>`** / **`RBACWrapperJwtToken<T>`** / **`RBACWrapperApiKey<T>`** follow the same pattern but carry a resource through the rules.

**Identity / API key services** use **`RBACWrapperJwtToken`** only (after `GetActingJwtTokenData()`), including when the caller used an API key.

---

## 4. Example: accounts and ACME (`AccountService`, `CertsFlowService`)

Illustrates **authentication without scoped RBAC** today.

| Action | RBAC pattern | Non–global-admin JWT | Non–global-admin API key |
|--------|----------------|----------------------|---------------------------|
| **List / read / create / patch / delete account** | `RBACWrapper(..., _ => Ok(), _ => Ok())` | Allowed if authenticated | Allowed if authenticated |
| **Certs flow steps** (`CertsFlowService`) | Same open rules | Allowed if authenticated | Allowed if authenticated |
| **Registration cache** (`CacheService`) | Same | Allowed if authenticated | Allowed if authenticated |

There is **no** organization- or account-level scope check on certificate operations yet. **Do not** treat API keys as least-privilege for ACME unless you issue **non-global** keys and accept that they can still drive any account operation once authenticated. Scoped **Identity** / **ApiKey** flags apply to **user and key administration** (see [RBAC reference](./RBAC_REFERENCE.md) §4–§6 and §5.1 for search vs CRUD).

---

## 5. Identity and API key administration (`GetActingJwtTokenData`)

Operations in **`IIdentityService`** / **`IApiKeyService`** and their controllers take **`JwtTokenData`** from **`GetActingJwtTokenData()`** — so **both** Bearer users and **`X-API-KEY`** callers can hit `/api/identity/...` and `/api/apikey/...` when the key’s scopes (or global admin) allow it.

Summary (matrices and enforcement detail: [RBAC reference](./RBAC_REFERENCE.md)):

| Operation | Global-admin actor | Non–global-admin actor |
|-----------|-------------------|-------------------------|
| **Search users / keys / scopes** | Full list (plus request filters) | Results limited to targets whose **organization** ids are covered by actor **`Identity`** / **`ApiKey`** **`Read`** scopes (enforced in query predicates) |
| **Read / patch / delete by id** | Full access | **Intended:** same org coverage as Vault-style identity rules; **current code:** non-admin CRUD wrappers return success without scope checks — see [RBAC reference](./RBAC_REFERENCE.md) §4.1 |
| **Create** | Any user/key; may set **`IsGlobalAdmin`** on new principal | **Intended:** `Create` on scopes for all orgs in request; **current code:** non-admin create wrapper allows any authenticated actor |
| **Patch `IsGlobalAdmin`** | Allowed | **Forbidden** (`RbacHelpers`) |

E2E regression tests in `src/e2e-tests/scenarios/Scenario-05-IdentityConfigurations.ps1` assert non-global users cannot create global-admin users or keys.

---

## 6. Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| **403** `Authorization required` | Neither valid `X-API-KEY` nor Bearer JWT. |
| **403** `Invalid API Key` / `API Key expired` | Wrong secret, revoked row, or past `ExpiresAt`. |
| **403** `User does not have access to resource.` | JWT user is not global-admin and the service used `RBACWrapper` with **null** rules (not used on account/certs today). |
| **403** `ApiKey does not have access to resource.` | Same for API key + null rules. |
| **403** on **`IsGlobalAdmin`** patch/create | Acting principal is not global-admin (`RbacHelpers`). |
| User/key **visible in UI search** but **403 on GET by id** | Unlikely today (CRUD is permissive for non-admins); if scope checks are added to match Vault, search and GET will align — until then, prefer **search** for scoped operators. |
| Automation works in UI but **403** with API key | Key lacks **`IsGlobalAdmin`** or scopes; UI user may be global-admin while the key is not. |
| **403** / unexpected status right after API key validation | Authorization row read failed; check logs and DB for that `ApiKeyId`. |

---

## 7. Code map

| Topic | Location |
|-------|----------|
| API key vs JWT resolution | `MaksIT.CertsUI/Authorization/Filters/CertsUIAuthorizationFilter.cs` |
| Acting principal for identity/API key | `MaksIT.CertsUI/Authorization/Extensions/HttpContextExtension.cs` (`GetActingJwtTokenData`, `ToActingJwtTokenData`) |
| Identity (dual auth) | `MaksIT.CertsUI/Controllers/IdentityController.cs` |
| API keys (dual auth) | `MaksIT.CertsUI/Controllers/APIKeyController.cs` |
| Accounts / certs / cache (dual auth) | `AccountController`, `CertsFlowController`, `CacheController` |
| HTTP-01 (no RBAC) | `MaksIT.CertsUI/Controllers/WellKnownController.cs` |
| `RBACWrapper*` helpers | `MaksIT.CertsUI/Abstractions/Services/ServiceBase.cs` |
| Scope helpers | `MaksIT.CertsUI/Services/Helpers/RbacHelpers.cs` |
| Identity RBAC | `MaksIT.CertsUI/Services/IdentityService.cs` |
| API key RBAC | `MaksIT.CertsUI/Services/ApiKeyService.cs` |
| Open certs RBAC | `AccountService`, `CertsFlowService`, `CacheService`, `AgentService` |
| Principal types | `MaksIT.CertsUI/Authorization/` (`JwtTokenData`, `ApiKeyData`, `CertsUIAuthorizationData`, `IdentityScopeData`) |
| Scope enums | `MaksIT.CertsUI.Engine/ScopeEntityType.cs`, `ScopePermission.cs` |

---

*Last updated: May 2026*
