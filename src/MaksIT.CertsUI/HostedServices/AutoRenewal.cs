using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.CertsUI.Engine.Infrastructure;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using MaksIT.CertsUI.Services;

namespace MaksIT.CertsUI.HostedServices;

/// <summary>Certificate renewal: each sweep acquires <see cref="RuntimeLeaseNames.RenewalSweep"/> so only one pod runs ACME renewal at a time (symmetric replicas, no elected primary).</summary>
public sealed class AutoRenewal(
  ILogger<AutoRenewal> logger,
  IServiceScopeFactory scopeFactory,
  IRuntimeLeaseService leaseService,
  IRuntimeInstanceId runtimeInstance
) : BackgroundService {

  private static readonly TimeSpan RenewalLeaseTtl = TimeSpan.FromMinutes(12);
  private static readonly Random Random = new();

  protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
    while (!stoppingToken.IsCancellationRequested) {
      var holder = runtimeInstance.InstanceId;
      var acquired = await leaseService.TryAcquireAsync(RuntimeLeaseNames.RenewalSweep, holder, RenewalLeaseTtl, stoppingToken).ConfigureAwait(false);
      if (!acquired.IsSuccess) {
        if (logger.IsEnabled(LogLevel.Warning))
          logger.LogWarning("Renewal sweep lease check failed: {Messages}", string.Join("; ", acquired.Messages ?? []));
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
        continue;
      }

      if (!acquired.Value) {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
        continue;
      }

      try {
        if (logger.IsEnabled(LogLevel.Information))
          logger.LogInformation("Running certificate renewal sweep (lease holder {Holder}).", holder);

        using var scope = scopeFactory.CreateScope();
        var registrationCacheDomain = scope.ServiceProvider.GetRequiredService<IRegistrationCacheDomainService>();
        var certsFlowDomain = scope.ServiceProvider.GetRequiredService<ICertsFlowDomainService>();
        var certsFlowService = scope.ServiceProvider.GetRequiredService<ICertsFlowService>();

        var purge = await certsFlowDomain.PurgeStaleHttpChallengesAsync(TimeSpan.FromDays(10), stoppingToken).ConfigureAwait(false);
        if (purge.IsSuccess && purge.Value > 0)
          logger.LogInformation("Purged {Count} HTTP-01 challenge row(s) older than 10 days.", purge.Value);

        var purgeSessions = await certsFlowDomain.PurgeExpiredAcmeSessionsAsync(stoppingToken).ConfigureAwait(false);
        if (purgeSessions.IsSuccess && purgeSessions.Value > 0)
          logger.LogInformation("Purged {Count} expired ACME session row(s).", purgeSessions.Value);

        var loadAccountsFromCacheResult = await registrationCacheDomain.LoadAllAsync(stoppingToken).ConfigureAwait(false);
        if (!loadAccountsFromCacheResult.IsSuccess || loadAccountsFromCacheResult.Value == null) {
          LogErrorMessages(loadAccountsFromCacheResult.Messages);
          await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
          continue;
        }

        var accountsResponse = loadAccountsFromCacheResult.Value;
        foreach (var account in accountsResponse.Where(x => !x.IsDisabled))
          await ProcessAccountAsync(certsFlowService, account).ConfigureAwait(false);
      }
      finally {
        var released = await leaseService.ReleaseAsync(RuntimeLeaseNames.RenewalSweep, holder, CancellationToken.None).ConfigureAwait(false);
        if (!released.IsSuccess && logger.IsEnabled(LogLevel.Warning))
          logger.LogWarning("Renewal sweep lease release: {Messages}", string.Join("; ", released.Messages ?? []));
      }

      await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
    }
  }

  private async Task ProcessAccountAsync(ICertsFlowService certsFlowService, RegistrationCache cache) {
    var hosts = cache.GetHosts();
    var toRenew = new List<string>();

    foreach (var host in hosts) {
      if (host.IsDisabled)
        continue;

      if ((host.Expires - DateTime.UtcNow).TotalDays < 30) {
        int randomDays = Random.Next(1, 6);
        var renewalTime = host.Expires.AddDays(-randomDays);
        if (DateTime.UtcNow >= renewalTime)
          toRenew.Add(host.Hostname);
      }
    }

    if (!toRenew.Any()) {
      logger.LogInformation("No certificates are due for randomized renewal at this time for account {AccountId}.", cache.AccountId);
      return;
    }

    var cooldownSkipped = new List<(string Hostname, DateTimeOffset NotBeforeUtc)>();
    var eligible = new List<string>();
    foreach (var hostname in toRenew) {
      if (cache.IsHostnameInAcmeCooldown(hostname, out var notBefore)) {
        cooldownSkipped.Add((hostname, notBefore));
        continue;
      }
      eligible.Add(hostname);
    }

    if (cooldownSkipped.Count > 0) {
      var sample = cooldownSkipped[0];
      logger.LogInformation(
        "Skipping {SkippedCount} hostname(s) in ACME cooldown for account {AccountId} (e.g. {ExampleHost} until {NotBeforeUtc:u} UTC).",
        cooldownSkipped.Count, cache.AccountId, sample.Hostname, sample.NotBeforeUtc);
    }

    if (!eligible.Any()) {
      logger.LogInformation("All due certificates for account {AccountId} are in ACME cooldown; no renewal attempted.", cache.AccountId);
      return;
    }

    var fullFlowResult = await certsFlowService.FullFlow(
      cache.IsStaging, cache.AccountId, cache.Description, cache.Contacts, cache.ChallengeType, eligible.ToArray()
    ).ConfigureAwait(false);

    if (!fullFlowResult.IsSuccess)
      LogErrorMessages(fullFlowResult.Messages);
    else
      logger.LogInformation("Certificates renewed for account {AccountId}: {Hostnames}", cache.AccountId, string.Join(", ", eligible));
  }

  private void LogErrorMessages(IEnumerable<string>? errors) {
    if (errors == null)
      return;
    foreach (var error in errors)
      logger.LogError("{Error}", error);
  }

  public override Task StopAsync(CancellationToken stoppingToken) {
    logger.LogInformation("Background service is stopping.");
    return base.StopAsync(stoppingToken);
  }
}
