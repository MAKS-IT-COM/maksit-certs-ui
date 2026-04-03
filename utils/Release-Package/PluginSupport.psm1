#requires -Version 7.0
#requires -PSEdition Core

if (-not (Get-Command Write-Log -ErrorAction SilentlyContinue)) {
    $loggingModulePath = Join-Path (Split-Path $PSScriptRoot -Parent) "Logging.psm1"
    if (Test-Path $loggingModulePath -PathType Leaf) {
        Import-Module $loggingModulePath -Force
    }
}

function Import-PluginDependency {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ModuleName,

        [Parameter(Mandatory = $true)]
        [string]$RequiredCommand
    )

    if (Get-Command $RequiredCommand -ErrorAction SilentlyContinue) {
        return
    }

    $moduleRoot = Split-Path $PSScriptRoot -Parent
    $modulePath = Join-Path $moduleRoot "$ModuleName.psm1"
    if (Test-Path $modulePath -PathType Leaf) {
        # Import into the global session so the calling plugin can see the exported commands.
        # Importing only into this module's scope would make the dependency invisible to the plugin.
        Import-Module $modulePath -Force -Global -ErrorAction Stop
    }

    if (-not (Get-Command $RequiredCommand -ErrorAction SilentlyContinue)) {
        throw "Required command '$RequiredCommand' is still unavailable after importing module '$ModuleName'."
    }
}

function Get-ConfiguredPlugins {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Settings
    )

    if (-not $Settings.PSObject.Properties['plugins'] -or $null -eq $Settings.plugins) {
        return @()
    }

    # JSON can deserialize a single plugin as one object or multiple plugins as an array.
    # Always return an array so the engine can loop without special-case logic.
    if ($Settings.plugins -is [System.Collections.IEnumerable] -and -not ($Settings.plugins -is [string])) {
        return @($Settings.plugins)
    }

    return @($Settings.plugins)
}

function Get-PluginStageLabel {
    param(
        [Parameter(Mandatory = $true)]
        $Plugin
    )

    if (-not $Plugin.PSObject.Properties['stageLabel'] -or [string]::IsNullOrWhiteSpace([string]$Plugin.stageLabel)) {
        return 'release'
    }

    return [string]$Plugin.stageLabel
}

function Get-PluginBranches {
    param(
        [Parameter(Mandatory = $true)]
        $Plugin
    )

    if (-not $Plugin.PSObject.Properties['branches'] -or $null -eq $Plugin.branches) {
        return @()
    }

    # Strings are also IEnumerable in PowerShell, so exclude them or we would split into characters.
    if ($Plugin.branches -is [System.Collections.IEnumerable] -and -not ($Plugin.branches -is [string])) {
        return @($Plugin.branches | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }

    if ([string]::IsNullOrWhiteSpace([string]$Plugin.branches)) {
        return @()
    }

    return @([string]$Plugin.branches)
}

function Test-PluginAllowedOnBranch {
    param(
        [Parameter(Mandatory = $true)]
        $Plugin,

        [Parameter(Mandatory = $true)]
        [string]$CurrentBranch
    )

    $allowedBranches = Get-PluginBranches -Plugin $Plugin
    if ($allowedBranches.Count -eq 0) {
        return $true
    }

    if ($allowedBranches -contains '*') {
        return $true
    }

    return $allowedBranches -contains $CurrentBranch
}

function Test-IsPublishPlugin {
    param(
        [Parameter(Mandatory = $true)]
        $Plugin
    )

    if ($null -eq $Plugin -or [string]::IsNullOrWhiteSpace([string]$Plugin.name)) {
        return $false
    }

    return @('GitHub', 'DotNetNuGet', 'DockerPush', 'HelmPush') -contains ([string]$Plugin.name)
}

function Get-PluginSettingValue {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Plugins,

        [Parameter(Mandatory = $true)]
        [string]$PropertyName
    )

    foreach ($plugin in $Plugins) {
        if ($null -eq $plugin -or [string]::IsNullOrWhiteSpace($plugin.name)) {
            continue
        }

        if (-not $plugin.PSObject.Properties[$PropertyName]) {
            continue
        }

        $value = $plugin.$PropertyName
        if ($null -eq $value) {
            continue
        }

        if ($value -is [string] -and [string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        return $value
    }

    return $null
}

function Get-PluginPathListSetting {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Plugins,

        [Parameter(Mandatory = $true)]
        [string]$PropertyName,

        [Parameter(Mandatory = $true)]
        [string]$BasePath
    )

    $rawPaths = @()
    $value = Get-PluginSettingValue -Plugins $Plugins -PropertyName $PropertyName

    if ($null -eq $value) {
        return @()
    }

    # Same rule as above: treat a string as one path, not a char-by-char sequence.
    if ($value -is [System.Collections.IEnumerable] -and -not ($value -is [string])) {
        $rawPaths += $value
    }
    else {
        $rawPaths += $value
    }

    $resolvedPaths = @()
    foreach ($path in $rawPaths) {
        if ([string]::IsNullOrWhiteSpace([string]$path)) {
            continue
        }

        $resolvedPaths += [System.IO.Path]::GetFullPath((Join-Path $BasePath ([string]$path)))
    }

    # Wrap again to stop PowerShell from unrolling a single-item array into a bare string.
    return @($resolvedPaths)
}

function Get-PluginPathSetting {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Plugins,

        [Parameter(Mandatory = $true)]
        [string]$PropertyName,

        [Parameter(Mandatory = $true)]
        [string]$BasePath
    )

    $value = Get-PluginSettingValue -Plugins $Plugins -PropertyName $PropertyName
    if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) {
        return $null
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath ([string]$value)))
}

function Get-ArchiveNamePattern {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Plugins,

        [Parameter(Mandatory = $true)]
        [string]$CurrentBranch
    )

    foreach ($plugin in $Plugins) {
        if ($null -eq $plugin -or [string]::IsNullOrWhiteSpace($plugin.name)) {
            continue
        }

        if (-not $plugin.enabled) {
            continue
        }

        if (-not (Test-PluginAllowedOnBranch -Plugin $plugin -CurrentBranch $CurrentBranch)) {
            continue
        }

        if ($plugin.PSObject.Properties['zipNamePattern'] -and -not [string]::IsNullOrWhiteSpace([string]$plugin.zipNamePattern)) {
            return [string]$plugin.zipNamePattern
        }
    }

    return "release-{version}.zip"
}

function Resolve-PluginModulePath {
    param(
        [Parameter(Mandatory = $true)]
        $Plugin,

        [Parameter(Mandatory = $true)]
        [string]$PluginsDirectory
    )

    $pluginFileName = "{0}.psm1" -f $Plugin.name
    $candidatePaths = @(
        (Join-Path $PluginsDirectory $pluginFileName),
        (Join-Path (Join-Path (Split-Path $PluginsDirectory -Parent) "CustomPlugins") $pluginFileName)
    )

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path $candidatePath -PathType Leaf) {
            return $candidatePath
        }
    }

    return $candidatePaths[0]
}

function Test-PluginRunnable {
    param(
        [Parameter(Mandatory = $true)]
        $Plugin,

        [Parameter(Mandatory = $true)]
        [psobject]$SharedSettings,

        [Parameter(Mandatory = $true)]
        [string]$PluginsDirectory,

        [Parameter(Mandatory = $false)]
        [bool]$WriteLogs = $true
    )

    if ($null -eq $Plugin -or [string]::IsNullOrWhiteSpace($Plugin.name)) {
        if ($WriteLogs) {
            Write-Log -Level "WARN" -Message "Skipping plugin entry with no name."
        }
        return $false
    }

    if (-not $Plugin.enabled) {
        if ($WriteLogs) {
            Write-Log -Level "WARN" -Message "Skipping plugin '$($Plugin.name)' (disabled)."
        }
        return $false
    }

    $pluginModulePath = Resolve-PluginModulePath -Plugin $Plugin -PluginsDirectory $PluginsDirectory
    if (-not (Test-Path $pluginModulePath -PathType Leaf)) {
        if ($WriteLogs) {
            Write-Log -Level "ERROR" -Message "Plugin module not found: $pluginModulePath"
        }
        return $false
    }

    return $true
}

function New-PluginInvocationSettings {
    param(
        [Parameter(Mandatory = $true)]
        $Plugin,

        [Parameter(Mandatory = $true)]
        [psobject]$SharedSettings
    )

    $properties = @{}
    foreach ($property in $Plugin.PSObject.Properties) {
        $properties[$property.Name] = $property.Value
    }

    # Plugins receive their own config plus shared runtime context.
    $properties['context'] = $SharedSettings
    return [pscustomobject]$properties
}

function Invoke-ConfiguredPlugin {
    param(
        [Parameter(Mandatory = $true)]
        $Plugin,

        [Parameter(Mandatory = $true)]
        [psobject]$SharedSettings,

        [Parameter(Mandatory = $true)]
        [string]$PluginsDirectory,

        [Parameter(Mandatory = $false)]
        [bool]$ContinueOnError = $true
    )

    if (-not (Test-PluginRunnable -Plugin $Plugin -SharedSettings $SharedSettings -PluginsDirectory $PluginsDirectory -WriteLogs:$true)) {
        return
    }

    if ((Test-IsPublishPlugin -Plugin $Plugin) -and ($SharedSettings.PSObject.Properties.Name -contains 'skipPublishPlugins') -and $SharedSettings.skipPublishPlugins) {
        Write-Log -Level "INFO" -Message "Skipping plugin '$($Plugin.name)' (ReleasePublishGuard suppressed publish)."
        return
    }

    $pluginModulePath = Resolve-PluginModulePath -Plugin $Plugin -PluginsDirectory $PluginsDirectory
    Write-Log -Level "STEP" -Message "Running plugin '$($Plugin.name)'..."

    try {
        $moduleInfo = Import-Module $pluginModulePath -Force -PassThru -ErrorAction Stop
        # Resolve Invoke-Plugin from the imported module explicitly so we call the plugin we just loaded,
        # not some command with the same name from another module already in session.
        $invokeCommand = Get-Command -Name "Invoke-Plugin" -Module $moduleInfo.Name -ErrorAction Stop
        $pluginSettings = New-PluginInvocationSettings -Plugin $Plugin -SharedSettings $SharedSettings

        & $invokeCommand -Settings $pluginSettings
        Write-Log -Level "OK" -Message "  Plugin '$($Plugin.name)' completed."
    }
    catch {
        Write-Log -Level "ERROR" -Message "  Plugin '$($Plugin.name)' failed: $($_.Exception.Message)"
        if (-not $ContinueOnError) {
            exit 1
        }
    }
}

Export-ModuleMember -Function Import-PluginDependency, Get-ConfiguredPlugins, Get-PluginStageLabel, Get-PluginBranches, Test-IsPublishPlugin, Get-PluginSettingValue, Get-PluginPathListSetting, Get-PluginPathSetting, Get-ArchiveNamePattern, Resolve-PluginModulePath, Test-PluginRunnable, New-PluginInvocationSettings, Invoke-ConfiguredPlugin
