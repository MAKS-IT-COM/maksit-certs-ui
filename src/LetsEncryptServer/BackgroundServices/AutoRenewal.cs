using Microsoft.Extensions.Options;


using MaksIT.LetsEncryptServer.Services;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Results;

namespace MaksIT.LetsEncryptServer.BackgroundServices {
  public class AutoRenewal : BackgroundService {

    private readonly IOptions<Configuration> _appSettings;
    private readonly ILogger<AutoRenewal> _logger;
    private readonly ICacheService _cacheService;
    private readonly ICertsInternalService _certsFlowService;

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
      
      var hostnames = cache.GetHostsWithUpcomingSslExpiry();
      if (hostnames == null) {
        _logger.LogError("Unexpected hostnames null");
        return Result.Ok();
      }


      if (!hostnames.Any()) {
        _logger.LogInformation("No hosts found with upcoming SSL expiry");
        return Result.Ok();
      }

      var fullFlowResult = await _certsFlowService.FullFlow(cache.IsStaging, cache.AccountId, cache.Description, cache.Contacts, cache.ChallengeType, hostnames);
      if (!fullFlowResult.IsSuccess)
        return fullFlowResult;

      _logger.LogInformation($"Certificates renewed for account {cache.AccountId}");

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
