#Requires -Version 7

Register-CertsUiE2eScenario -Id 'AccountReadPatch' -Description 'Get-CertsUIAccounts → Get-CertsUIAccount → Invoke-CertsUIPatchAccount → verify (no ACME create)' -ScriptBlock {
  $suffix = New-CertsUiE2eSuffix
  $originalDescription = $null
  $accountId = $null

  try {
    Write-E2eLog -Kind Step -Message '[AccountReadPatch] Get-CertsUIAccounts'
    $accounts = @(Get-CertsUIAccounts -ErrorAction Stop)
    if ($accounts.Count -eq 0) {
      Write-E2eLog -Kind Warn '[AccountReadPatch] SKIP: no accounts in deployment (create one in WebUI first, or set CERTSUI_E2E_ACCOUNT_ID)'
      return
    }

    $accountIdRaw = [Environment]::GetEnvironmentVariable('CERTSUI_E2E_ACCOUNT_ID', 'Process')
    if ([string]::IsNullOrWhiteSpace($accountIdRaw)) {
      $accountIdRaw = [Environment]::GetEnvironmentVariable('CERTSUI_E2E_ACCOUNT_ID', 'User')
    }
    if (-not [string]::IsNullOrWhiteSpace($accountIdRaw)) {
      $accountId = [guid]::Parse($accountIdRaw.Trim())
      Write-E2eLog -Message "  → Using CERTSUI_E2E_ACCOUNT_ID $accountId"
    }
    else {
      $accountId = $accounts[0].AccountId
      Write-E2eLog -Message "  → Using first account $accountId"
    }

    Write-E2eLog -Kind Step -Message '[AccountReadPatch] Get-CertsUIAccount'
    $before = Get-CertsUIAccount -AccountId $accountId -ErrorAction Stop
    $originalDescription = $before.Description
    Write-E2eLog -Kind Ok -Message "  → Description: $originalDescription"

    $patchedDescription = "e2e-patch-$suffix"
    Write-E2eLog -Kind Step -Message '[AccountReadPatch] Invoke-CertsUIPatchAccount'
    $patched = Invoke-CertsUIPatchAccount -AccountId $accountId -Description $patchedDescription -ErrorAction Stop
    if ($patched.Description -ne $patchedDescription) {
      throw "Patch response description mismatch: '$($patched.Description)'"
    }
    Write-E2eLog -Kind Ok -Message '  → Patch OK'

    Write-E2eLog -Kind Step -Message '[AccountReadPatch] Get-CertsUIAccount (verify)'
    $after = Get-CertsUIAccount -AccountId $accountId -ErrorAction Stop
    if ($after.Description -ne $patchedDescription) {
      throw "Read-after-patch description mismatch: '$($after.Description)'"
    }
    Write-E2eLog -Kind Ok -Message '  → Read-after-patch OK'

    Write-E2eLog -Kind Ok -Message '[AccountReadPatch] PASSED'
  }
  finally {
    if ($accountId -and $null -ne $originalDescription) {
      Write-E2eLog -Message '[AccountReadPatch] Restore original description'
      Invoke-CertsUIPatchAccount -AccountId $accountId -Description $originalDescription -ErrorAction SilentlyContinue | Out-Null
    }
  }
}
