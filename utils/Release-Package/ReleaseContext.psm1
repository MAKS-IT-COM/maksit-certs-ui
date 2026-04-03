#requires -Version 7.0
#requires -PSEdition Core

<#
.SYNOPSIS
    Helpers to resolve release semver from plugin configuration.

.DESCRIPTION
    Used by New-EngineContext and the DotNetReleaseVersion plugin:
    - Source: DotNetReleaseVersion plugin -> projectFiles
    - Version from first path in projectFiles (SDK-style .csproj <Version>)
#>

if (-not (Get-Command Write-Log -ErrorAction SilentlyContinue)) {
    $loggingModulePath = Join-Path (Split-Path $PSScriptRoot -Parent) "Logging.psm1"
    if (Test-Path $loggingModulePath -PathType Leaf) {
        Import-Module $loggingModulePath -Force
    }
}

function Resolve-RelativePaths {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Value,

        [Parameter(Mandatory = $true)]
        [string]$BasePath
    )

    if ($null -eq $Value) {
        return @()
    }

    $rawPaths = @()
    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
        $rawPaths += $Value
    }
    else {
        $rawPaths += $Value
    }

    $resolved = @()
    foreach ($p in $rawPaths) {
        if ([string]::IsNullOrWhiteSpace([string]$p)) {
            continue
        }

        $resolved += [System.IO.Path]::GetFullPath((Join-Path $BasePath ([string]$p)))
    }

    return @($resolved)
}

function Get-CsprojPropertyValue {
    param(
        [Parameter(Mandatory = $true)]
        [xml]$Csproj,

        [Parameter(Mandatory = $true)]
        [string]$PropertyName
    )

    # SDK-style .csproj files can have multiple PropertyGroup nodes.
    # Use the first group that defines the requested property.
    $propNode = $Csproj.Project.PropertyGroup |
        Where-Object { $_.$PropertyName } |
        Select-Object -First 1

    if ($propNode) {
        return $propNode.$PropertyName
    }

    return $null
}

function Get-CsprojVersions {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$ProjectFiles
    )

    Write-Log -Level "INFO" -Message "Reading version(s) from SDK-style project files (projectFiles)..."
    $projectVersions = @{}

    foreach ($projectPath in $ProjectFiles) {
        if (-not (Test-Path $projectPath -PathType Leaf)) {
            Write-Error "Project file not found at: $projectPath"
            exit 1
        }

        if ([System.IO.Path]::GetExtension($projectPath) -ne ".csproj") {
            Write-Error "Configured project file is not a .csproj file: $projectPath"
            exit 1
        }

        [xml]$csproj = Get-Content $projectPath
        $version = Get-CsprojPropertyValue -Csproj $csproj -PropertyName "Version"

        if (-not $version) {
            Write-Error "Version not found in $projectPath"
            exit 1
        }

        $projectVersions[$projectPath] = $version
        Write-Log -Level "OK" -Message "  $([System.IO.Path]::GetFileName($projectPath)): $version"
    }

    return $projectVersions
}

function Resolve-DotNetReleaseVersion {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Plugins,

        [Parameter(Mandatory = $true)]
        [string]$ScriptDir
    )

    $releaseVersionPlugin = @($Plugins | Where-Object { $_.name -eq 'DotNetReleaseVersion' } | Select-Object -First 1)
    if ($releaseVersionPlugin.Count -eq 0 -or $null -eq $releaseVersionPlugin[0]) {
        Write-Error "Configure a DotNetReleaseVersion plugin in scriptsettings.json with projectFiles."
        exit 1
    }

    $releaseVersionSettings = $releaseVersionPlugin[0]
    $projectFiles = @(Resolve-RelativePaths -Value $releaseVersionSettings.projectFiles -BasePath $ScriptDir)

    if ($projectFiles.Count -eq 0) {
        Write-Error "Configure release version via DotNetReleaseVersion.projectFiles (first .csproj with <Version>)."
        exit 1
    }

    $projectVersions = Get-CsprojVersions -ProjectFiles $projectFiles
    $version = $projectVersions[$projectFiles[0]]

    return [pscustomobject]@{
        version = $version
    }
}

Export-ModuleMember -Function Get-CsprojPropertyValue, Get-CsprojVersions, Resolve-RelativePaths, Resolve-DotNetReleaseVersion



