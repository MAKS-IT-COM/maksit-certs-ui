using Newtonsoft.Json;


namespace ACMEv2
{
    public class OrderIdentifier
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

    }

}
