# CertsUI API key E2E (PowerShell)

End-to-end tests run against a **running** CertsUI deployment via [`MaksIT.CertsUI.Client.PowerShell`](../MaksIT.CertsUI.Client.PowerShell/) cmdlets. They are **not** part of CI (`utils/engines/test/scriptSettings.json`); run them manually after compose, Helm, or k3s deploy.

Cmdlet reference and `Import-Module` paths: [assets/docs/POWERSHELL_CLIENT_MODULE.md](../../assets/docs/POWERSHELL_CLIENT_MODULE.md).

Requires **latest PowerShell 7** with a **.NET 10** host (install from [PowerShell releases](https://github.com/PowerShell/PowerShell/releases)). **pwsh 7.4 / .NET 8** cannot load the `net10.0` module. Verify: `[System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription` → **.NET 10.x**.

**Docker Compose + YARP on `http://localhost:8080`:** use `http://localhost:8080` as the base URL (no `/api` suffix).

## Credentials

Uses **one** environment variable: **`CERTSUI_E2E_CREDENTIALS`** — UTF-8 text, Base64-encoded.

The script reads it with **`[Environment]::GetEnvironmentVariable`** in order **Process**, **User**, then **Machine** (same as Vault E2E).

After Base64 decode, the payload is a **single line**: `<baseUrl><US><apiKey>` where **`<US>`** is ASCII Unit Separator, U+001F (`[char]0x1F`).

Encode in pwsh:

```powershell
$us = [char]0x1F   # required on its own line before $b64
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("http://localhost:8080$us" + 'paste-raw-api-key-here'))
$b64
```

Persist for your **user** account:

```powershell
[Environment]::SetEnvironmentVariable('CERTSUI_E2E_CREDENTIALS', '<paste-base64-here>', 'User')
```

Or only for the **current process**:

```powershell
$env:CERTSUI_E2E_CREDENTIALS = '<paste-base64-here>'
```

### JWT credentials (optional)

Identity admin scenarios use the **global admin API key** from `CERTSUI_E2E_CREDENTIALS` (`X-API-KEY`) when the server supports it. Optionally set **`CERTSUI_E2E_JWT_CREDENTIALS`** — same encoding, payload `<adminUsername><US><password>` — for JWT-only probes (e.g. scoped-user login).

```powershell
$us = [char]0x1F
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("admin$us" + 'your-admin-password'))
[Environment]::SetEnvironmentVariable('CERTSUI_E2E_JWT_CREDENTIALS', $b64, 'User')
```

## Run

```powershell
pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1
```

Or run `src\e2e-tests\Test-CertsUiApiKeyE2E.bat` after credentials are set.

Filter scenarios:

```powershell
pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1 -Scenario Health
pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1 -Scenario AccountReadPatch
```

## Environment variables

| Variable | Purpose |
|----------|---------|
| `CERTSUI_E2E_CREDENTIALS` | Required — Base64 `<baseUrl><US><apiKey>` |
| `CERTSUI_E2E_JWT_CREDENTIALS` | Optional — Base64 `<adminUser><US><password>` |
| `CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES` | Optional — `MultiReplica` defaults to **1** (Docker Compose); set **2**+ for HA |
| `CERTSUI_E2E_ACCOUNT_ID` | Optional — `AccountReadPatch` target account (else first account) |

## Registered scenarios

| Id | Cmdlets |
|----|---------|
| `Health` | `Test-CertsUIHealth` |
| `ApiKeyConcurrentReads` | `Get-CertsUIAccounts` (connected session) |
| `MultiReplica` | `Get-CertsUIRuntimeInstanceId` (default **1** instance for Docker Compose; set `CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES=2` for HA) |
| `AccountReadPatch` | `Get-CertsUIAccounts`, `Get-CertsUIAccount`, `Invoke-CertsUIPatchAccount` |
| `IdentityConfigurations` | Global admin API key: users/API keys (all scope configs), PATCH remove all scopes, global-admin create probe |

`AccountReadPatch` skips when there are no accounts.

**Docker Compose:** no extra env needed for `MultiReplica` (single server container). For k8s with multiple pods: `$env:CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES = '2'`.

## Unit tests (mock HTTP only)

[`MaksIT.CertsUI.Client.Tests`](../MaksIT.CertsUI.Client.Tests/) exercises `CertsUIClient` with `FakeHttpMessageHandler` — no live server. Included in CI via `dotnet test` on that project.
