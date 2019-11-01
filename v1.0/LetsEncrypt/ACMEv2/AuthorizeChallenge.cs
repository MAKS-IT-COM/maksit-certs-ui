using Newtonsoft.Json;

namespace ACMEv2
{

    public class AuthorizeChallenge
    {
        [JsonProperty("keyAuthorization")]
        public string KeyAuthorization { get; set; }

    }




}
