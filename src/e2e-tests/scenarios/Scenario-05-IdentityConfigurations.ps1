#Requires -Version 7

Register-CertsUiE2eScenario -Id 'IdentityConfigurations' -Description 'Global admin API key (or JWT): create users/API keys in all scope configs, patch remove all scopes, probe global-admin create' -ScriptBlock {
  if (-not (Test-CertsUiE2eAdminConfigured)) {
    Write-E2eLog -Kind Warn '[IdentityConfigurations] SKIP: set CERTSUI_E2E_CREDENTIALS (global admin API key) or CERTSUI_E2E_JWT_CREDENTIALS'
    return
  }

  $suffix = New-CertsUiE2eSuffix
  $readScope = 1

  function Remove-E2eUser {
    param($Id)
    if ($null -eq $Id) { return }
    try {
      $guid = [guid]$Id
      if ($guid -eq [guid]::Empty) { return }
      Invoke-CertsUiE2eAdminApi -Method Delete -Path "api/identity/user/$guid" -AllowFailure | Out-Null
    }
    catch { }
  }

  function Remove-E2eApiKey {
    param($Id)
    if ($null -eq $Id) { return }
    try {
      $guid = [guid]$Id
      if ($guid -eq [guid]::Empty) { return }
      Invoke-CertsUiE2eAdminApi -Method Delete -Path "api/apikey/$guid" -AllowFailure | Out-Null
    }
    catch { }
  }

  function New-E2eUser {
    param(
      [string] $Label,
      [bool] $IsGlobalAdmin,
      [object[]] $EntityScopes
    )
    $body = @{
      username      = "e2e-$Label-$suffix"
      email         = "e2e-$Label-$suffix@test.invalid"
      mobileNumber  = '+10000000001'
      password      = 'E2e-User-Test-1!'
      isGlobalAdmin = $IsGlobalAdmin
      entityScopes  = @($EntityScopes)
    }
    Write-E2eLog -Kind Step -Message "[IdentityConfigurations] POST user ($Label)"
    $created = (Invoke-CertsUiE2eAdminApi -Method Post -Path 'api/identity/user' -Body $body).Response
    if (-not $created.id) {
      throw 'POST user returned no id (is the server rebuilt/restarted with CertsUIAuthorizationFilter on identity routes?)'
    }
    $id = [guid]$created.id
    if ([bool]$created.isGlobalAdmin -ne $IsGlobalAdmin) {
      throw "User $Label isGlobalAdmin mismatch: $($created.isGlobalAdmin)"
    }
    $read = (Invoke-CertsUiE2eAdminApi -Method Get -Path "api/identity/user/$id").Response
    if (@($read.entityScopes).Count -ne @($EntityScopes).Count) {
      throw "User $Label scope count mismatch: expected $(@($EntityScopes).Count), got $(@($read.entityScopes).Count)"
    }
    Write-E2eLog -Kind Ok -Message "  → User $Label OK ($id)"
    return @{ Id = $id; Username = $body.username; Password = $body.password; Response = $read }
  }

  function New-E2eApiKey {
    param(
      [string] $Label,
      [bool] $IsGlobalAdmin,
      [object[]] $EntityScopes
    )
    $body = @{
      description   = "e2e-$Label-$suffix"
      isGlobalAdmin = $IsGlobalAdmin
      entityScopes  = @($EntityScopes)
    }
    Write-E2eLog -Kind Step -Message "[IdentityConfigurations] POST apikey ($Label)"
    $created = (Invoke-CertsUiE2eAdminApi -Method Post -Path 'api/apikey' -Body $body).Response
    $id = [guid]$created.id
    if ([bool]$created.isGlobalAdmin -ne $IsGlobalAdmin) {
      throw "API key $Label isGlobalAdmin mismatch: $($created.isGlobalAdmin)"
    }
    $read = (Invoke-CertsUiE2eAdminApi -Method Get -Path "api/apikey/$id").Response
    if (@($read.entityScopes).Count -ne @($EntityScopes).Count) {
      throw "API key $Label scope count mismatch: expected $(@($EntityScopes).Count), got $(@($read.entityScopes).Count)"
    }
    Write-E2eLog -Kind Ok -Message "  → API key $Label OK ($id)"
    return @{ Id = $id; Response = $read }
  }

  $userConfigs = @(
    @{ Label = 'user-gadmin'; IsGlobalAdmin = $true; Scopes = @() }
    @{ Label = 'user-all-scopes'; IsGlobalAdmin = $false; Scopes = (Get-CertsUiE2eAllEntityScopePayload) }
    @{ Label = 'user-identity-only'; IsGlobalAdmin = $false; Scopes = (Get-CertsUiE2eScopePayload -EntityTypes @($script:CertsUiE2eScopeEntityTypeIdentity)) }
    @{ Label = 'user-apikey-only'; IsGlobalAdmin = $false; Scopes = (Get-CertsUiE2eScopePayload -EntityTypes @($script:CertsUiE2eScopeEntityTypeApiKey)) }
    @{ Label = 'user-identity-read'; IsGlobalAdmin = $false; Scopes = (Get-CertsUiE2eScopePayload -EntityTypes @($script:CertsUiE2eScopeEntityTypeIdentity) -Scope $readScope) }
  )

  $keyConfigs = @(
    @{ Label = 'key-gadmin'; IsGlobalAdmin = $true; Scopes = @() }
    @{ Label = 'key-all-scopes'; IsGlobalAdmin = $false; Scopes = (Get-CertsUiE2eAllEntityScopePayload) }
    @{ Label = 'key-identity-only'; IsGlobalAdmin = $false; Scopes = (Get-CertsUiE2eScopePayload -EntityTypes @($script:CertsUiE2eScopeEntityTypeIdentity)) }
    @{ Label = 'key-apikey-only'; IsGlobalAdmin = $false; Scopes = (Get-CertsUiE2eScopePayload -EntityTypes @($script:CertsUiE2eScopeEntityTypeApiKey)) }
    @{ Label = 'key-identity-read'; IsGlobalAdmin = $false; Scopes = (Get-CertsUiE2eScopePayload -EntityTypes @($script:CertsUiE2eScopeEntityTypeIdentity) -Scope $readScope) }
  )

  $scopedUser = $null
  $patchKeyId = $null
  $patchUserId = $null

  try {
    foreach ($cfg in $userConfigs) {
      $u = New-E2eUser -Label $cfg.Label -IsGlobalAdmin $cfg.IsGlobalAdmin -EntityScopes $cfg.Scopes
      Remove-E2eUser -Id $u.Id
    }

    foreach ($cfg in $keyConfigs) {
      $k = New-E2eApiKey -Label $cfg.Label -IsGlobalAdmin $cfg.IsGlobalAdmin -EntityScopes $cfg.Scopes
      Remove-E2eApiKey -Id $k.Id
    }

    Write-E2eLog -Kind Step -Message '[IdentityConfigurations] PATCH apikey — remove all scopes (UI regression)'
    $withScopes = New-E2eApiKey -Label 'key-patch-remove' -IsGlobalAdmin $false -EntityScopes (Get-CertsUiE2eAllEntityScopePayload)
    $patchKeyId = $withScopes.Id
    $removePatch = @{ entityScopes = (New-CertsUiE2eEntityScopeRemovePatch -EntityScopes $withScopes.Response.entityScopes) }
    $patched = (Invoke-CertsUiE2eAdminApi -Method Patch -Path "api/apikey/$patchKeyId" -Body $removePatch).Response
    if (@($patched.entityScopes).Count -ne 0) {
      throw "Expected zero scopes after remove patch; got $(@($patched.entityScopes).Count)"
    }
    Write-E2eLog -Kind Ok -Message '  → API key scope removal OK'
    Remove-E2eApiKey -Id $patchKeyId
    $patchKeyId = $null

    Write-E2eLog -Kind Step -Message '[IdentityConfigurations] PATCH user — remove all scopes'
    $userWithScopes = New-E2eUser -Label 'user-patch-remove' -IsGlobalAdmin $false -EntityScopes (Get-CertsUiE2eAllEntityScopePayload)
    $patchUserId = $userWithScopes.Id
    $userRemovePatch = @{ entityScopes = (New-CertsUiE2eEntityScopeRemovePatch -EntityScopes $userWithScopes.Response.entityScopes) }
    $patchedUser = (Invoke-CertsUiE2eAdminApi -Method Patch -Path "api/identity/user/$patchUserId" -Body $userRemovePatch).Response
    if (@($patchedUser.entityScopes).Count -ne 0) {
      throw "Expected zero user scopes after remove patch; got $(@($patchedUser.entityScopes).Count)"
    }
    Write-E2eLog -Kind Ok -Message '  → User scope removal OK'
    Remove-E2eUser -Id $patchUserId
    $patchUserId = $null

    Write-E2eLog -Kind Step -Message '[IdentityConfigurations] Scoped user JWT probes global-admin user create'
    $scopedUser = New-E2eUser -Label 'user-scoped-probe' -IsGlobalAdmin $false -EntityScopes (Get-CertsUiE2eAllEntityScopePayload)
    $scopedToken = Get-CertsUiE2eJwtToken -Username $scopedUser.Username -Password $scopedUser.Password
    $probe = Invoke-CertsUiE2eAdminApi -Method Post -Path 'api/identity/user' -Token $scopedToken -AllowFailure -Body @{
      username      = "e2e-gadmin-denied-$suffix"
      email         = "e2e-denied-$suffix@test.invalid"
      mobileNumber  = '+10000000004'
      password      = 'E2e-Denied-Probe-1!'
      isGlobalAdmin = $true
      entityScopes  = @()
    }
    if (-not $probe.Ok) {
      Write-E2eLog -Kind Ok -Message "  → Non-global-admin JWT denied global-admin user create (HTTP $($probe.StatusCode))"
    }
    else {
      throw 'Non-global-admin JWT must not create a global-admin user (RBAC regression)'
    }

    Write-E2eLog -Kind Step -Message '[IdentityConfigurations] Scoped user JWT probes global-admin API key create'
    $keyProbe = Invoke-CertsUiE2eAdminApi -Method Post -Path 'api/apikey' -Token $scopedToken -AllowFailure -Body @{
      description   = "e2e-gadmin-key-denied-$suffix"
      isGlobalAdmin = $true
      entityScopes  = @()
    }
    if (-not $keyProbe.Ok) {
      Write-E2eLog -Kind Ok -Message "  → Non-global-admin JWT denied global-admin API key create (HTTP $($keyProbe.StatusCode))"
    }
    else {
      Remove-E2eApiKey -Id ([guid]$keyProbe.Response.id)
      throw 'Non-global-admin JWT must not create a global-admin API key (RBAC regression)'
    }

    Write-E2eLog -Kind Ok -Message '[IdentityConfigurations] PASSED'
  }
  finally {
    if ($scopedUser) { Remove-E2eUser -Id $scopedUser.Id }
    Remove-E2eUser -Id $patchUserId
    Remove-E2eApiKey -Id $patchKeyId
  }
}
