# Set variables
$projectName = "certs-ui"
$harborUrl = "cr.maks-it.com"  # e.g., "harbor.yourdomain.com"
$tag = "latest"  # Customize the tag as needed

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
    "server" = "LetsEncryptServer/Dockerfile"
    "client" = "ClientApp/Dockerfile.prod"
}

$contextPath = "."

foreach ($service in $services.Keys) {
    $dockerfilePath = $services[$service]
    $imageName = "$harborUrl/$projectName/${service}:${tag}"
    
    # Build the Docker image
    Write-Output "Building image $imageName from $dockerfilePath..."
    docker build -t $imageName -f $dockerfilePath $contextPath
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed for $imageName"
    }

    # Push the Docker image
    Write-Output "Pushing image $imageName..."
    docker push $imageName
    if ($LASTEXITCODE -ne 0) {
        throw "Docker push failed for $imageName"
    }
}

# Logout after pushing images
docker logout $harborUrl | Out-Null
Write-Output "Completed successfully."
