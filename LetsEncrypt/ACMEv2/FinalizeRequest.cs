using Newtonsoft.Json;

namespace ACMEv2
{
    public class FinalizeRequest
    {
        [JsonProperty("csr")]
        public string CSR { get; set; }
    }

}
