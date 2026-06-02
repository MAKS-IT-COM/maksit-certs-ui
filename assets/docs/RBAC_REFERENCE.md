# RBAC reference — scopes and permission matrices

This document is the **policy reference** for **MaksIT.CertsUI**: scope flags, shorthand roles, and **who may perform which actions** on Identity, API-key management, and ACME/certificate endpoints.

**Read first:** [User vs API key RBAC](./USER_AND_API_KEY_RBAC.md) — how JWT and `X-API-KEY` are resolved, `CertsUIAuthorizationFilter` on all protected routes, `GetActingJwtTokenData`, global admin on user vs key, and troubleshooting.

---

## 1. Scope model

Storage and wire shape follow **MaksIT.Vault** (organization id + flags on `user_entity_scopes` / `api_key_entity_scopes`). CertsUI uses a **smaller** scope type enum than Vault.

Each **scope** is:

| Field | Meaning |
|-------|---------|
| **`EntityId`** | **Organization** id this grant applies to (same as Vault org scopes) |
| **`EntityType`** | **`Identity`** or **`ApiKey`** — which *administration* surface the flags govern (not “organization” as a type) |
| **`Scope`** | `ScopePermission` flags bitmask |

### 1.1 Permission flags (`ScopePermission`)

| Flag | Value | Typical use |
|------|-------|-------------|
| `Read` | `1 << 0` | List/search and read in that org for the given `EntityType` |
| `Write` | `1 << 1` | Patch targets tied to that org |
| `Delete` | `1 << 2` | Delete targets tied to that org |
| `Create` | `1 << 3` | Create principals whose requested scopes include that org |

**Check:** `RbacHelpers.Has(granted, required)` — all bits in `required` must be set.

**Search/list:** RBAC filters results in **`IdentityService`** / **`ApiKeyService`** query predicates first; request/UI filters apply on top. The acting user is excluded from user search.

A principal may hold many scopes; effective access is the **union** of matching rows.

---

## 2. Shorthand columns (matrices below)

Tables use **Admin** (global, no scopes required) and two **scoped** columns keyed by organization **O**:

| Column | Meaning |
|--------|---------|
| **Identity manager (org O)** | `EntityType = Identity`, `EntityId = O`, with `Read` + `Write` + `Create` / `Delete` as required by the action |
| **Identity reader (org O)** | `EntityType = Identity`, `EntityId = O`, with `Read` only |

For API-key administration, replace **Identity** with **ApiKey** in the column names (**API key manager** / **API key reader** on org **O**).

CertsUI has **no** Vault-style Application or Secret scopes; certificate work is not scoped per org in the database today.

---

## 3. Global administrator

A **global administrator** is a JWT user with **`IsGlobalAdmin`** or an API key with **`IsGlobalAdmin`** on the key row (independent flags — see [User vs API key RBAC](./USER_AND_API_KEY_RBAC.md) §2.1).

Global admins bypass scoped checks in `RBACWrapper*` helpers. Only global admins may **assign or remove** `IsGlobalAdmin` on users or keys (`RbacHelpers`).

---

## 4. Identity (users)

Target users carry **entity scopes** (org ids + flags). A scoped caller must cover **every organization** the target belongs to (same rule as Vault identity).

### 4.1 Enforced in code today (source of truth)

| Area | Global admin | Non–global-admin |
|------|--------------|------------------|
| **Search users / user scopes** | All (minus self in user search) | Only users whose org ids ⊆ actor’s org ids with **`Identity` + `Read`** |
| **Create / patch `IsGlobalAdmin`** | Allowed | **Forbidden** if request touches `IsGlobalAdmin` |
| **Read / create / patch / delete user by id** | Allowed | **RBAC wrapper returns success without org checks** — scoped enforcement **not** implemented in `ReadUserRBAC` / `CreateUserRBAC` / `PatchUserRBAC` / `DeleteUserRBAC` lambdas yet (XML comments describe the **intended** Vault-aligned rules) |

If shorthand tables below disagree with §4.1, **§4.1 wins** until CRUD wrappers are brought in line with Vault (`GetEntityIdsWithScope` checks like `maksit-vault` `IdentityService`).

### 4.2 Intended policy (target behavior; align CRUD with this)

| Action | Admin | Identity manager | Identity reader |
|--------|-------|------------------|-----------------|
| Read user | Yes (any) | Yes if `Read` on **Identity** for **all** target orgs | Yes if `Read` on **all** target orgs |
| Create user | Yes (any) | Yes if `Create` on **Identity** for **all** orgs in create request | No |
| Patch user | Yes (any) | Yes if `Write` on **all** target orgs (and touched orgs on scope patch) | Self only for profile fields; no role/org changes |
| Delete user | Yes (any, not self) | Yes if `Delete` on **all** target orgs; not self | No |

**Self:** Vault allows self read/patch with restrictions; Certs **search** excludes self; self-service rules for patch should match Vault when CRUD is completed.

---

## 5. ACME, accounts, cache, and agent

There is **no** per-organization scope on certificate accounts or ACME sessions in the current schema.

### 5.1 Enforced permissions (from code)

| Resource / area | Routes | Non–global-admin JWT | Non–global-admin API key |
|-----------------|--------|----------------------|---------------------------|
| Accounts | `/api/account/...` | Any authenticated principal | Any authenticated principal |
| ACME flow | `/api/certs/...` | Any authenticated principal | Any authenticated principal |
| Registration cache | `/api/cache/...` | Any authenticated principal | Any authenticated principal |
| Agent hello | `/api/agent/...` | Any authenticated principal | Any authenticated principal |
| HTTP-01 challenge | `GET /.well-known/acme-challenge/...` | **Anonymous** (no filter) | **Anonymous** |

All of the above use `RBACWrapper(..., _ => Result.Ok(), _ => Result.Ok())` except Well-Known. **Treat every valid API key like a full automation principal** for certificate operations until account-scoped RBAC exists.

### 5.2 Operational guidance

| Goal | Recommendation |
|------|----------------|
| Least privilege for **cert automation** | Issue keys only to trusted pipelines; rotate and expire keys; network-restrict the API. Do not rely on org scopes for ACME yet. |
| Least privilege for **user/key admin** | Use scoped **Identity** / **ApiKey** grants; use **search** for operators without global admin. |
| Full platform control | Global-admin JWT or global-admin API key. |

---

## 6. Managing API keys

Routes: `/api/apikey/...`. Callers use **`GetActingJwtTokenData()`** — JWT **or** API key (see [User vs API key RBAC](./USER_AND_API_KEY_RBAC.md) §5).

### 6.1 Enforced in code today

Same pattern as §4.1:

| Area | Global admin | Non–global-admin |
|------|--------------|------------------|
| **Search keys / key scopes** | All | Keys whose org ids ⊆ actor’s org ids with **`ApiKey` + `Read`** |
| **Patch `IsGlobalAdmin` on a key** | Allowed | **Forbidden** |
| **Read / create / patch / delete key by id** | Allowed | **No org scope check in RBAC lambdas yet** |

### 6.2 Intended policy (target behavior)

| Action | Admin | API key manager | API key reader |
|--------|-------|-----------------|---------------|
| Read API key | Yes (any) | Yes if `Read` on **ApiKey** for **all** key orgs | Yes if `Read` on **all** key orgs |
| Create API key | Yes (any); may set key `IsGlobalAdmin` | Yes if `Create` on **all** orgs in request; cannot set key `IsGlobalAdmin` | No |
| Patch API key | Yes (any) | Yes if `Write` on **all** key orgs | No |
| Delete API key | Yes (any) | Yes if `Delete` on **all** key orgs | No |

---

## 7. Calling the API with an API key

Use **`X-API-KEY`** on any protected route that uses **`CertsUIAuthorizationFilter`**. RBAC uses the **key’s** scopes and **`IsGlobalAdmin` on the key** — not the human operator’s JWT.

| Use case | Key type |
|----------|----------|
| PowerShell / `MaksIT.CertsUI.Client` automation (accounts, ACME) | Any valid key (prefer dedicated non-global keys only if you accept §5.1) |
| Create accounts, run `FullFlow`, cache | Authenticated key (global admin not required today) |
| Manage users or API keys via API | Key needs appropriate **Identity** / **ApiKey** scopes or global admin |
| Match WebUI “full admin” for certs + identity | **`IsGlobalAdmin: true`** on the key |

Details: [User vs API key RBAC](./USER_AND_API_KEY_RBAC.md) §1–2, [POWERSHELL_CLIENT_MODULE.md](./POWERSHELL_CLIENT_MODULE.md).

---

## 8. Comparison with MaksIT.Vault

| Topic | Vault | CertsUI |
|-------|-------|---------|
| Dual auth filter | `VaultAuthorizationFilter` on `/api/vault/...` only | `CertsUIAuthorizationFilter` on all protected API controllers |
| Identity / API key routes | JWT only | JWT **or** API key via `GetActingJwtTokenData()` |
| Resource scopes | Organization, Application, secrets | **None** for ACME; Identity/ApiKey admin only |
| Scoped CRUD on identity | Enforced in `IdentityService` | **Search enforced**; CRUD wrappers **stub** (May 2026) |

When porting fixes from Vault, copy **`GetEntityIdsWithScope`** patterns from `maksit-vault` `IdentityService` / `APIKeyService` into this repo’s RBAC lambdas.

---

*Last updated: May 2026*
