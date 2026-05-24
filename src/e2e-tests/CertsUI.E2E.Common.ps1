# Shared helpers for CertsUI API key E2E. Dot-sourced by Test-CertsUiApiKeyE2E.ps1 and by scenario scripts under scenarios/.

$script:CertsUiE2eScenarioRegistry = [System.Collections.Generic.List[hashtable]]::new()

function Clear-CertsUiE2eScenarioRegistry {
  $script:CertsUiE2eScenarioRegistry.Clear()
}

<#
.SYNOPSIS
  Registers one runnable E2E scenario (called from src\e2e-tests\scenarios\Scenario-*.ps1).
.DESCRIPTION
  After all scenario files are dot-sourced, the harness runs each registered entry in registration order
  (use numeric prefixes in filenames, e.g. Scenario-01-*.ps1, to control order).
.PARAMETER Id
  Short id for -Scenario filtering (e.g. Health, ApiKeyConcurrentReads).
#>
function Register-CertsUiE2eScenario {
  param(
    [Parameter(Mandatory)][string] $Id,
    [Parameter(Mandatory)][string] $Description,
    [Parameter(Mandatory)][scriptblock] $ScriptBlock
  )
  $script:CertsUiE2eScenarioRegistry.Add(@{
    Id          = $Id
    Description = $Description
    ScriptBlock = $ScriptBlock
  }) | Out-Null
}

function Write-E2eLog {
  param(
    [Parameter(Mandatory)][string] $Message,
    [ValidateSet('Default', 'Step', 'Ok', 'Warn')]
    [string] $Kind = 'Default'
  )
  $ts = (Get-Date).ToUniversalTime().ToString('o')
  $line = "[$ts] $Message"
  switch ($Kind) {
    'Step' { Write-Host $line -ForegroundColor Cyan }
    'Ok' { Write-Host $line -ForegroundColor Green }
    'Warn' { Write-Host $line -ForegroundColor Yellow }
    default { Write-Host $line }
  }
}

function New-CertsUiE2eSuffix {
  [guid]::NewGuid().ToString('N').Substring(0, 8)
}

# Populated by Test-CertsUiApiKeyE2E.ps1 after decoding CERTSUI_E2E_CREDENTIALS.
$script:CertsUiE2eBaseUrl = $null
$script:CertsUiE2eApiKey = $null
$script:CertsUiE2eJwtUsername = $null
$script:CertsUiE2eJwtPassword = $null

# Platform-wide Identity / ApiKey scope rows (WebUI sentinel GUID).
$script:CertsUiE2ePlatformScopeEntityId = [guid]'00000000-0000-0000-0000-000000000001'
# ScopePermission: Read|Write|Delete|Create
$script:CertsUiE2eAllScopePermissions = 15
$script:CertsUiE2eScopeEntityTypeIdentity = 0
$script:CertsUiE2eScopeEntityTypeApiKey = 1

function Get-CertsUiE2eEnvVar {
  param([Parameter(Mandatory)][string] $Name)
  $v = [Environment]::GetEnvironmentVariable($Name, 'Process')
  if ([string]::IsNullOrWhiteSpace($v)) {
    $v = [Environment]::GetEnvironmentVariable($Name, 'User')
  }
  if ([string]::IsNullOrWhiteSpace($v)) {
    $v = [Environment]::GetEnvironmentVariable($Name, 'Machine')
  }
  return $v
}

<#
.SYNOPSIS
  Decodes CERTSUI_E2E_*_CREDENTIALS (Base64 of UTF-8 "<part1><US><part2>").
#>
function Get-CertsUiE2eDecodedUsCredential {
  param(
    [Parameter(Mandatory)][string] $EnvVarName,
    [switch] $Required
  )
  $b64 = Get-CertsUiE2eEnvVar -Name $EnvVarName
  if ([string]::IsNullOrWhiteSpace($b64)) {
    if ($Required) {
      throw "$EnvVarName is not set (Process, User, Machine)."
    }
    return $null
  }
  $b64 = ($b64.Trim() -replace '\s', '')
  try {
    $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($b64))
  }
  catch {
    throw "$EnvVarName is not valid Base64: $($_.Exception.Message)"
  }
  $us = [char]0x1F
  if (-not $decoded.Contains($us)) {
    throw "$EnvVarName must decode to <part1><US><part2> (UTF-8), then Base64."
  }
  $parts = $decoded.Split($us, 2, [StringSplitOptions]::None)
  $p1 = $parts[0].Trim()
  $p2 = if ($parts.Length -gt 1) { $parts[1].Trim() } else { '' }
  if ([string]::IsNullOrWhiteSpace($p1) -or [string]::IsNullOrWhiteSpace($p2)) {
    throw "$EnvVarName decoded parts must be non-empty."
  }
  return @{ Part1 = $p1; Part2 = $p2 }
}

function Test-CertsUiE2eJwtConfigured {
  -not [string]::IsNullOrWhiteSpace($script:CertsUiE2eJwtUsername) -and
    -not [string]::IsNullOrWhiteSpace($script:CertsUiE2eJwtPassword) -and
    -not [string]::IsNullOrWhiteSpace($script:CertsUiE2eBaseUrl)
}

function Test-CertsUiE2eAdminConfigured {
  (-not [string]::IsNullOrWhiteSpace($script:CertsUiE2eApiKey) -or (Test-CertsUiE2eJwtConfigured)) -and
    -not [string]::IsNullOrWhiteSpace($script:CertsUiE2eBaseUrl)
}

function Get-CertsUiE2eScopePayload {
  param(
    [Parameter(Mandatory)][int[]] $EntityTypes,
    [int] $Scope = $script:CertsUiE2eAllScopePermissions
  )
  $id = $script:CertsUiE2ePlatformScopeEntityId.ToString('D')
  return @($EntityTypes | ForEach-Object {
    @{ entityId = $id; entityType = $_; scope = $Scope }
  })
}

function Get-CertsUiE2eAllEntityScopePayload {
  Get-CertsUiE2eScopePayload -EntityTypes @(
    $script:CertsUiE2eScopeEntityTypeIdentity
    $script:CertsUiE2eScopeEntityTypeApiKey
  )
}

function Test-CertsUiE2eEntityScopesMatchAll {
  param($EntityScopes)
  $expected = $script:CertsUiE2eAllScopePermissions
  $platform = $script:CertsUiE2ePlatformScopeEntityId
  foreach ($type in @($script:CertsUiE2eScopeEntityTypeIdentity, $script:CertsUiE2eScopeEntityTypeApiKey)) {
    $row = @($EntityScopes) | Where-Object {
      [int]$_.entityType -eq $type -and [guid]$_.entityId -eq $platform
    } | Select-Object -First 1
    if (-not $row) {
      throw "Expected platform scope row for entityType=$type"
    }
    if ([int]$row.scope -ne $expected) {
      throw "entityType=$type scope=$($row.scope); expected all CRUD flags ($expected)"
    }
  }
}

function Get-CertsUiE2eJwtToken {
  param(
    [string] $Username = $script:CertsUiE2eJwtUsername,
    [string] $Password = $script:CertsUiE2eJwtPassword
  )
  $base = $script:CertsUiE2eBaseUrl.TrimEnd('/')
  $body = @{ username = $Username; password = $Password } | ConvertTo-Json -Compress
  try {
    $login = Invoke-RestMethod -Method Post -Uri "$base/api/identity/login" -Body $body -ContentType 'application/json'
  }
  catch {
    throw "Login failed for '$Username': $($_.Exception.Message)"
  }
  if ([string]::IsNullOrWhiteSpace($login.token)) {
    throw 'Login response did not include a token.'
  }
  return $login.token
}

function New-CertsUiE2eEntityScopeRemovePatch {
  param([Parameter(Mandatory)] $EntityScopes)
  # PatchOperation.RemoveFromCollection = 3 (matches WebUI enum)
  return @($EntityScopes | ForEach-Object {
    @{
      id         = [guid]$_.id
      operations = @{ collectionItemOperation = 3 }
    }
  })
}

function Invoke-CertsUiE2eAdminApi {
  param(
    [Parameter(Mandatory)][ValidateSet('Get', 'Post', 'Patch', 'Delete')]
    [string] $Method,
    [Parameter(Mandatory)][string] $Path,
    [object] $Body,
    [string] $Token,
    [switch] $AllowFailure
  )
  $uri = "$($script:CertsUiE2eBaseUrl.TrimEnd('/'))/$($Path.TrimStart('/'))"
  $headers = @{}
  if (-not [string]::IsNullOrWhiteSpace($Token)) {
    $headers.Authorization = "Bearer $Token"
  }
  elseif (-not [string]::IsNullOrWhiteSpace($script:CertsUiE2eApiKey)) {
    $headers['X-API-KEY'] = $script:CertsUiE2eApiKey
  }
  elseif (Test-CertsUiE2eJwtConfigured) {
    $headers.Authorization = "Bearer $(Get-CertsUiE2eJwtToken)"
  }
  else {
    throw 'No admin auth: set CERTSUI_E2E_CREDENTIALS (global admin API key) or CERTSUI_E2E_JWT_CREDENTIALS.'
  }
  $params = @{
    Method      = $Method
    Uri         = $uri
    Headers     = $headers
    ContentType = 'application/json'
  }
  if ($null -ne $Body) {
    $params.Body = ($Body | ConvertTo-Json -Depth 12 -Compress)
  }
  try {
    $response = Invoke-RestMethod @params
    return @{ Ok = $true; StatusCode = 200; Response = $response }
  }
  catch {
    $status = 0
    $resp = $_.Exception.Response
    if ($resp) {
      $status = [int]$resp.StatusCode
    }
    $detail = $_.ErrorDetails.Message
    if ([string]::IsNullOrWhiteSpace($detail)) {
      $detail = $_.Exception.Message
    }
    if ($AllowFailure) {
      return @{ Ok = $false; StatusCode = $status; Error = $detail; Response = $null }
    }
    throw "HTTP $status ${Method} ${Path}: $detail"
  }
}

function Invoke-CertsUiE2eJwtApi {
  param(
    [Parameter(Mandatory)][ValidateSet('Get', 'Post', 'Patch', 'Delete')]
    [string] $Method,
    [Parameter(Mandatory)][string] $Path,
    [Parameter(Mandatory)][string] $Token,
    [object] $Body,
    [switch] $AllowFailure
  )
  Invoke-CertsUiE2eAdminApi -Method $Method -Path $Path -Token $Token -Body $Body -AllowFailure:$AllowFailure
}
