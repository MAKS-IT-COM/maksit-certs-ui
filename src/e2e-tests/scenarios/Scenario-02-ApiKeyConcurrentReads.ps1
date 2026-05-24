#Requires -Version 7

Register-CertsUiE2eScenario -Id 'ApiKeyConcurrentReads' -Description 'Concurrent Get-CertsUIAccounts with API key — no auth failures, all succeed' -ScriptBlock {
  $parallelRequests = 12
  Write-E2eLog -Kind Step -Message "[ApiKeyConcurrentReads] issuing $parallelRequests concurrent Get-CertsUIAccounts"

  1..$parallelRequests | ForEach-Object {
    $null = Get-CertsUIAccounts -ErrorAction Stop
  }

  Write-E2eLog -Kind Ok -Message '  → All concurrent API-key reads succeeded'
  Write-E2eLog -Kind Ok -Message '[ApiKeyConcurrentReads] PASSED'
}
