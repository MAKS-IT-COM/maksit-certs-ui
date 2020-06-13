// https://tools.ietf.org/html/rfc7517

using System;
using Newtonsoft.Json;


namespace LetsEncrypt.Entities
{
    public class Jwk
    {
        /// <summary>
        /// "kty" (Key Type) Parameter
        /// <para>
        /// The "kty" (key type) parameter identifies the cryptographic algorithm
        /// family used with the key, such as "RSA" or "EC".
        /// </para>
        /// </summary>
        [JsonProperty("kty")]
        public string KeyType { get; set; }

        /// <summary>
        /// "kid" (Key ID) Parameter
        /// <para>
        /// The "kid" (key ID) parameter is used to match a specific key. This
        /// is used, for instance, to choose among a set of keys within a JWK Set
        /// during key rollover.  The structure of the "kid" value is
        /// unspecified.
        /// </para>
        /// </summary>
        [JsonProperty("kid")]
        public string KeyId { get; set; }

        /// <summary>
        /// "use" (Public Key Use) Parameter
        /// <para>
        /// The "use" (public key use) parameter identifies the intended use of
        /// the public key.  The "use" parameter is employed to indicate whether
        /// a public key is used for encrypting data or verifying the signature
        /// on data.
        /// </para>
        /// </summary>
        [JsonProperty("use")]
        public string Use { get; set; }

        /// <summary>
        /// The the modulus value for the public RSA key. It is represented as the Base64URL encoding of value's big endian representation.
        /// </summary>
        [JsonProperty("n")]
        public string Modulus { get; set; }

        /// <summary>
        /// The exponent value for the public RSA key. It is represented as the Base64URL encoding of value's big endian representation.
        /// </summary>
        [JsonProperty("e")]
        public string Exponent { get; set; }

        /// <summary>
        /// The private exponent. It is represented as the Base64URL encoding of the value's big endian representation.
        /// </summary>
        [JsonProperty("d")]
        public string D { get; set; }

        /// <summary>
        /// The first prime factor. It is represented as the Base64URL encoding of the value's big endian representation.
        /// </summary>
        [JsonProperty("p")]
        public string P { get; set; }

        /// <summary>
        /// The second prime factor. It is represented as the Base64URL encoding of the value's big endian representation.
        /// </summary>
        [JsonProperty("q")]
        public string Q { get; set; }

        /// <summary>
        /// The first factor Chinese Remainder Theorem exponent. It is represented as the Base64URL encoding of the value's big endian representation.
        /// </summary>
        [JsonProperty("dp")]
        public string DP { get; set; }

        /// <summary>
        /// The second factor Chinese Remainder Theorem exponent. It is represented as the Base64URL encoding of the value's big endian representation.
        /// </summary>
        [JsonProperty("dq")]
        public string DQ { get; set; }

        /// <summary>
        /// The first Chinese Remainder Theorem coefficient. It is represented as the Base64URL encoding of the value's big endian representation.
        /// </summary>
        [JsonProperty("qi")]
        public string InverseQ { get; set; }

        /// <summary>
        /// The other primes information, should they exist, null or an empty list if not specified.
        /// </summary>
        [JsonProperty("oth")]
        public string OthInf { get; set; }

        /// <summary>
        /// "alg" (Algorithm) Parameter
        /// <para>
        /// The "alg" (algorithm) parameter identifies the algorithm intended for
        /// use with the key.
        /// </para>
        /// </summary>
        [JsonProperty("alg")]
        public string Algorithm { get; set; }
    }
}