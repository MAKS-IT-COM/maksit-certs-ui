using Microsoft.Extensions.Options;

using DomainResults.Common;


using MaksIT.LetsEncryptServer.Services;
using MaksIT.LetsEncrypt.Entities;
using MaksIT.Models.LetsEncryptServer.CertsFlow.Requests;

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

        var (accountsResponse, getAccountIdsResult) = await _cacheService.LoadAccountsFromCacheAsync();
        if (!getAccountIdsResult.IsSuccess || accountsResponse == null) {
          LogErrors(getAccountIdsResult.Errors);
          continue;
        }

        foreach (var account in accountsResponse.Where(x => !x.IsDisabled)) {
          await ProcessAccountAsync(account);
        }

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
      }
    }

    private async Task<IDomainResult> ProcessAccountAsync(RegistrationCache cache) {
      
      var hostnames = cache.GetHostsWithUpcomingSslExpiry();
      if (hostnames == null) {
        _logger.LogError("Unexpected hostnames null");
        return IDomainResult.Success();
      }


      if (!hostnames.Any()) {
        _logger.LogInformation("No hosts found with upcoming SSL expiry");
        return IDomainResult.Success();
      }

      var (_, renewResult) = await _certsFlowService.FullFlow(cache.IsStaging, cache.AccountId, cache.Description, cache.Contacts, cache.ChallengeType, hostnames);
      if (!renewResult.IsSuccess)
        return renewResult;

      _logger.LogInformation($"Certificates renewed for account {cache.AccountId}");

      return IDomainResult.Success();
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
