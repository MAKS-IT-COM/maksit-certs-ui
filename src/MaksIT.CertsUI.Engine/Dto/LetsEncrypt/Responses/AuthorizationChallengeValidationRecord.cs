namespace MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;


public class AuthorizationChallengeValidationRecord {
  public Uri? Url { get; set; }

  public string? Hostname { get; set; }

  public string? Port { get; set; }

  public List<string>? AddressesResolved { get; set; }

  public string? AddressUsed { get; set; }
}
