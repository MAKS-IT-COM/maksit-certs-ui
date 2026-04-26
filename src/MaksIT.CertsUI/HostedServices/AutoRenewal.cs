using MaksIT.CertsUI.Engine.Domain.Certs;
using MaksIT.CertsUI.Engine.Persistance.Services;
using MaksIT.CertsUI.Engine.RuntimeCoordination;
using MaksIT.Results;
using MaksIT.CertsUI.Services;
using Microsoft.Extensions.Options;
using System;

namespace MaksIT.CertsUI.HostedServices {
  public class AutoRenewal : BackgroundService {

    private readonly IOptions<Configuration> _appSettings;
    private readonly ILogger<AutoRenewal> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPrimaryReplicaWorkload _primaryReplica;

    private static readonly Random _random = new();

    public AutoRenewal(
        IOptions<Configuration> appSettings,
        ILogger<AutoRenewal> logger,
        IServiceScopeFactory scopeFactory,
        IPrimaryReplicaWorkload primaryReplica
    ) {
      _appSettings = appSettings;
      _logger = logger;
      _scopeFactory = scopeFactory;
      _primaryReplica = primaryReplica;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      while (!stoppingToken.IsCancellationRequested) {
        if (!_primaryReplica.IsPrimary) {
          await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
          continue;
        }

        _logger.LogInformation("Background service is running (primary replica).");

        using var scope = _scopeFactory.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var certsFlowService = scope.ServiceProvider.GetRequiredService<ICertsFlowService>();
        var httpChallenges = scope.ServiceProvider.GetRequiredService<IAcmeHttpChallengePersistenceService>();

        var purge = await httpChallenges.DeleteOlderThanAsync(TimeSpan.FromDays(10), stoppingToken);
        if (purge.IsSuccess && purge.Value > 0)
          _logger.LogInformation("Purged {Count} HTTP-01 challenge row(s) older than 10 days.", purge.Value);

        var loadAccountsFromCacheResult = await cacheService.LoadAccountsFromCacheAsync();
        if (!loadAccountsFromCacheResult.IsSuccess || loadAccountsFromCacheResult.Value == null) {
          LogErrors(loadAccountsFromCacheResult.Messages);
          await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
          continue;
        }

        var accountsResponse = loadAccountsFromCacheResult.Value;

        foreach (var account in accountsResponse.Where(x => !x.IsDisabled)) {
          await ProcessAccountAsync(certsFlowService, account);
        }

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
      }
    }

    private async Task<Result> ProcessAccountAsync(ICertsFlowService certsFlowService, RegistrationCache cache) {

      var hosts = cache.GetHosts();
      var toRenew = new List<string>();

      foreach (var host in hosts) {
        if (host.IsDisabled)
          continue;

        // Only consider certs expiring within 30 days
        if ((host.Expires - DateTime.UtcNow).TotalDays < 30) {
          // Randomize renewal between 1 and 5 days before expiry
          int randomDays = _random.Next(1, 6);
          var renewalTime = host.Expires.AddDays(-randomDays);
          if (DateTime.UtcNow >= renewalTime) {
            toRenew.Add(host.Hostname);
          }
        }
      }

      if (!toRenew.Any()) {
        _logger.LogInformation("No certificates are due for randomized renewal at this time.");
        return Result.Ok();
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
        _logger.LogInformation(
          "Skipping {SkippedCount} hostname(s) in ACME cooldown for account {AccountId} (e.g. {ExampleHost} until {NotBeforeUtc:u} UTC).",
          cooldownSkipped.Count, cache.AccountId, sample.Hostname, sample.NotBeforeUtc);
      }

      if (!eligible.Any()) {
        _logger.LogInformation("All due certificates for account {AccountId} are in ACME cooldown; no renewal attempted.", cache.AccountId);
        return Result.Ok();
      }

      var fullFlowResult = await certsFlowService.FullFlow(
          cache.IsStaging, cache.AccountId, cache.Description, cache.Contacts, cache.ChallengeType, eligible.ToArray()
      );

      if (!fullFlowResult.IsSuccess)
        return fullFlowResult;

      _logger.LogInformation("Certificates renewed for account {AccountId}: {Hostnames}", cache.AccountId, string.Join(", ", eligible));

      return Result.Ok();
    }

    

    private void LogErrors(IEnumerable<string> errors) {
      foreach (var error in errors) {
        _logger.LogError(error);
      }
    }

    public override Task StopAsync(CancellationToken stoppingToken) {
      _logger.LogInformation("Background service is stopping.");
      return base.StopAsync(stoppingToken);
    }
  }
}
