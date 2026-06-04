# MaksIT.CertsUI.Client.PowerShell

PowerShell module for the CertsUI HTTP API (built on **MaksIT.CertsUI.Client**).

**Requirements:** PowerShell 7 on **.NET 10**. **Cmdlet help:** `Get-Help Connect-CertsUI -Full` (and other exported commands) after `Import-Module`. **E2E:** [src/e2e-tests/README.md](../e2e-tests/README.md).

**Quick start:**

```powershell
Import-Module .\bin\Debug\net10.0\MaksIT.CertsUI.Client.PowerShell.psd1 -Force
Connect-CertsUI -BaseAddress "http://localhost:8080" -ApiKey "<key>"
Test-CertsUIHealth
```

Requires **PowerShell 7** with a **.NET 10** host.
