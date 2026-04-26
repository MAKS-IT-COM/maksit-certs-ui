# Release-Package

Plugin-driven release engine. Run `Release-Package.ps1` from this directory (or `Release-Package.bat`). Configuration: `scriptsettings.json` (see `_comments` for plugin keys).

Canonical source: this folder in **maksit-repoutils**. Product repositories refresh via `Update-RepoUtils` or by copying from here.

## Modules (orchestration)

| File | Role |
|------|------|
| `Release-Package.ps1` | Loads settings, builds `New-EngineContext`, runs plugins in order. |
| `PluginSupport.psm1` | Plugin discovery, `Invoke-ConfiguredPlugin`; publish plugins honor `skipPublishPlugins` from `ReleasePublishGuard` (no per-plugin `branches` on Docker/Helm/GitHub/NuGet). |
| `ReleaseContext.psm1` | Resolves semver via `Resolve-DotNetReleaseVersion` from the `DotNetReleaseVersion` plugin `projectFiles` (first `.csproj` `<Version>`). |
| `EngineSupport.psm1` | Warn-only dirty-tree preflight; default `context.tag` = `v{version}`; `Initialize-ReleaseStageContext` sets `releaseDir` only. |

## Plugins

`CorePlugins/` — e.g. `DotNetReleaseVersion`, `DockerPush`, `HelmPush`, `ReleasePublishGuard`. Optional `CustomPlugins/`.

`DotNetPack` and `QualityGate` (when used) can declare their own `projectFiles`; semver still comes only from `DotNetReleaseVersion.projectFiles`.

## `ReleasePublishGuard`

Configure this plugin **immediately before** `DockerPush`, `HelmPush`, `GitHub`, and `DotNetNuGet`. It sets shared `skipPublishPlugins` when branch/tag rules fail (`whenRequirementsNotMet`: `skip` or `fail`). Those publish plugins no longer use their own `branches` key — list allowed branches on the guard only. Preflight does not read git tags; the guard sets `context.tag` from `HEAD` when `requireExactTagOnHead` is true. **`context.version` always stays from `DotNetReleaseVersion`** (the guard does not override it).

## `DotNetTest` and shared context

`DotNetTest` runs once and writes aggregated coverage and test metrics on the shared engine context (`qualityLineCoverage`, `coverageLineRate`, `testResult`, …). `QualityGate` reads those values for optional line-coverage thresholds; it does not re-run tests. Set `scanVulnerabilities` to false to skip `dotnet list package --vulnerable`.

## Helm charts in git

Commit `Chart.yaml` with placeholder `version` and `appVersion` (for example `0.0.0`) so `helm lint` stays valid. `HelmPush` temporarily replaces both with the release semver (same as the git tag / `DotNetReleaseVersion`) before packaging, then restores the file. `DockerPush` image tags come from the engine context (not from chart files), and you can optionally set per-image `versionEnvFiles` to temporarily write `VITE_APP_VERSION={shared.version}` into frontend `.env` files during build, with automatic restore after push.

This repository uses `src/helm/`. For a minimal scaffold chart, see **maksit-repoutils** `charts/my-service/`.


