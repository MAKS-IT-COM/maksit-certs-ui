using Newtonsoft.Json;

namespace LetsEncrypt.Entities
{
    public class FinalizeRequest
    {
        [JsonProperty("csr")]
        public string CSR { get; set; }
    }

}
