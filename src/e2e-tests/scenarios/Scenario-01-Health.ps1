#Requires -Version 7

Register-CertsUiE2eScenario -Id 'Health' -Description 'Test-CertsUIHealth: /health/live and /health/ready' -ScriptBlock {
  Write-E2eLog -Kind Step -Message '[Health] Test-CertsUIHealth'
  Test-CertsUIHealth -ErrorAction Stop
  Write-E2eLog -Kind Ok -Message '[Health] PASSED'
}
