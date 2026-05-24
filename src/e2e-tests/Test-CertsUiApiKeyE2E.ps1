#Requires -Version 7
<#
.SYNOPSIS
  End-to-end API key tests against a running CertsUI via MaksIT.CertsUI.Client.PowerShell.

.DESCRIPTION
  Builds the module, connects with CERTSUI_E2E_CREDENTIALS, then runs registered scenarios from src\e2e-tests\scenarios\Scenario-*.ps1 (sorted by file name).

  Add a new file src\e2e-tests\scenarios\Scenario-NN-YourName.ps1 that calls Register-CertsUiE2eScenario (see CertsUI.E2E.Common.ps1). Use a numeric prefix (01, 02, …) to control order.

  Filter which scenarios run:
    pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1 -Scenario 'Health'
    pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1 -Scenario '*Replica*','AccountReadPatch'

  Default -Scenario '*' runs all registered scenarios.

  Credentials:
    CERTSUI_E2E_CREDENTIALS — Base64 of UTF-8 "<baseUrl><US><apiKey>" (API key cmdlets).
    CERTSUI_E2E_JWT_CREDENTIALS — Base64 of UTF-8 "<adminUsername><US><password>" (identity / apikey admin REST).

.EXAMPLE
  pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1
  pwsh -File .\src\e2e-tests\Test-CertsUiApiKeyE2E.ps1 -Scenario Health
#>
param(
  [string[]] $Scenario = @('*')
)

$e2eRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $e2eRoot 'CertsUI.E2E.Common.ps1')

if ($PSVersionTable.PSVersion.Major -lt 7) {
  throw 'This script is pwsh-only.'
}

$runtimeFx = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
if ($runtimeFx -notmatch '\.NET 10\.') {
  throw (
    "The CertsUI module is net10.0; this pwsh is hosted on: $runtimeFx" + [Environment]::NewLine +
    'Install PowerShell 7 with .NET 10 from https://github.com/PowerShell/PowerShell/releases'
  )
}

$CertsUiE2ECredName = 'CERTSUI_E2E_CREDENTIALS'
$CredentialsB64 = [Environment]::GetEnvironmentVariable($CertsUiE2ECredName, 'Process')
if ([string]::IsNullOrWhiteSpace($CredentialsB64)) {
  $CredentialsB64 = [Environment]::GetEnvironmentVariable($CertsUiE2ECredName, 'User')
}
if ([string]::IsNullOrWhiteSpace($CredentialsB64)) {
  $CredentialsB64 = [Environment]::GetEnvironmentVariable($CertsUiE2ECredName, 'Machine')
}

$nl = [Environment]::NewLine
$CertsUiE2EEncodeHint = @(
  'CERTSUI_E2E_CREDENTIALS: Base64(UTF-8 "<baseUrl><US><apiKey>"). Set $us = [char]0x1F on the line before $b64.'
  '  $us = [char]0x1F'
  '  $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("http://localhost:8080$us" + ''your-api-key''))'
  '  [Environment]::SetEnvironmentVariable(''CERTSUI_E2E_CREDENTIALS'', $b64, ''User'')'
) -join $nl

if ([string]::IsNullOrWhiteSpace($CredentialsB64)) {
  throw ("CERTSUI_E2E_CREDENTIALS is not set (Process, User, Machine)." + $nl + $nl + $CertsUiE2EEncodeHint)
}

$CredentialsB64 = ($CredentialsB64.Trim() -replace '\s', '')

try {
  $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($CredentialsB64))
}
catch {
  throw "CERTSUI_E2E_CREDENTIALS is not valid Base64: $($_.Exception.Message)"
}

$decoded = $decoded.Trim()
if ([string]::IsNullOrWhiteSpace($decoded)) {
  throw 'Decoded credentials are empty.'
}

$us = [char]0x1F
if (-not $decoded.Contains($us)) {
  throw ("Decoded credentials must be <baseUrl><US><apiKey> (UTF-8), then Base64." + $nl + $CertsUiE2EEncodeHint)
}

$parts = $decoded.Split($us, 2, [StringSplitOptions]::None)
$BaseUrl = $parts[0].Trim()
$ApiKey = if ($parts.Length -gt 1) { $parts[1].Trim() } else { '' }

if ([string]::IsNullOrWhiteSpace($BaseUrl) -or [string]::IsNullOrWhiteSpace($ApiKey)) {
  throw 'Decoded BaseUrl or ApiKey is empty.'
}

$script:CertsUiE2eBaseUrl = $BaseUrl
$script:CertsUiE2eApiKey = $ApiKey
$jwtCred = Get-CertsUiE2eDecodedUsCredential -EnvVarName 'CERTSUI_E2E_JWT_CREDENTIALS'
if ($jwtCred) {
  $script:CertsUiE2eJwtUsername = $jwtCred.Part1
  $script:CertsUiE2eJwtPassword = $jwtCred.Part2
  Write-E2eLog -Kind Ok -Message "JWT E2E login user: $($script:CertsUiE2eJwtUsername) (password not logged)"
}
else {
  Write-E2eLog -Message 'Optional CERTSUI_E2E_JWT_CREDENTIALS not set; identity scenarios use global admin X-API-KEY from CERTSUI_E2E_CREDENTIALS.'
}

$repoRoot = Resolve-Path (Join-Path $e2eRoot '..\..')
$moduleTfm = 'net10.0'
$relModuleProject = 'src\MaksIT.CertsUI.Client.PowerShell\MaksIT.CertsUI.Client.PowerShell.csproj'
$relModuleManifest = "src\MaksIT.CertsUI.Client.PowerShell\bin\Debug\$moduleTfm\MaksIT.CertsUI.Client.PowerShell.psd1"

Write-E2eLog -Kind Step -Message "Build: $relModuleProject"
Push-Location $repoRoot
try {
  $buildOutput = dotnet build $relModuleProject 2>&1
  if ($LASTEXITCODE -ne 0) {
    $buildOutput | ForEach-Object { Write-Host $_ }
    throw "Build failed: $relModuleProject"
  }
  if (-not (Test-Path -LiteralPath $relModuleManifest)) {
    throw "Module manifest not found: $relModuleManifest"
  }
  $moduleManifest = (Resolve-Path -LiteralPath $relModuleManifest).Path
}
finally {
  Pop-Location
}

Write-E2eLog -Kind Ok -Message "Importing module: $moduleManifest"
Import-Module $moduleManifest -Force

Clear-CertsUiE2eScenarioRegistry
$scenarioDir = Join-Path $e2eRoot 'scenarios'
if (-not (Test-Path -LiteralPath $scenarioDir)) {
  throw "Scenarios directory missing: $scenarioDir"
}
Get-ChildItem -LiteralPath $scenarioDir -Filter 'Scenario-*.ps1' | Sort-Object Name | ForEach-Object {
  Write-E2eLog -Message "Load scenarios: $($_.Name)"
  . $_.FullName
}

Write-E2eLog -Kind Step -Message "Connect-CertsUI (base URL: $BaseUrl; API key not logged)"
Connect-CertsUI -BaseAddress $BaseUrl -ApiKey $ApiKey
Write-E2eLog -Kind Ok -Message 'Connect-CertsUI: session ready'

$ErrorActionPreference = 'Stop'
$ran = 0
try {
  foreach ($entry in $script:CertsUiE2eScenarioRegistry) {
    $include = $false
    foreach ($p in $Scenario) {
      if ($entry.Id -like $p) {
        $include = $true
        break
      }
    }
    if (-not $include) {
      Write-E2eLog -Kind Warn "Skip (filter): $($entry.Id)"
      continue
    }

    Write-E2eLog -Kind Step -Message "========== Scenario: $($entry.Id) =========="
    Write-E2eLog -Message $entry.Description
    try {
      & $entry.ScriptBlock
      $ran++
    }
    catch {
      Write-E2eLog -Kind Warn "Scenario '$($entry.Id)' FAILED: $($_.Exception.Message)"
      throw
    }
  }

  if ($ran -eq 0) {
    $registered = ($script:CertsUiE2eScenarioRegistry | ForEach-Object { $_.Id }) -join ', '
    throw "No scenarios matched -Scenario patterns: $($Scenario -join ', '). Registered: $registered"
  }

  Write-E2eLog -Kind Ok -Message "All selected scenarios passed ($ran run)."
}
finally {
  Disconnect-CertsUI
  Write-E2eLog -Message 'Disconnect-CertsUI'
}
