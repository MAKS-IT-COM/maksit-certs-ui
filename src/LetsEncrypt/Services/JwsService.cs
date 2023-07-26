/**
* https://tools.ietf.org/html/rfc4648
* https://tools.ietf.org/html/rfc4648#section-5
*/


using System.Text;
using System.Security.Cryptography;

using MaksIT.LetsEncrypt.Entities.Jws;

using MaksIT.Core.Extensions;

namespace MaksIT.LetsEncrypt.Services {
  public interface IJwsService {

    void Init(RSA rsa, string? keyId);

    JwsMessage Encode(JwsHeader protectedHeader);

    JwsMessage Encode<TPayload>(TPayload payload, JwsHeader protectedHeader);

    string GetKeyAuthorization(string token);

    string Base64UrlEncoded(byte[] arg);

    void SetKeyId(string location);
  }


  public class JwsService : IJwsService {

    public Jwk? _jwk;
    private RSA? _rsa;

    public void Init(RSA rsa, string? keyId) {
      _rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));

      var publicParameters = rsa.ExportParameters(false);

      _jwk = new Jwk() {
        KeyType = "RSA",
        Exponent = Base64UrlEncoded(publicParameters.Exponent),
        Modulus = Base64UrlEncoded(publicParameters.Modulus),
        KeyId = keyId
      };
    }

    public JwsMessage Encode(JwsHeader protectedHeader) =>
      Encode<string>(null, protectedHeader);

    public JwsMessage Encode<TPayload>(TPayload? payload, JwsHeader protectedHeader) {

      protectedHeader.Algorithm = "RS256";
      if (_jwk.KeyId != null) {
        protectedHeader.KeyId = _jwk.KeyId;
      }
      else {
        protectedHeader.Key = _jwk;
      }

      var message = new JwsMessage {
        Payload = "",
        Protected = Base64UrlEncoded(protectedHeader.ToJson())
      };

      if (payload != null) {
        if (payload is string) {
          string value = payload.ToString();
          message.Payload = Base64UrlEncoded(value);
        }
        else {
          message.Payload = Base64UrlEncoded(payload.ToJson());
        }
      }


      message.Signature = Base64UrlEncoded(
          _rsa.SignData(Encoding.ASCII.GetBytes($"{message.Protected}.{message.Payload}"),
              HashAlgorithmName.SHA256,
              RSASignaturePadding.Pkcs1));

      return message;
    }

    private string GetSha256Thumbprint() {
      var json = "{\"e\":\"" + _jwk.Exponent + "\",\"kty\":\"RSA\",\"n\":\"" + _jwk.Modulus + "\"}";

      using (var sha256 = SHA256.Create()) {
        return Base64UrlEncoded(sha256.ComputeHash(Encoding.UTF8.GetBytes(json)));
      }
    }

    public string GetKeyAuthorization(string token) => $"{token}.{GetSha256Thumbprint()}";



    public string Base64UrlEncoded(string s) => Base64UrlEncoded(Encoding.UTF8.GetBytes(s));


    // https://tools.ietf.org/html/rfc4648#section-5
    public string Base64UrlEncoded(byte[] arg) {
      var s = Convert.ToBase64String(arg); // Regular base64 encoder
      s = s.Split('=')[0]; // Remove any trailing '='s
      s = s.Replace('+', '-'); // 62nd char of encoding
      s = s.Replace('/', '_'); // 63rd char of encoding
      return s;
    }

    public void SetKeyId(string location) {
      _jwk.KeyId = location;
    }
  }
}
