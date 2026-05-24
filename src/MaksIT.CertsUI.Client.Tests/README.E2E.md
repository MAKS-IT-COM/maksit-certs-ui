# MaksIT.CertsUI.Client API key E2E

This project contains E2E tests in `CertsUiApiKeyE2ETests` that execute against a **running** CertsUI deployment using API key auth.

## Environment variables

- `CERTSUI_E2E_BASE_URL` (for example: `http://localhost:8080`)
- `CERTSUI_E2E_API_KEY` (API key with access to the endpoints under test)
- `CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES` (optional) — **PowerShell E2E** defaults to `1` (Docker Compose); set to `2`+ for HA. **dotnet test** defaults to `2`.

## Run (dotnet test)

```powershell
$env:CERTSUI_E2E_BASE_URL = "http://localhost:8080"
$env:CERTSUI_E2E_API_KEY = "<api-key>"
dotnet test .\src\MaksIT.CertsUI.Client.Tests\MaksIT.CertsUI.Client.Tests.csproj --filter "Category=E2E"
```

If env vars are missing, the E2E tests exit early and do not call the API.

## PowerShell E2E script (`src/e2e-tests/Test-CertsUiApiKeyE2E.ps1`)

Cmdlet reference and `Import-Module` paths: [assets/docs/POWERSHELL_CLIENT_MODULE.md](../../assets/docs/POWERSHELL_CLIENT_MODULE.md).

Requires **latest PowerShell 7** with a **.NET 10** host (install from [PowerShell releases](https://github.com/PowerShell/PowerShell/releases)). **pwsh 7.4 / .NET 8** cannot load the `net10.0` module. Verify: `[System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription` → **.NET 10.x**.

**Docker Compose + YARP on `http://localhost:8080`:** use `http://localhost:8080` as the base URL (no `/api` suffix).

### Credentials (PowerShell E2E script)

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

**dotnet test** still uses separate `CERTSUI_E2E_BASE_URL` and `CERTSUI_E2E_API_KEY` (see above).

### JWT credentials (optional)

Identity admin scenarios use the **global admin API key** from `CERTSUI_E2E_CREDENTIALS` (`X-API-KEY`) when the server supports it. Optionally set **`CERTSUI_E2E_JWT_CREDENTIALS`** — same encoding, payload `<adminUsername><US><password>` — for JWT-only probes (e.g. scoped-user login).

```powershell
$us = [char]0x1F
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("admin$us" + 'your-admin-password'))
[Environment]::SetEnvironmentVariable('CERTSUI_E2E_JWT_CREDENTIALS', $b64, 'User')
```

Then:

```powershell
pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1
```

Or run `src\e2e-tests\Test-CertsUiApiKeyE2E.bat` after credentials are set.

### Registered scenarios (all use cmdlets)

| Id | Cmdlets |
|----|---------|
| `Health` | `Test-CertsUIHealth` |
| `ApiKeyConcurrentReads` | `Get-CertsUIAccounts` (connected session) |
| `MultiReplica` | `Get-CertsUIRuntimeInstanceId` (default **1** instance for Docker Compose; set `CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES=2` for HA) |
| `AccountReadPatch` | `Get-CertsUIAccounts`, `Get-CertsUIAccount`, `Invoke-CertsUIPatchAccount` |
| `IdentityConfigurations` | Global admin API key: users/API keys (all scope configs), PATCH remove all scopes, global-admin create probe |

Optional: `CERTSUI_E2E_ACCOUNT_ID` for `AccountReadPatch` (otherwise the first account is used). `AccountReadPatch` skips when there are no accounts.

**Docker Compose:** no extra env needed for `MultiReplica` (single server container). For k8s with multiple pods: `$env:CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES = '2'`.

Filter: `pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1 -Scenario AccountReadPatch`
