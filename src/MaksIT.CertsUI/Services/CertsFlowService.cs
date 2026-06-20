using MaksIT.CertsUI.Abstractions.Services;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaksIT.CertsUI.Services;

public interface ICertsFlowService {

  #region Terms of service
  Task<Result<string?>> GetTermsOfServiceAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId);
  #endregion

  #region Session, orders, and certificates
  Task<Result> CompleteChallengesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId);
  Task<Result<Guid?>> ConfigureClientAsync(CertsUIAuthorizationData certsAuthorizationData, bool isStaging);
  Task<Result<Guid?>> InitAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, Guid? accountId, string description, string[] contacts);
  Task<Result<List<string>?>> NewOrderAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames, string challengeType);
  Task<Result> GetOrderAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames);
  Task<Result> GetCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames);
  #endregion

  #region Deploy and revoke
  Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId);
  Task<Result> RevokeCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames);
  #endregion

  #region Full orchestration
  Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames);
  Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames);
  #endregion

  #region HTTP-01 challenge
  Task<Result<string?>> AcmeChallengeAsync(string fileName, CancellationToken cancellationToken = default);
  #endregion
}

/// <summary>HTTP-facing façade; ACME orchestration lives in <see cref="ICertsFlowDomainService"/>.</summary>
public sealed class CertsFlowService(
  ILogger<CertsFlowService> logger,
  IOptions<Configuration> appSettings,
  ICertsFlowDomainService domain
) : ServiceBase(
  logger,
  appSettings
), ICertsFlowService {

  #region Terms of service

  public async Task<Result<string?>> GetTermsOfServiceAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<string?>(null);

    return await domain.GetTermsOfServiceAsync(sessionId);
  }

  #endregion

  #region Session, orders, and certificates

  public async Task<Result> CompleteChallengesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac;

    return await domain.CompleteChallengesAsync(sessionId);
  }

  public async Task<Result<Guid?>> ConfigureClientAsync(CertsUIAuthorizationData certsAuthorizationData, bool isStaging) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<Guid?>(null);

    return await domain.ConfigureClientAsync(isStaging);
  }

  public async Task<Result<Guid?>> InitAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, Guid? accountId, string description, string[] contacts) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<Guid?>(null);

    return await domain.InitAsync(sessionId, accountId, description, contacts);
  }

  public async Task<Result<List<string>?>> NewOrderAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames, string challengeType) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<List<string>?>(null);

    return await domain.NewOrderAsync(sessionId, hostnames, challengeType);
  }

  public async Task<Result> GetOrderAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac;

    return await domain.GetOrderAsync(sessionId, hostnames);
  }

  public async Task<Result> GetCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac;

    return await domain.GetCertificatesAsync(sessionId, hostnames);
  }

  #endregion

  #region Deploy and revoke

  public async Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac.ToResultOfType<Dictionary<string, string>?>(null);

    return await domain.ApplyCertificatesAsync(accountId);
  }

  public async Task<Result> RevokeCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess)
      return rbac;

    return await domain.RevokeCertificatesAsync(sessionId, hostnames);
  }

  #endregion

  #region Full orchestration

  public Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames) =>
    domain.FullFlow(isStaging, accountId, description, contacts, challengeType, hostnames);

  public Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames) =>
    domain.FullRevocationFlow(isStaging, accountId, description, contacts, hostnames);

  #endregion

  #region HTTP-01 challenge

  public Task<Result<string?>> AcmeChallengeAsync(string fileName, CancellationToken cancellationToken = default) =>
    domain.AcmeChallengeAsync(fileName, cancellationToken);

  #endregion
}
