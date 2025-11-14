/**
* https://tools.ietf.org/html/rfc4648
* https://tools.ietf.org/html/rfc4648#section-5
*/

using System.Security.Cryptography;
using MaksIT.LetsEncrypt.Entities.Jws;
using MaksIT.Core.Security.JWK;
using MaksIT.Core.Security.JWS;



namespace MaksIT.LetsEncrypt.Services;

public interface IJwsService {
  void SetKeyId(string location);
  JwsMessage Encode(ACMEJwsHeader protectedHeader);
  JwsMessage Encode<TPayload>(TPayload payload, ACMEJwsHeader protectedHeader);
  string GetKeyAuthorization(string token);
}

public class JwsService : IJwsService {

  public Jwk _jwk;
  private RSA _rsa;

  public JwsService(RSA rsa) {
    _rsa = rsa;

    if (!JwkGenerator.TryGenerateFromRSA(rsa, out _jwk, out var errorMessage)) {
      throw new Exception(errorMessage);
    }
  }

  public void SetKeyId(string location) {
    _jwk.KeyId = location;
  }

  public JwsMessage Encode(ACMEJwsHeader protectedHeader) {

    Encode<string>(null, protectedHeader);

    if (!JwsGenerator.TryEncode(_rsa, _jwk, protectedHeader, out var jwsMessage, out var errorMessage)) {
      throw new Exception(errorMessage);
    }

    return jwsMessage;

  }
  

  public JwsMessage Encode<TPayload>(TPayload? payload, ACMEJwsHeader protectedHeader) {

    if (!JwsGenerator.TryEncode(_rsa, _jwk, protectedHeader, payload, out var jwsMessage, out var errorMessage)) {
      throw new Exception(errorMessage);
    }

    return jwsMessage;

  }

  public string GetKeyAuthorization(string token) {
    if (!JwkThumbprintUtility.TryGetKeyAuthorization(_jwk, token, out var keyAuthorization, out var errorMessage))
      throw new Exception(errorMessage);

    return keyAuthorization;

  }
}
