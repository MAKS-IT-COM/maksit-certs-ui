namespace Models.LetsEncryptServer.CertsFlow.Requests
{
    public class GetCertificatesRequest
    {
        public string[] Hostnames { get; set; }
    }
}
