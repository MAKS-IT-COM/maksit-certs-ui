# Login and Refresh Token Architecture

This document describes how authentication (login), token refresh, and logout work across the **MaksIT.CertsUI** WebAPI and WebUI.

**Audience:** Backend (C# / ASP.NET) and Frontend (TypeScript / React) developers.

**Related:** This repo’s WebUI identity layer is **aligned** with **MaksIT-Vault** [`LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md`](../../maksit-vault/assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md) when both projects sit side by side under the same parent folder; otherwise open that path in your Vault clone. Vault documents **2FA**, **API-key RBAC**, and a **database-backed** user store. **MaksIT.CertsUI** uses a **settings-backed** user list, **no 2FA** on the backend (optional `LoginRequest` fields exist for shared models but are ignored; the WebUI 2FA inputs are commented out), and **no Vault-style ACL product surface**—JWT may still carry role/ACL claims for shared **MaksIT.Core** JWT helpers, but Certs is not an ACL administration app.

---

## 1. Overview

- **Access token:** Short-lived JWT used in the `Authorization: Bearer <token>` header for API calls.
- **Refresh token:** Opaque string stored with the user in settings; used to obtain a new access token (and optionally a new refresh token) when the access token expires.
- **Login** returns both tokens; the client stores them and uses the access token until it expires, then calls **refresh** with the refresh token.
- **Logout** revokes the current session (or all sessions) on the server and clears tokens on the client. On Certs, the logout HTTP endpoint does **not** require a `Authorization` header; it identifies the session via the **access token** in the JSON body (see §3.4).

---

## 2. Token model

### 2.1 Backend: `JwtToken` (domain)

**Location:** `src/MaksIT.Webapi/Domain/JwtToken.cs`

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Token identifier. |
| `Token` | string | The JWT access token string. |
| `TokenType` | string | Typically `"Bearer"`. |
| `IssuedAt` | DateTime | When the token was issued (UTC). |
| `ExpiresAt` | DateTime | When the access token expires. |
| `RefreshToken` | string | Opaque refresh token. |
| `RefreshTokenExpiresAt` | DateTime | When the refresh token expires. |
| `IsRevoked` | bool | If true, token is treated as unusable (revoked entries are removed when resolving refresh). |

- A **User** holds a list of `JwtToken` instances (multiple devices/sessions).
- New tokens are **upserted** via `User.UpsertJwtToken`.

### 2.2 API response: `LoginResponse`

**Location:** `src/MaksIT.Models/LetsEncryptServer/Identity/Login/LoginResponse.cs`

Returned by **login** and **refresh**:

- `TokenType` (e.g. `"Bearer"`)
- `Token` (access JWT)
- `ExpiresAt` (access token expiry)
- `RefreshToken`
- `RefreshTokenExpiresAt`

There is **no** `Username` field on this model; the WebUI derives display name from **JWT claims** when hydrating identity (`identitySlice`).

---

## 3. Backend flow

### 3.1 Layers

| Layer | Component | Responsibility |
|-------|------------|----------------|
| API | `IdentityController` | Login, refresh, logout, and authenticated `PATCH` user. |
| Service | `IdentityService` | Loads/saves **settings**, validates credentials, issues JWTs via `JwtGenerator`, maps domain `JwtToken` to `LoginResponse`. |
| Domain | `User` | Password validation, JWT token list (upsert/remove/revoke). |
| Persistence | `ISettingsService` | Users and tokens persist in application settings (not a separate identity database). |

### 3.2 Login

**Endpoint:** `POST /api/identity/login`  
**Controller:** `IdentityController.Login` → `IdentityService.LoginAsync`

1. **Load** settings via `ISettingsService.LoadAsync`.
2. **Resolve user** by username (`GetUserByName`).
3. **Validate password** (`User.ValidatePassword`) with configured **pepper**.
4. **Optional 2FA fields** on `LoginRequest` are **not** validated by Certs—ignored if sent.
5. **Generate** access JWT via `JwtGenerator.TryGenerateToken` (secret, issuer, audience, expiration from config).
6. **Generate** opaque refresh token and build a domain `JwtToken` with access + refresh expiry (`RefreshExpiration` from config).
7. **Upsert** token on user, **SetLastLogin**, persist settings.
8. Return `LoginResponse` (no username field; client uses JWT claims).

**Request body (`LoginRequest`):** `username`, `password`, optional unused `twoFactorCode` / `twoFactorRecoveryCode` (shared model shape only).

### 3.3 Refresh

**Endpoint:** `POST /api/identity/refresh`  
**Controller:** `IdentityController.RefreshToken` → `IdentityService.RefreshTokenAsync`

**Request body (`RefreshTokenRequest`):** `refreshToken` only (`src/MaksIT.Models/LetsEncryptServer/Identity/Login/RefreshTokenRequest.cs`). The WebUI may send a `force` flag for parity with shared thunk code; the Certs API model does **not** include it (extra properties are typically ignored by the serializer).

1. **Load** settings.
2. **Resolve user** by refresh token (`GetByRefreshToken`).
3. **Remove** revoked JWT rows (`RemoveRevokedJwtTokens`).
4. **Find** the token where `RefreshToken` matches.
5. **Unauthorized** if not found → e.g. “Invalid refresh token.”
6. **If the access token is still valid** (`UtcNow <= token.ExpiresAt`): update last login, save settings, return the **same** `LoginResponse` (no new JWT). There is **no** server-side `force` refresh path like Vault.
7. **If access expired** but refresh is still valid (`UtcNow <= RefreshTokenExpiresAt`): issue a **new** access JWT + new refresh token, upsert token, save, return new `LoginResponse`.
8. **If refresh is expired**: remove that token record, return **401** “Refresh token has expired.”

### 3.4 Logout

**Endpoint:** `POST /api/identity/logout` (**no** `JwtAuthorizationFilter` on this action)  
**Controller:** `IdentityController.Logout` → `IdentityService.Logout`

1. **Load** settings.
2. **Resolve user** by **access JWT string** in the body (`LogoutRequest.Token`) via `GetByJwtToken`.
3. If found: **`LogoutFromAllDevices`** → `RevokeAllJwtTokens()`; else → `RemoveJwtToken(token)` for the current session.
4. Persist settings if the user was updated.
5. Return success (implementation may still return OK if the token was unknown—clients should clear local state regardless).

**Request body (`LogoutRequest`):** `token` (access JWT), `logoutFromAllDevices`.

The WebUI sends the current access token from stored identity; it does not rely on a Bearer header for this route.

---

## 4. Frontend flow

### 4.1 Identity state and storage

**Redux:** `identitySlice` (`src/MaksIT.WebUI/src/redux/slices/identitySlice.ts`)

- **State:** `identity: Identity | null`, `hydrated: boolean`, `status`, `showUserOffcanvas`.
- **Persistence:** Login/refresh responses are written to **localStorage** via `writeIdentity`; on load, `setIdentityFromLocalStorage` reads and hydrates state and **enriches** from JWT claims (`userId`, `username`, `roles`, `isGlobalAdmin`, `acls` when those claims exist).

**Identity type** extends `LoginResponse` with client-side fields: `userId`, `username`, `roles`, `isGlobalAdmin`, `acls`.

### 4.2 Login UI

**Component:** `LoginScreen` (`src/MaksIT.WebUI/src/components/LoginScreen.tsx`)

- Form: username and password; **2FA** inputs are **commented out** until the backend supports them.
- On submit: `dispatch(login(loginRequest))`.
- On successful login, `identitySlice` stores the response in state and localStorage; `LoginScreen` redirects when identity is present and refresh token is not expired.

### 4.3 Route protection

**Component:** `Authorization` (`src/MaksIT.WebUI/src/components/Authorization.tsx`)

- Wraps protected routes.
- On mount, if not hydrated, dispatches `setIdentityFromLocalStorage()`.
- **When hydrated:** if `identity` is missing or `refreshTokenExpiresAt` is in the past, redirects to `/login` (with `state.from` for return URL).
- Renders children only when hydrated and refresh token is not expired.

**Refresh token** expiry is what forces re-login; the **access** token may expire while refresh is still valid (handled by axios).

### 4.4 Axios: token attachment and refresh

**File:** `src/MaksIT.WebUI/src/axiosConfig.ts`

- **Excluded URLs** (no Bearer token, no refresh loop): login and refresh routes (`GetApiRoute(ApiRoutes.identityLogin).route`, `GetApiRoute(ApiRoutes.identityRefresh).route`).
- **Request interceptor:** If access token is expired but refresh is still valid by client clock, await a single shared `refreshJwt()`; on success attach new `Authorization`; on failure dispatch `clearIdentity()` and reject (do not send protected calls with an expired access token).
- **Response interceptor:** On **401**, optionally retry once after refresh when refresh is still valid; on refresh failure, `clearIdentity()`.
- **Serialization:** `isRefreshing` / `refreshPromise` so concurrent requests share one refresh.

### 4.5 Async thunks and clearIdentity

- **`login`:** POST login; on success writes identity and enriches from JWT.
- **`refreshJwt(force?)`:** POST refresh with `refreshToken` (and optional `force` in body for shared code paths; server ignores `force` on Certs). On failure, identity cleared.
- **`logout`:** POST logout with access token in body, then clear state/localStorage.
- **`clearIdentity()`:** Clears Redux and localStorage without calling logout API (used when refresh fails).

---

## 5. API summary

| Method | Endpoint | Bearer required | Purpose |
|--------|----------|-----------------|--------|
| POST | `/api/identity/login` | No | Login; returns access + refresh token. |
| POST | `/api/identity/refresh` | No | Exchange refresh token for same or new tokens. |
| POST | `/api/identity/logout` | No | Revoke session(s) using access token **in body**. |

Other identity routes (e.g. `PATCH /api/identity/user/{id}`) use `JwtAuthorizationFilter` and require a valid JWT.

Base route: `api/identity` (`IdentityController`, `AppMap`).

---

## 6. Sequence overview

**Login:** User submits credentials → POST `/api/identity/login` → settings updated with new `JwtToken` → WebUI stores identity → redirect into app.

**Authenticated request (access token valid):** Interceptor adds `Authorization: Bearer` → API validates JWT.

**Access expired, refresh valid:** Interceptor awaits `refreshJwt()` → POST `/api/identity/refresh` → updated identity → original request retried with new token.

**401 on protected request:** Response interceptor attempts refresh; if refresh returns 401, `clearIdentity()` and redirect to `/login`.

**Logout:** POST `/api/identity/logout` with body `{ token, logoutFromAllDevices }` → server removes token(s) from settings → client clears storage.

Replace illustrative “organizations” examples in Vault with Certs resources (e.g. **accounts**, **certificate flows**)—the **mechanism** is the same: no protected API should run after refresh has failed without clearing identity.

---

## 7. Security notes

- **Passwords** use salt + server-side **pepper**; not stored in plain text.
- **Refresh tokens** are stored per user in settings; expiry and invalidation are enforced in `IdentityService.RefreshTokenAsync`.
- **2FA** is **not** implemented on the Certs WebAPI; do not enable the WebUI 2FA fields until backend support exists.
- **Login/refresh** do not require Bearer; other protected controllers use `JwtAuthorizationFilter`.
- Frontend keeps **one** identity in localStorage; refresh is serialized to avoid duplicate refresh storms.

---

## 8. Key files reference

| Area | File |
|------|------|
| Domain – User | `src/MaksIT.Webapi/Domain/User.cs` |
| Domain – JwtToken | `src/MaksIT.Webapi/Domain/JwtToken.cs` |
| API service | `src/MaksIT.Webapi/Services/IdentityService.cs` |
| API controller | `src/MaksIT.Webapi/Controllers/IdentityController.cs` |
| API models | `src/MaksIT.Models/LetsEncryptServer/Identity/Login/`, `.../Logout/` |
| Frontend – state | `src/MaksIT.WebUI/src/redux/slices/identitySlice.ts` |
| Frontend – HTTP | `src/MaksIT.WebUI/src/axiosConfig.ts` |
| Frontend – routes | `src/MaksIT.WebUI/src/components/Authorization.tsx` |
| Frontend – login UI | `src/MaksIT.WebUI/src/components/LoginScreen.tsx` |
| Frontend – API map | `src/MaksIT.WebUI/src/AppMap.tsx` |
