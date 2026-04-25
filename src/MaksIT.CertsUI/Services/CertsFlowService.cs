using MaksIT.CertsUI.Engine.DomainServices;
using MaksIT.Results;

namespace MaksIT.CertsUI.Services;

public interface ICertsFlowService {
  Result<string?> GetTermsOfService(Guid sessionId);
  Task<Result> CompleteChallengesAsync(Guid sessionId);
  Task<Result<Guid?>> ConfigureClientAsync(bool isStaging);
  Task<Result<Guid?>> InitAsync(Guid sessionId, Guid? accountId, string description, string[] contacts);
  Task<Result<List<string>?>> NewOrderAsync(Guid sessionId, string[] hostnames, string challengeType);
  Task<Result> GetOrderAsync(Guid sessionId, string[] hostnames);
  Task<Result> GetCertificatesAsync(Guid sessionId, string[] hostnames);
  Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid accountId);
  Task<Result> RevokeCertificatesAsync(Guid sessionId, string[] hostnames);
  Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames);
  Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames);
  Task<Result<string?>> AcmeChallengeAsync(string fileName, CancellationToken cancellationToken = default);
}

/// <summary>HTTP-facing façade; ACME orchestration lives in <see cref="ICertsFlowDomainService"/>.</summary>
public sealed class CertsFlowService(
  ICertsFlowDomainService domain
) : ICertsFlowService {

  public Result<string?> GetTermsOfService(Guid sessionId) =>
    domain.GetTermsOfService(sessionId);

  public Task<Result> CompleteChallengesAsync(Guid sessionId) =>
    domain.CompleteChallengesAsync(sessionId);

  public Task<Result<Guid?>> ConfigureClientAsync(bool isStaging) =>
    domain.ConfigureClientAsync(isStaging);

  public Task<Result<Guid?>> InitAsync(Guid sessionId, Guid? accountId, string description, string[] contacts) =>
    domain.InitAsync(sessionId, accountId, description, contacts);

  public Task<Result<List<string>?>> NewOrderAsync(Guid sessionId, string[] hostnames, string challengeType) =>
    domain.NewOrderAsync(sessionId, hostnames, challengeType);

  public Task<Result> GetOrderAsync(Guid sessionId, string[] hostnames) =>
    domain.GetOrderAsync(sessionId, hostnames);

  public Task<Result> GetCertificatesAsync(Guid sessionId, string[] hostnames) =>
    domain.GetCertificatesAsync(sessionId, hostnames);

  public Task<Result<Dictionary<string, string>?>> ApplyCertificatesAsync(Guid accountId) =>
    domain.ApplyCertificatesAsync(accountId);

  public Task<Result> RevokeCertificatesAsync(Guid sessionId, string[] hostnames) =>
    domain.RevokeCertificatesAsync(sessionId, hostnames);

  public Task<Result<Guid?>> FullFlow(bool isStaging, Guid? accountId, string description, string[] contacts, string challengeType, string[] hostnames) =>
    domain.FullFlow(isStaging, accountId, description, contacts, challengeType, hostnames);

  public Task<Result> FullRevocationFlow(bool isStaging, Guid accountId, string description, string[] contacts, string[] hostnames) =>
    domain.FullRevocationFlow(isStaging, accountId, description, contacts, hostnames);

  public Task<Result<string?>> AcmeChallengeAsync(string fileName, CancellationToken cancellationToken = default) =>
    domain.AcmeChallengeAsync(fileName, cancellationToken);
}
