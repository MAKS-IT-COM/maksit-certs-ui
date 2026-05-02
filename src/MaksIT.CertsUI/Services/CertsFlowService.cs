using MaksIT.CertsUI.Abstractions.Services;
using MaksIT.CertsUI.Authorization;
using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaksIT.CertsUI.Services;

public interface ICertsFlowService {
  Task<Result<string?>> GetTermsOfServiceAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId);
  Task<Result> CompleteChallengesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId);
  Task<Result<Guid?>> ConfigureClientAsync(CertsUIAuthorizationData certsAuthorizationData, bool isStaging);
  Task<Result<Guid?>> InitAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, Guid? accountId, string description, string[] contacts);
  Task<Result<List<string>?>> NewOrderAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames, string challengeType);
  Task<Result> GetOrderAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames);
  Task<Result> GetCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames);
  Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId);
  Task<Result> RevokeCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames);
  Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames);
  Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames);
  Task<Result<string?>> AcmeChallengeAsync(string fileName, CancellationToken cancellationToken = default);
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

  private readonly ICertsFlowDomainService _domain = domain;

  public async Task<Result<string?>> GetTermsOfServiceAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac.ToResultOfType<string?>(null);
    return await _domain.GetTermsOfServiceAsync(sessionId);
  }

  public async Task<Result> CompleteChallengesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac;
    return await _domain.CompleteChallengesAsync(sessionId);
  }

  public async Task<Result<Guid?>> ConfigureClientAsync(CertsUIAuthorizationData certsAuthorizationData, bool isStaging) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac.ToResultOfType<Guid?>(null);
    return await _domain.ConfigureClientAsync(isStaging);
  }

  public async Task<Result<Guid?>> InitAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, Guid? accountId, string description, string[] contacts) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac.ToResultOfType<Guid?>(null);
    return await _domain.InitAsync(sessionId, accountId, description, contacts);
  }

  public async Task<Result<List<string>?>> NewOrderAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames, string challengeType) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac.ToResultOfType<List<string>?>(null);
    return await _domain.NewOrderAsync(sessionId, hostnames, challengeType);
  }

  public async Task<Result> GetOrderAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac;
    return await _domain.GetOrderAsync(sessionId, hostnames);
  }

  public async Task<Result> GetCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac;
    return await _domain.GetCertificatesAsync(sessionId, hostnames);
  }

  public async Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid accountId) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac.ToResultOfType<Dictionary<string, string>?>(null);
    return await _domain.ApplyCertificatesAsync(accountId);
  }

  public async Task<Result> RevokeCertificatesAsync(CertsUIAuthorizationData certsAuthorizationData, Guid sessionId, string[] hostnames) {
    var rbac = RBACWrapper(certsAuthorizationData, _ => Result.Ok(), _ => Result.Ok());
    if (!rbac.IsSuccess) return rbac;
    return await _domain.RevokeCertificatesAsync(sessionId, hostnames);
  }

  public Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames) =>
    _domain.FullFlow(isStaging, accountId, description, contacts, challengeType, hostnames);

  public Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames) =>
    _domain.FullRevocationFlow(isStaging, accountId, description, contacts, hostnames);

  public Task<Result<string?>> AcmeChallengeAsync(string fileName, CancellationToken cancellationToken = default) =>
    _domain.AcmeChallengeAsync(fileName, cancellationToken);
}
