using MaksIT.Results;

namespace MaksIT.CertsUI.Trng;

/// <summary>Same contract as <c>MaksIT.Vault.Trng.ITrngClient</c>: cryptographically strong random material for API keys.</summary>
public interface ITrngClient {
  Task<Result<byte[]?>> GetRandomBytesAsync(int? length);

  Task<Result<string?>> GetRandomBytesBase64Async(int? length);
}
