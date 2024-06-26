namespace MaksIT.LetsEncrypt.Entities;

public class CertificateCache {
  public required string Cert { get; set; }
  public required byte[]? Private { get; set; }
  public required string? PrivatePem { get; set; }
  public bool IsDisabled { get; set; }
}
