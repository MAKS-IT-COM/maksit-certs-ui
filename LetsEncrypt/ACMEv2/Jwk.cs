/*
 * JSON Web Key (JWK)
 * https://tools.ietf.org/html/rfc7517
 * https://www.gnupg.org/documentation/manuals/gcrypt-devel/RSA-key-parameters.html
 *
*/

using Newtonsoft.Json;

namespace ACMEv2
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
        /// RSA public modulus n.
        /// </summary>
        [JsonProperty("n")]
        public string Modulus { get; set; }

        /// <summary>
        /// RSA public exponent e. 
        /// </summary>
        [JsonProperty("e")]
        public string Exponent { get; set; }

        /// <summary>
        /// RSA secret exponent d = e^-1 \bmod (p-1)(q-1).
        /// </summary>
        [JsonProperty("d")]
        public string D { get; set; }

        /// <summary>
        /// RSA secret prime p. 
        /// </summary>
        [JsonProperty("p")]
        public string P { get; set; }

        /// <summary>
        /// RSA secret prime q with p < q. 
        /// </summary>
        [JsonProperty("q")]
        public string Q { get; set; }

        [JsonProperty("dp")]
        public string DP { get; set; }

        [JsonProperty("dq")]
        public string DQ { get; set; }

        [JsonProperty("qi")]
        public string InverseQ { get; set; }

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
