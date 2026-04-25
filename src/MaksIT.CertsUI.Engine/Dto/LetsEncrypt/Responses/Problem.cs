namespace MaksIT.CertsUI.Engine.Dto.LetsEncrypt.Responses;

public class Problem {
  public string? Type { get; set; }

  public string? Detail { get; set; }

  public string? RawJson { get; set; }
}
