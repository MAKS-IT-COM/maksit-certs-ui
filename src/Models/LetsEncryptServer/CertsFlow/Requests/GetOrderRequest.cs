namespace Models.LetsEncryptServer.CertsFlow.Requests
{
    public class GetOrderRequest
    {
        public string[] Hostnames { get; set; }
    }
}
