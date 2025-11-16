# Set variables
$projectName = "certs-ui"
$namespace = "certs-ui"
$chartPath = "./helm"
$harborUrl = "cr.maks-it.com"

# Retrieve and decode username:password from environment variable (Base64)
try {
    $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Env:CR_MAKS_IT))
} catch {
    Write-Error "Failed to decode CR_MAKS_IT as Base64. Expected base64('username:password')."
    exit 1
}

# Split decoded credentials
$creds = $decoded -split ':', 2
$harborUsername = $creds[0]
$harborPassword = $creds[1]

# Verify environment variable
if (-not $harborUsername -or -not $harborPassword) {
    Write-Error "Decoded CR_MAKS_IT must be in the format 'username:password'."
    exit 1
}

# Ensure namespace exists
if (-not (kubectl get ns $namespace -o name 2>$null)) {
    Write-Output "Creating namespace '$namespace'..."
    kubectl create namespace $namespace | Out-Null
}
else {
    Write-Output "Namespace '$namespace' already exists."
}

# Create or update Docker registry pull secret
Write-Output "Creating or updating image pull secret..."
kubectl -n $namespace create secret docker-registry cr-maksit-pull `
    --docker-server=$harborUrl `
    --docker-username=$harborUsername `
    --docker-password=$harborPassword `
    --docker-email="devnull@maks-it.com" `
    --dry-run=client -o yaml | kubectl apply -f - | Out-Null

# Lint Helm chart
Write-Output "Linting Helm chart..."
helm lint $chartPath

# Render Helm chart to verify output (optional)
Write-Output "Rendering Helm chart for validation..."
helm template $projectName $chartPath -n $namespace | Out-Null

# Generate a unique rollout value (current Unix timestamp)
$rollme = [int][double]::Parse((Get-Date -UFormat %s))

# Deploy Helm release
Write-Output "Deploying Helm release '$projectName'..."
helm upgrade --install $projectName $chartPath -n $namespace `
    --set imagePullSecret.create=false `
    --set imagePullSecrets[0].name=cr-maksit-pull `
    --set-string "rollme=$rollme"

# Check deployment status
Write-Output "Waiting for deployment rollout..."
kubectl -n $namespace rollout status deployment/$projectName-reverseproxy

# Display service details
Write-Output "Service information:"
kubectl -n $namespace get svc $projectName-reverseproxy
