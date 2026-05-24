# PowerShell client module (`MaksIT.CertsUI.Client.PowerShell`)

PowerShell module that exposes the **MaksIT CertsUI API** via custom cmdlets, built on **MaksIT.CertsUI.Client** (C# / .NET).

**Source:** `src/MaksIT.CertsUI.Client.PowerShell/` · **E2E:** [README.E2E.md](../../src/MaksIT.CertsUI.Client.Tests/README.E2E.md)

---

## Requirements

- **Latest [PowerShell 7](https://github.com/PowerShell/PowerShell/releases)** with a **.NET 10** host. **pwsh 7.4 on .NET 8** cannot load this **`net10.0`** module.

```powershell
[System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
# expect: .NET 10.0.x
```

## Installation

1. Build the solution (or the `MaksIT.CertsUI.Client.PowerShell` project).
2. Import from build output:

   ```powershell
   Import-Module .\src\MaksIT.CertsUI.Client.PowerShell\bin\Debug\net10.0\MaksIT.CertsUI.Client.PowerShell.psd1 -Force
   ```

## Usage

1. **Connect** (base URL is the public ingress or YARP root, e.g. `http://localhost:8080` — no `/api` suffix):

   ```powershell
   Connect-CertsUI -BaseAddress "http://localhost:8080" -ApiKey "your-api-key"
   ```

2. **Health and accounts:**

   ```powershell
   Test-CertsUIHealth
   Get-CertsUIAccounts
   Get-CertsUIAccount -AccountId <guid>
   ```

3. **HA / load-balancer check:**

   ```powershell
   Get-CertsUIRuntimeInstanceId
   ```

4. **Create / patch / delete account** (create runs the full ACME flow — use staging for tests):

   ```powershell
   Invoke-CertsUICreateAccount -Description "e2e" -Contacts "mailto:a@b" -ChallengeType "http-01" -Hostnames "example.com" -IsStaging -AgreeToS
   Invoke-CertsUIPatchAccount -AccountId <guid> -Description "updated"
   Invoke-CertsUIDeleteAccount -AccountId <guid>
   ```

5. **Disconnect:**

   ```powershell
   Disconnect-CertsUI
   ```

## Cmdlets

| Cmdlet | Description |
|--------|-------------|
| `Connect-CertsUI` | Set base URL and API key for the session |
| `Disconnect-CertsUI` | Clear session |
| `Test-CertsUIHealth` | `GET /health/live` and `/health/ready` |
| `Get-CertsUIAccounts` | `GET /api/accounts` |
| `Get-CertsUIAccount` | `GET /api/account/{id}` |
| `Get-CertsUIRuntimeInstanceId` | `GET /api/debug/runtime-instance-id` |
| `Invoke-CertsUICreateAccount` | `POST /api/account` |
| `Invoke-CertsUIPatchAccount` | `PATCH /api/account/{id}` |
| `Invoke-CertsUIDeleteAccount` | `DELETE /api/account/{id}` |

## E2E scenarios

PowerShell scenarios under [`src/e2e-tests/`](../src/e2e-tests/) build this module and run registered tests. See [README.E2E.md](../../src/MaksIT.CertsUI.Client.Tests/README.E2E.md).
