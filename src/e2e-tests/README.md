# CertsUI E2E tests (PowerShell)

API-key end-to-end tests against a running CertsUI deployment via **MaksIT.CertsUI.Client.PowerShell**.

**Module quick start:** [MaksIT.CertsUI.Client.PowerShell/README.md](../MaksIT.CertsUI.Client.PowerShell/README.md).

## Credentials

Set **`CERTSUI_E2E_CREDENTIALS`** — Base64 of UTF-8 `<baseUrl><US><apiKey>` (`<US>` = ASCII unit separator, `[char]0x1F`):

```powershell
$us = [char]0x1F
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("http://localhost:8080$us" + 'your-api-key'))
[Environment]::SetEnvironmentVariable('CERTSUI_E2E_CREDENTIALS', $b64, 'User')
```

Optional **`CERTSUI_E2E_JWT_CREDENTIALS`** — same encoding, payload `<adminUsername><US><password>` — for JWT-only identity probes.

## Run

```powershell
pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1
pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1 -Scenario Health
```

Or `src\e2e-tests\Test-CertsUiApiKeyE2E.bat` after credentials are set.

E2E is **not** run in CI. Requires **PowerShell 7** on **.NET 10** (see script error text if the host is wrong).
