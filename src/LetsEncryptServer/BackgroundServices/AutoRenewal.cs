using Microsoft.Extensions.Options;

using DomainResults.Common;


using MaksIT.LetsEncryptServer.Services;
using Models.LetsEncryptServer.CertsFlow.Requests;

namespace MaksIT.LetsEncryptServer.BackgroundServices {
  public class AutoRenewal : BackgroundService {

    private readonly IOptions<Configuration> _appSettings;
    private readonly ILogger<AutoRenewal> _logger;
    private readonly ICacheService _cacheService;
    private readonly ICertsFlowService _certsFlowService;

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

        var (accountsResponse, getAccountIdsResult) = await _cacheService.GetAccountsAsync();
        if (!getAccountIdsResult.IsSuccess || accountsResponse == null) {
          LogErrors(getAccountIdsResult.Errors);
          continue;
        }

        foreach (var accountId in accountsResponse.AccountIds) {
          await ProcessAccountAsync(accountId);
        }

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
      }
    }

    private async Task<IDomainResult> ProcessAccountAsync(Guid accountId) {
      var (cache, loadResult) = await _cacheService.LoadFromCacheAsync(accountId);
      if (!loadResult.IsSuccess || cache == null) {
        LogErrors(loadResult.Errors);
        return loadResult;
      }

      var hostnames = cache.GetHostsWithUpcomingSslExpiry();
      if (hostnames == null) {
        _logger.LogError("Unexpected hostnames null");
        return IDomainResult.Success();
      }


      if (!hostnames.Any()) {
        _logger.LogInformation("No hosts found with upcoming SSL expiry");
        return IDomainResult.Success();
      }

      var renewResult = await RenewCertificatesForHostnames(accountId, cache.Contacts, hostnames);
      if (!renewResult.IsSuccess)
        return renewResult;

      _logger.LogInformation($"Certificates renewed for account {accountId}");

      return IDomainResult.Success();
    }

    private async Task<IDomainResult> RenewCertificatesForHostnames(Guid accountId, string[] contacts, string[] hostnames) {
      var (sessionId, configureClientResult) = await _certsFlowService.ConfigureClientAsync();
      if (!configureClientResult.IsSuccess || sessionId == null) {
        LogErrors(configureClientResult.Errors);
        return configureClientResult;
      }

      var sessionIdValue = sessionId.Value;

      var (_, initResult) = await _certsFlowService.InitAsync(sessionIdValue, accountId, new InitRequest {
        Contacts = contacts
      });
      if (!initResult.IsSuccess) {
        LogErrors(initResult.Errors);
        return initResult;
      }

      var (_, newOrderResult) = await _certsFlowService.NewOrderAsync(sessionIdValue, new NewOrderRequest {
        Hostnames = hostnames,
        ChallengeType = "http-01"
      });
      if (!newOrderResult.IsSuccess) {
        LogErrors(newOrderResult.Errors);
        return newOrderResult;
      }

      var challengeResult = await _certsFlowService.CompleteChallengesAsync(sessionIdValue);
      if (!challengeResult.IsSuccess) {
        LogErrors(challengeResult.Errors);
        return challengeResult;
      }

      var getOrderResult = await _certsFlowService.GetOrderAsync(sessionIdValue, new GetOrderRequest {
        Hostnames = hostnames
      });
      if (!getOrderResult.IsSuccess) {
        LogErrors(getOrderResult.Errors);
        return getOrderResult;
      }

      var certs = await _certsFlowService.GetCertificatesAsync(sessionIdValue, new GetCertificatesRequest {
        Hostnames = hostnames
      });
      if (!certs.IsSuccess) {
        LogErrors(certs.Errors);
        return certs;
      }

      var (_, applyCertsResult) = await _certsFlowService.ApplyCertificatesAsync(sessionIdValue, new GetCertificatesRequest {
        Hostnames = hostnames
      });
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
