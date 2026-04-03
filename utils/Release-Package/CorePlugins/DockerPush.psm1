#requires -Version 7.0
#requires -PSEdition Core

<#
.SYNOPSIS
    Build and push Docker images to a container registry.

.DESCRIPTION
    Logs in with credentials from a Base64-encoded username:password environment variable,
    builds each configured image once, then tags and pushes: bare semver from DotNetReleaseVersion
    (e.g. 3.3.4), v-prefixed alias (v3.3.4) when different, optional exact shared.tag if it differs,
    and optional latest.

    Release image tags align with shared.version (same bare semver as Helm chart/OCI); not from Chart.yaml.
#>

if (-not (Get-Command Import-PluginDependency -ErrorAction SilentlyContinue)) {
    $pluginSupportModulePath = Join-Path (Split-Path $PSScriptRoot -Parent) "PluginSupport.psm1"
    if (Test-Path $pluginSupportModulePath -PathType Leaf) {
        Import-Module $pluginSupportModulePath -Force -Global -ErrorAction Stop
    }
}

function Get-RegistryCredentialsFromEnv {
    param(
        [Parameter(Mandatory = $true)]
        [string]$EnvVarName
    )

    $raw = [Environment]::GetEnvironmentVariable($EnvVarName)
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "Environment variable '$EnvVarName' is not set."
    }

    try {
        $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($raw))
    }
    catch {
        throw "Failed to decode '$EnvVarName' as Base64 (expected base64('username:password')): $($_.Exception.Message)"
    }

    $parts = $decoded -split ':', 2
    if ($parts.Count -ne 2 -or [string]::IsNullOrWhiteSpace($parts[0]) -or [string]::IsNullOrWhiteSpace($parts[1])) {
        throw "Decoded '$EnvVarName' must be in the form 'username:password'."
    }

    return @{ User = $parts[0]; Password = $parts[1] }
}

function Invoke-Plugin {
    param(
        [Parameter(Mandatory = $true)]
        $Settings
    )

    Import-PluginDependency -ModuleName "Logging" -RequiredCommand "Write-Log"
    Import-PluginDependency -ModuleName "ScriptConfig" -RequiredCommand "Assert-Command"

    $pluginSettings = $Settings
    $shared = $Settings.context

    Assert-Command docker

    if ([string]::IsNullOrWhiteSpace($pluginSettings.registryUrl)) {
        throw "DockerPush plugin requires 'registryUrl' (registry hostname, no scheme)."
    }

    if ([string]::IsNullOrWhiteSpace($pluginSettings.credentialsEnvVar)) {
        throw "DockerPush plugin requires 'credentialsEnvVar' (name of env var holding base64 username:password)."
    }

    if ([string]::IsNullOrWhiteSpace($pluginSettings.projectName)) {
        throw "DockerPush plugin requires 'projectName' (image path segment after registry)."
    }

    if ([string]::IsNullOrWhiteSpace($pluginSettings.contextPath)) {
        throw "DockerPush plugin requires 'contextPath' (Docker build context, relative to Release-Package folder)."
    }

    if (-not $pluginSettings.images -or @($pluginSettings.images).Count -eq 0) {
        throw "DockerPush plugin requires a non-empty 'images' array with 'service' and 'dockerfile' per entry."
    }

    $scriptDir = $shared.scriptDir
    $contextPath = [System.IO.Path]::GetFullPath((Join-Path $scriptDir ([string]$pluginSettings.contextPath)))
    if (-not (Test-Path $contextPath -PathType Container)) {
        throw "Docker context directory not found: $contextPath"
    }

    $registryUrl = [string]$pluginSettings.registryUrl.TrimEnd('/')
    $creds = Get-RegistryCredentialsFromEnv -EnvVarName ([string]$pluginSettings.credentialsEnvVar)

    $bareVersion = $null
    if ($shared.PSObject.Properties.Name -contains 'version' -and -not [string]::IsNullOrWhiteSpace([string]$shared.version)) {
        $bareVersion = ([string]$shared.version).Trim() -replace '^[vV]', ''
    }
    if ([string]::IsNullOrWhiteSpace($bareVersion) -and $shared.PSObject.Properties.Name -contains 'tag') {
        $bareVersion = ([string]$shared.tag).Trim() -replace '^[vV]', ''
    }
    if ([string]::IsNullOrWhiteSpace($bareVersion)) {
        throw "DockerPush: could not derive version tag (need shared.version from DotNetReleaseVersion or shared.tag)."
    }

    $imageTags = New-Object System.Collections.Generic.List[string]
    function Add-ImageTag([System.Collections.Generic.List[string]]$List, [string]$Tag) {
        if ([string]::IsNullOrWhiteSpace($Tag)) { return }
        if (-not $List.Contains($Tag)) { [void]$List.Add($Tag) }
    }
    Add-ImageTag $imageTags $bareVersion
    Add-ImageTag $imageTags "v$bareVersion"
    if ($shared.PSObject.Properties.Name -contains 'tag') {
        Add-ImageTag $imageTags ([string]$shared.tag).Trim()
    }
    $pushLatest = if ($null -ne $pluginSettings.pushLatest) { [bool]$pluginSettings.pushLatest } else { $true }
    if ($pushLatest) {
        Add-ImageTag $imageTags 'latest'
    }

    Write-Log -Level "STEP" -Message "Docker login to $registryUrl..."
    $loginResult = $creds.Password | docker login $registryUrl -u $creds.User --password-stdin 2>&1
    if ($LASTEXITCODE -ne 0 -or ($loginResult -notmatch 'Login Succeeded')) {
        throw "Docker login failed for ${registryUrl}: $loginResult"
    }

    try {
        foreach ($img in @($pluginSettings.images)) {
            if ($null -eq $img.service -or $null -eq $img.dockerfile) {
                throw "Each images[] entry must define 'service' and 'dockerfile'."
            }

            $dockerfileRel = [string]$img.dockerfile
            $dockerfilePath = [System.IO.Path]::GetFullPath((Join-Path $contextPath $dockerfileRel))
            if (-not (Test-Path $dockerfilePath -PathType Leaf)) {
                throw "Dockerfile not found: $dockerfilePath"
            }

            $service = [string]$img.service
            $baseName = "$registryUrl/$($pluginSettings.projectName)/$service"

            $primaryRef = "${baseName}:$($imageTags[0])"
            Write-Log -Level "STEP" -Message "Building $primaryRef ..."
            docker build -t $primaryRef -f $dockerfilePath $contextPath
            if ($LASTEXITCODE -ne 0) {
                throw "Docker build failed for $primaryRef"
            }

            Write-Log -Level "STEP" -Message "Pushing $primaryRef ..."
            docker push $primaryRef
            if ($LASTEXITCODE -ne 0) {
                throw "Docker push failed for $primaryRef"
            }

            for ($ti = 1; $ti -lt $imageTags.Count; $ti++) {
                $aliasRef = "${baseName}:$($imageTags[$ti])"
                Write-Log -Level "STEP" -Message "Tagging and pushing $aliasRef ..."
                docker tag $primaryRef $aliasRef
                if ($LASTEXITCODE -ne 0) {
                    throw "Docker tag failed: $primaryRef -> $aliasRef"
                }
                docker push $aliasRef
                if ($LASTEXITCODE -ne 0) {
                    throw "Docker push failed for $aliasRef"
                }
            }
        }
    }
    finally {
        docker logout $registryUrl 2>&1 | Out-Null
    }

    Write-Log -Level "OK" -Message "  Docker push completed."
    $shared | Add-Member -NotePropertyName publishCompleted -NotePropertyValue $true -Force
}

Export-ModuleMember -Function Invoke-Plugin
