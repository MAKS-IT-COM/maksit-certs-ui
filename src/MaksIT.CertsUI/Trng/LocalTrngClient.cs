using System.Security.Cryptography;
using MaksIT.Results;

namespace MaksIT.CertsUI.Trng;

/// <summary>Local TRNG (parity with Vault LocalTrngClient): cryptographically strong RNG-backed bytes.</summary>
public sealed class LocalTrngClient : ITrngClient {

  public async Task<Result<byte[]?>> GetRandomBytesAsync(int? length) {
    if (length == null || length <= 0)
      return Result<byte[]?>.BadRequest(null, "Length must be a positive integer.");

    try {
      var randomBytes = new byte[length.Value];
      RandomNumberGenerator.Fill(randomBytes);
      return await Task.FromResult(Result<byte[]?>.Ok(randomBytes)).ConfigureAwait(false);
    }
    catch (Exception ex) {
      return Result<byte[]?>.InternalServerError(null, "Failed to generate random bytes locally.", ex.Message);
    }
  }

  public async Task<Result<string?>> GetRandomBytesBase64Async(int? length) {
    var byteResult = await GetRandomBytesAsync(length).ConfigureAwait(false);
    if (!byteResult.IsSuccess || byteResult.Value == null)
      return byteResult.ToResultOfType<string?>(_ => null);

    try {
      var base64String = Convert.ToBase64String(byteResult.Value);
      return Result<string?>.Ok(base64String);
    }
    catch (Exception ex) {
      return Result<string?>.InternalServerError(null, "Failed to encode random bytes to Base64.", ex.Message);
    }
  }
}
