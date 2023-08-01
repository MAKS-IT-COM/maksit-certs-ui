// https://tools.ietf.org/html/rfc7517

using System.Text.Json.Serialization;

namespace MaksIT.LetsEncrypt.Entities.Jws;
public class Jwk {
  /// <summary>
  /// "kty" (Key Type) Parameter
  /// <para>
  /// The "kty" (key type) parameter identifies the cryptographic algorithm
  /// family used with the key, such as "RSA" or "EC".
  /// </para>
  /// </summary>
  [JsonPropertyName("kty")]
  public string? KeyType { get; set; }

  /// <summary>
  /// "kid" (Key ID) Parameter
  /// <para>
  /// The "kid" (key ID) parameter is used to match a specific key. This
  /// is used, for instance, to choose among a set of keys within a JWK Set
  /// during key rollover.  The structure of the "kid" value is
  /// unspecified.
  /// </para>
  /// </summary>
  [JsonPropertyName("kid")]
  public string? KeyId { get; set; }

  /// <summary>
  /// "use" (Public Key Use) Parameter
  /// <para>
  /// The "use" (public key use) parameter identifies the intended use of
  /// the public key.  The "use" parameter is employed to indicate whether
  /// a public key is used for encrypting data or verifying the signature
  /// on data.
  /// </para>
  /// </summary>
  [JsonPropertyName("use")]
  public string? Use { get; set; }

  /// <summary>
  /// The the modulus value for the public RSA key. It is represented as the Base64URL encoding of value's big endian representation.
  /// </summary>
  [JsonPropertyName("n")]
  public string? Modulus { get; set; }

  /// <summary>
  /// The exponent value for the public RSA key. It is represented as the Base64URL encoding of value's big endian representation.
  /// </summary>
  [JsonPropertyName("e")]
  public string? Exponent { get; set; }

  /// <summary>
  /// The private exponent. It is represented as the Base64URL encoding of the value's big endian representation.
  /// </summary>
  [JsonPropertyName("d")]
  public string? D { get; set; }

  /// <summary>
  /// The first prime factor. It is represented as the Base64URL encoding of the value's big endian representation.
  /// </summary>
  [JsonPropertyName("p")]
  public string? P { get; set; }

  /// <summary>
  /// The second prime factor. It is represented as the Base64URL encoding of the value's big endian representation.
  /// </summary>
  [JsonPropertyName("q")]
  public string? Q { get; set; }

  /// <summary>
  /// The first factor Chinese Remainder Theorem exponent. It is represented as the Base64URL encoding of the value's big endian representation.
  /// </summary>
  [JsonPropertyName("dp")]
  public string? DP { get; set; }

  /// <summary>
  /// The second factor Chinese Remainder Theorem exponent. It is represented as the Base64URL encoding of the value's big endian representation.
  /// </summary>
  [JsonPropertyName("dq")]
  public string? DQ { get; set; }

  /// <summary>
  /// The first Chinese Remainder Theorem coefficient. It is represented as the Base64URL encoding of the value's big endian representation.
  /// </summary>
  [JsonPropertyName("qi")]
  public string? InverseQ { get; set; }

  /// <summary>
  /// The other primes information, should they exist, null or an empty list if not specified.
  /// </summary>
  [JsonPropertyName("oth")]
  public string? OthInf { get; set; }

  /// <summary>
  /// "alg" (Algorithm) Parameter
  /// <para>
  /// The "alg" (algorithm) parameter identifies the algorithm intended for
  /// use with the key.
  /// </para>
  /// </summary>
  [JsonPropertyName("alg")]
  public string? Algorithm { get; set; }
}
