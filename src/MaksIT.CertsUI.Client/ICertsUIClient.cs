using MaksIT.CertsUI.Client.Models;
using MaksIT.CertsUI.Contracts;

namespace MaksIT.CertsUI.Client;

/// <summary>Client for the MaksIT CertsUI API using API key authentication.</summary>
public interface ICertsUIClient {
  Task CheckHealthLiveAsync(CancellationToken cancellationToken = default);
  Task CheckHealthReadyAsync(CancellationToken cancellationToken = default);

  Task<AccountResponse[]> GetAccountsAsync(CancellationToken cancellationToken = default);
  Task<AccountResponse> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
  Task<AccountResponse> CreateAccountAsync(PostAccountRequest request, CancellationToken cancellationToken = default);
  Task<AccountResponse> PatchAccountAsync(Guid accountId, PatchAccountRequest request, CancellationToken cancellationToken = default);
  Task DeleteAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

  Task<RuntimeInstanceIdResponse> GetRuntimeInstanceIdAsync(CancellationToken cancellationToken = default);
}
