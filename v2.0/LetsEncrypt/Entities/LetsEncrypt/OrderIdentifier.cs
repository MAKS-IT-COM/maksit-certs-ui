using Newtonsoft.Json;


namespace LetsEncrypt.Entities
{
    public class OrderIdentifier
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

    }

}
