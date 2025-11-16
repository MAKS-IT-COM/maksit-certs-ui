# HINT: To create a tag for the last commit in git, use:
#   git tag 1.2.3
#   git push origin 1.2.3
# Replace '1.2.3' with your desired tag name.

# Set variables
$projectName = "certs-ui"
$harborUrl = "cr.maks-it.com"  # e.g., "harbor.yourdomain.com"

# Ensure we are on main branch and up to date
git checkout main
git pull

# Get the latest tag reachable from main
$tag = git describe --tags --abbrev=0

if (-not $tag) {
    throw "No tags found on main branch."
}

$tags = @($tag, "latest")
Write-Output "Using tags: $($tags -join ', ')"

# Retrieve and decode username:password from environment variable (Base64 encoded)
try {
    $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Env:CR_MAKS_IT))
} catch {
    throw "Failed to decode CR_MAKS_IT as Base64. Ensure it's base64('username:password'). Error: $_"
}

# Split decoded credentials
$creds = $decoded -split ':', 2
if ($creds.Count -ne 2) {
    throw "Invalid decoded CR_MAKS_IT format. Expected 'username:password'."
}

$harborUsername = $creds[0]
$harborPassword = $creds[1]

# Authenticate with Harbor
Write-Output "Logging into $harborUrl as $harborUsername..."
$loginResult = $harborPassword | docker login $harborUrl -u $harborUsername --password-stdin 2>&1
if ($LASTEXITCODE -ne 0 -or ($loginResult -notmatch "Login Succeeded")) {
    throw "Docker login failed for $harborUrl.`n$loginResult"
}

# List of services to build and push with the current context
$services = @{
    "reverseproxy" = "ReverseProxy/Dockerfile"
    "server" = "MaksIT.Webapi/Dockerfile"
    "client" = "MaksIT.WebUI/Dockerfile.prod"
}

$contextPath = "."

foreach ($service in $services.Keys) {
    $dockerfilePath = $services[$service]
    $baseImageName = "$harborUrl/$projectName/${service}"

    foreach ($t in $tags) {
        $imageName = "$baseImageName`:$t"
        Write-Output "Building image $imageName from $dockerfilePath..."
        docker build -t $imageName -f $dockerfilePath $contextPath
        if ($LASTEXITCODE -ne 0) {
            throw "Docker build failed for $imageName"
        }

        Write-Output "Pushing image $imageName..."
        docker push $imageName
        if ($LASTEXITCODE -ne 0) {
            throw "Docker push failed for $imageName"
        }
    }
}

# --- Helm Chart Release Section ---
# Package the Helm chart
$chartDir = "helm"
$chartPackageOutput = helm package $chartDir
$chartPackage = $null
if ($chartPackageOutput -match "Successfully packaged chart and saved it to: (.+\.tgz)") {
    $chartPackage = $Matches[1]
}
if (-not $chartPackage) {
    throw "Helm chart packaging failed. Output: $chartPackageOutput"
}

# Push the Helm chart to the same Harbor project/repo as Docker images
$helmRepoUrl = "oci://$harborUrl/$projectName/charts"
Write-Output "Pushing Helm chart $chartPackage to $helmRepoUrl..."
helm push $chartPackage $helmRepoUrl --username $harborUsername --password $harborPassword
if ($LASTEXITCODE -ne 0) {
    throw "Helm chart push failed."
}

if ($chartPackage) {
    Remove-Item $chartPackage -Force
    Write-Output "Cleaned up $chartPackage"
}

docker logout $harborUrl | Out-Null
Write-Output "Completed successfully."


# Logout after pushing images
