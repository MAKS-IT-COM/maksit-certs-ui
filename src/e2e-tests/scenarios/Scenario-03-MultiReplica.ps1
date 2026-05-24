#Requires -Version 7

Register-CertsUiE2eScenario -Id 'MultiReplica' -Description 'Get-CertsUIRuntimeInstanceId returns instanceId; optional HA check when CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES > 1' -ScriptBlock {
  # Default 1: Docker Compose runs a single server container (YARP → one upstream).
  # Set CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES=2 (or higher) for k8s / scaled deployments.
  $minDistinct = 1
  $minDistinctRaw = [Environment]::GetEnvironmentVariable('CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES', 'Process')
  if ([string]::IsNullOrWhiteSpace($minDistinctRaw)) {
    $minDistinctRaw = [Environment]::GetEnvironmentVariable('CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES', 'User')
  }
  if ([string]::IsNullOrWhiteSpace($minDistinctRaw)) {
    $minDistinctRaw = [Environment]::GetEnvironmentVariable('CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES', 'Machine')
  }
  if (-not [string]::IsNullOrWhiteSpace($minDistinctRaw)) {
    $parsed = 0
    if ([int]::TryParse($minDistinctRaw.Trim(), [ref]$parsed) -and $parsed -gt 0) {
      $minDistinct = $parsed
    }
  }

  $requestCount = if ($minDistinct -le 1) { 8 } else { 32 }
  Write-E2eLog -Kind Step -Message "[MultiReplica] $requestCount Get-CertsUIRuntimeInstanceId calls; expect distinct instances >= $minDistinct"

  $instanceIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach ($i in 1..$requestCount) {
    $response = Get-CertsUIRuntimeInstanceId -ErrorAction Stop
    $id = [string]$response.InstanceId
    if ([string]::IsNullOrWhiteSpace($id)) {
      throw 'instanceId missing or empty in response'
    }
    $null = $instanceIds.Add($id)
  }

  $sorted = @($instanceIds | Sort-Object)
  Write-E2eLog -Message "Observed distinct instance ids ($($instanceIds.Count)): $($sorted -join ', ')"

  if ($instanceIds.Count -lt $minDistinct) {
    if ($minDistinct -gt 1) {
      throw (
        "Expected at least $minDistinct distinct instance ids, observed $($instanceIds.Count). " +
        'Scale server replicas and ensure non-sticky load balancing, or unset CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES for local Docker Compose.'
      )
    }
    throw 'Expected at least one instance id from Get-CertsUIRuntimeInstanceId.'
  }

  if ($minDistinct -le 1) {
    Write-E2eLog -Kind Ok -Message '[MultiReplica] PASSED (single-instance / local Compose mode)'
  }
  else {
    Write-E2eLog -Kind Ok -Message "[MultiReplica] PASSED ($($instanceIds.Count) distinct instances)"
  }
}
