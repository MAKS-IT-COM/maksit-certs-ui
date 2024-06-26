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

      var renewResult = await RenewCertificatesForHostnames(cache.AccountId, cache.Description, cache.Contacts, hostnames, cache.ChallengeType, cache.IsStaging);
      if (!renewResult.IsSuccess)
        return renewResult;

      _logger.LogInformation($"Certificates renewed for account {cache.AccountId}");

      return IDomainResult.Success();
    }

    private async Task<IDomainResult> RenewCertificatesForHostnames(Guid accountId, string description, string[] contacts, string[] hostnames, string challengeType, bool isStaging) {
      var (sessionId, configureClientResult) = await _certsFlowService.ConfigureClientAsync(isStaging);
      if (!configureClientResult.IsSuccess || sessionId == null) {
        LogErrors(configureClientResult.Errors);
        return configureClientResult;
      }

      var sessionIdValue = sessionId.Value;

      var (_, initResult) = await _certsFlowService.InitAsync(sessionIdValue, accountId, description, contacts);
      if (!initResult.IsSuccess) {
        LogErrors(initResult.Errors);
        return initResult;
      }

      var (_, newOrderResult) = await _certsFlowService.NewOrderAsync(sessionIdValue, hostnames, challengeType);
      if (!newOrderResult.IsSuccess) {
        LogErrors(newOrderResult.Errors);
        return newOrderResult;
      }

      var challengeResult = await _certsFlowService.CompleteChallengesAsync(sessionIdValue);
      if (!challengeResult.IsSuccess) {
        LogErrors(challengeResult.Errors);
        return challengeResult;
      }

      var getOrderResult = await _certsFlowService.GetOrderAsync(sessionIdValue, hostnames);
      if (!getOrderResult.IsSuccess) {
        LogErrors(getOrderResult.Errors);
        return getOrderResult;
      }

      var certs = await _certsFlowService.GetCertificatesAsync(sessionIdValue, hostnames);
      if (!certs.IsSuccess) {
        LogErrors(certs.Errors);
        return certs;
      }

      var (_, applyCertsResult) = await _certsFlowService.ApplyCertificatesAsync(sessionIdValue, hostnames);
      if (!applyCertsResult.IsSuccess) {
        LogErrors(applyCertsResult.Errors);
        return applyCertsResult;
      }

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
