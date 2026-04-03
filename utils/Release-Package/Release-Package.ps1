#requires -Version 7.0
#requires -PSEdition Core

<#
.SYNOPSIS
    Plugin-driven release engine.

.DESCRIPTION
    This script is the orchestration layer for release automation.
    It loads scriptsettings.json, evaluates the configured plugins in order,
    builds shared execution context, and invokes each plugin's Invoke-Plugin
    entrypoint with that plugin's own settings object plus runtime context.

    The engine is intentionally generic:
    - It does not embed release-provider-specific logic
    - It preserves plugin execution order from scriptsettings.json
    - It isolates plugin failures according to the stage/runtime policy
    - It keeps shared orchestration helpers in dedicated support modules

.REQUIREMENTS
    Tools (Required):
    - Shared support modules required by the engine
    - Any commands required by configured plugins or support helpers

.WORKFLOW
    1. Load and normalize plugin configuration
    2. Determine branch mode from configured plugin metadata
    3. Validate repository state and resolve the release version
    4. Build shared execution context
    5. Execute plugins one by one in configured order
    6. Initialize release-stage shared artifacts only when needed
    7. Report completion summary

.USAGE
    Configure plugin order and plugin settings in scriptsettings.json, then run:
        pwsh -File .\Release-Package.ps1

.CONFIGURATION
    All settings are stored in scriptsettings.json:
    - plugins: Ordered plugin definitions and plugin-specific settings

.NOTES
    Plugin-specific behavior belongs in the plugin modules, not in this engine.
#>

# No parameters - behavior is controlled by configured plugin metadata:
# - ReleasePublishGuard (before Docker/Helm/GitHub/NuGet): optional branches, tag on HEAD, remote tag; sets skipPublishPlugins.
# - Publish plugins do not use per-plugin "branches"; centralize allowed branches on the guard.

# Get the directory of the current script (for loading settings and relative paths)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

#region Import Modules

$utilsDir = Split-Path $scriptDir -Parent

# Import ScriptConfig module
$scriptConfigModulePath = Join-Path $utilsDir "ScriptConfig.psm1"
if (-not (Test-Path $scriptConfigModulePath)) {
    Write-Error "ScriptConfig module not found at: $scriptConfigModulePath"
    exit 1
}

Import-Module $scriptConfigModulePath -Force

# Import Logging module
$loggingModulePath = Join-Path $utilsDir "Logging.psm1"
if (-not (Test-Path $loggingModulePath)) {
    Write-Error "Logging module not found at: $loggingModulePath"
    exit 1
}

Import-Module $loggingModulePath -Force
# Import PluginSupport module
$pluginSupportModulePath = Join-Path $scriptDir "PluginSupport.psm1"
if (-not (Test-Path $pluginSupportModulePath)) {
    Write-Error "PluginSupport module not found at: $pluginSupportModulePath"
    exit 1
}

Import-Module $pluginSupportModulePath -Force

# Import ReleaseContext module (semver resolution for the engine)
$releaseContextModulePath = Join-Path $scriptDir "ReleaseContext.psm1"
if (-not (Test-Path $releaseContextModulePath)) {
    Write-Error "ReleaseContext module not found at: $releaseContextModulePath"
    exit 1
}

Import-Module $releaseContextModulePath -Force

# Import EngineSupport module
$engineSupportModulePath = Join-Path $scriptDir "EngineSupport.psm1"
if (-not (Test-Path $engineSupportModulePath)) {
    Write-Error "EngineSupport module not found at: $engineSupportModulePath"
    exit 1
}

Import-Module $engineSupportModulePath -Force

#endregion

#region Load Settings
$settings = Get-ScriptSettings -ScriptDir $scriptDir
$configuredPlugins = Get-ConfiguredPlugins -Settings $settings

#endregion

#region Configuration

$pluginsDir = Join-Path $scriptDir "CorePlugins"

#endregion

#endregion

#region Main

Write-Log -Level "STEP" -Message "=================================================="
Write-Log -Level "STEP" -Message "RELEASE ENGINE"
Write-Log -Level "STEP" -Message "=================================================="

#region Preflight

$plugins = $configuredPlugins
$engineContext = New-EngineContext -Plugins $plugins -ScriptDir $scriptDir -UtilsDir $utilsDir -Settings $settings
Write-Log -Level "OK" -Message "All pre-flight checks passed!"
$sharedPluginSettings = $engineContext

#endregion

#region Plugin Execution

$releaseStageInitialized = $false

if ($plugins.Count -eq 0) {
    Write-Log -Level "WARN" -Message "No plugins configured in scriptsettings.json."
}
else {
    for ($pluginIndex = 0; $pluginIndex -lt $plugins.Count; $pluginIndex++) {
        $plugin = $plugins[$pluginIndex]
        $pluginStageLabel = Get-PluginStageLabel -Plugin $plugin

        if ((Test-IsPublishPlugin -Plugin $plugin) -and -not $releaseStageInitialized) {
            if (Test-PluginRunnable -Plugin $plugin -SharedSettings $sharedPluginSettings -PluginsDirectory $pluginsDir -WriteLogs:$false) {
                $remainingPlugins = @($plugins[$pluginIndex..($plugins.Count - 1)])
                Initialize-ReleaseStageContext -RemainingPlugins $remainingPlugins -SharedSettings $sharedPluginSettings -ArtifactsDirectory $engineContext.artifactsDirectory -Version $engineContext.version
                $releaseStageInitialized = $true
            }
        }

        $continueOnError = $pluginStageLabel -eq "release"
        Invoke-ConfiguredPlugin -Plugin $plugin -SharedSettings $sharedPluginSettings -PluginsDirectory $pluginsDir -ContinueOnError:$continueOnError
    }
}

if (-not $releaseStageInitialized) {
    $noReleasePluginsLogLevel = if ($engineContext.isNonReleaseBranch) { "INFO" } else { "WARN" }
    Write-Log -Level $noReleasePluginsLogLevel -Message "No release-stage initialization ran (no enabled publish plugins reached, or none runnable)."
}

#endregion

#region Summary
Write-Log -Level "OK" -Message "=================================================="
if ($engineContext.PSObject.Properties.Name -contains 'skipPublishPlugins' -and $engineContext.skipPublishPlugins) {
    Write-Log -Level "OK" -Message "RUN COMPLETE (publish skipped by ReleasePublishGuard)"
}
elseif ($engineContext.isNonReleaseBranch) {
    Write-Log -Level "OK" -Message "NON-RELEASE RUN COMPLETE"
}
else {
    Write-Log -Level "OK" -Message "RELEASE COMPLETE"
}
Write-Log -Level "OK" -Message "=================================================="

if ($engineContext.isNonReleaseBranch -and -not ($engineContext.PSObject.Properties.Name -contains 'skipPublishPlugins' -and $engineContext.skipPublishPlugins)) {
    $preferredReleaseBranch = Get-PreferredReleaseBranch -EngineContext $engineContext
    Write-Log -Level "INFO" -Message "For publish, use an allowed branch (see ReleasePublishGuard.branches), e.g. '$preferredReleaseBranch', and satisfy the guard requirements."
}

#endregion

#endregion

