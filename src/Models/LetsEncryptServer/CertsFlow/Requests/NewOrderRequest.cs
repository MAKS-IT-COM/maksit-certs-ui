namespace Models.LetsEncryptServer.CertsFlow.Requests
{
    public class NewOrderRequest
    {
        public string[] Hostnames { get; set; }

        public string ChallengeType { get; set; }
    }
}
