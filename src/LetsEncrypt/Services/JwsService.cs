/**
* https://tools.ietf.org/html/rfc4648
* https://tools.ietf.org/html/rfc4648#section-5
*/

using System.Text;
using System.Security.Cryptography;
using MaksIT.Core.Extensions;
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
    _rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));

    var publicParameters = rsa.ExportParameters(false);

    var exp = publicParameters.Exponent ?? throw new ArgumentNullException(nameof(publicParameters.Exponent));
    var mod = publicParameters.Modulus ?? throw new ArgumentNullException(nameof(publicParameters.Modulus));

    _jwk = new Jwk() {
      KeyType = JwkKeyType.Rsa.Name,
      RsaExponent = Base64UrlUtility.Encode(exp),
      RsaModulus = Base64UrlUtility.Encode(mod),
    };
  }

  public void SetKeyId(string location) {
    _jwk.KeyId = location;
  }

  public JwsMessage Encode(ACMEJwsHeader protectedHeader) =>
    Encode<string>(null, protectedHeader);

  public JwsMessage Encode<T>(T? payload, ACMEJwsHeader protectedHeader) {

    protectedHeader.Algorithm = JwkAlgorithm.Rs256.Name;
    if (_jwk.KeyId != null) {
      protectedHeader.KeyId = _jwk.KeyId;
    }
    else {
      protectedHeader.Key = _jwk;
    }

    var message = new JwsMessage {
      Payload = "",
      Protected = Base64UrlUtility.Encode(protectedHeader.ToJson())
    };

    if (payload != null) {
      if (payload is string stringPayload) 
        message.Payload = Base64UrlUtility.Encode(stringPayload);
      else 
        message.Payload = Base64UrlUtility.Encode(payload.ToJson());
    }

    message.Signature = Base64UrlUtility.Encode(
        _rsa.SignData(Encoding.ASCII.GetBytes($"{message.Protected}.{message.Payload}"),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1));

    return message;
  }

  public string GetKeyAuthorization(string token) =>
    $"{token}.{GetSha256Thumbprint()}";

  private string GetSha256Thumbprint() {

    var thumbprint = new {
      e = _jwk.RsaExponent,
      kty = "RSA",
      n = _jwk.RsaModulus
    };

    var json = "{\"e\":\"" + _jwk.RsaExponent + "\",\"kty\":\"RSA\",\"n\":\"" + _jwk.RsaModulus + "\"}";
    return Base64UrlUtility.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
  }
}
