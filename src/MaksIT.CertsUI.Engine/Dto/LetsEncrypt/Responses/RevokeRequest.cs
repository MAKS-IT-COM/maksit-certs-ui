namespace MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;

public class RevokeRequest {
  public string Certificate { get; set; } = string.Empty;
  public int Reason { get; set; }
}
