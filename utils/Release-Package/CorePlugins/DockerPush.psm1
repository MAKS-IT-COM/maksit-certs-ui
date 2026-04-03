#requires -Version 7.0
#requires -PSEdition Core

<#
.SYNOPSIS
    Build and push Docker images to a container registry.

.DESCRIPTION
    Logs in with credentials from a Base64-encoded username:password environment variable,
    builds each configured image from the Docker context path, and pushes tags derived from
    the shared release context (exact git tag on HEAD, plus optional latest).

    Release image tags come from the shared engine context (git tag / DotNetReleaseVersion), not from
    Chart.yaml (chart versions in git are placeholders until HelmPush rewrites them for packaging).
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
    $versionTag = [string]$shared.Tag
    $pushLatest = if ($null -ne $pluginSettings.pushLatest) { [bool]$pluginSettings.pushLatest } else { $true }

    $tags = @($versionTag)
    if ($pushLatest) {
        $tags += 'latest'
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

            foreach ($t in $tags) {
                $imageRef = "${baseName}:$t"
                Write-Log -Level "STEP" -Message "Building $imageRef ..."
                docker build -t $imageRef -f $dockerfilePath $contextPath
                if ($LASTEXITCODE -ne 0) {
                    throw "Docker build failed for $imageRef"
                }

                Write-Log -Level "STEP" -Message "Pushing $imageRef ..."
                docker push $imageRef
                if ($LASTEXITCODE -ne 0) {
                    throw "Docker push failed for $imageRef"
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
