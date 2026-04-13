using MaksIT.LetsEncrypt.Entities;
using MaksIT.Results;
using MaksIT.Webapi.Services;
using Microsoft.Extensions.Options;
using System;

namespace MaksIT.Webapi.BackgroundServices {
  public class AutoRenewal : BackgroundService {

    private readonly IOptions<Configuration> _appSettings;
    private readonly ILogger<AutoRenewal> _logger;
    private readonly ICacheService _cacheService;
    private readonly ICertsFlowService _certsFlowService;

    private static readonly Random _random = new();

    public AutoRenewal(
        IOptions<Configuration> appSettings,
        ILogger<AutoRenewal> logger,
        ICacheService cacheService,
        ICertsFlowService certsFlowService
    ) {
      _appSettings = appSettings;
      _logger = logger;
      _cacheService = cacheService;
      _certsFlowService = certsFlowService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
      while (!stoppingToken.IsCancellationRequested) {
        _logger.LogInformation("Background service is running.");

        var loadAccountsFromCacheResult = await _cacheService.LoadAccountsFromCacheAsync();
        if (!loadAccountsFromCacheResult.IsSuccess || loadAccountsFromCacheResult.Value == null) {
          LogErrors(loadAccountsFromCacheResult.Messages);
          continue;
        }

        var accountsResponse = loadAccountsFromCacheResult.Value;

        foreach (var account in accountsResponse.Where(x => !x.IsDisabled)) {
          await ProcessAccountAsync(account);
        }

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
      }
    }

    private async Task<Result> ProcessAccountAsync(RegistrationCache cache) {

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

      var fullFlowResult = await _certsFlowService.FullFlow(
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
