namespace MaksIT.LetsEncryptServer.Models.Requests {
  public class NewOrderRequest {
    public string[] Hostnames { get; set; }
    
    public string ChallengeType { get; set; }
  }
}
