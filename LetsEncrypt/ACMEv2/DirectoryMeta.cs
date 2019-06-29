using Newtonsoft.Json;

namespace ACMEv2
{
    public class DirectoryMeta
    {
        [JsonProperty("termsOfService")]
        public string TermsOfService { get; set; }
    }


}
