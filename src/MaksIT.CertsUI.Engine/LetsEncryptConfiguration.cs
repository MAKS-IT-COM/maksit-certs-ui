namespace MaksIT.LetsEncrypt;

public interface ILetsEncryptConfiguration {
  string Production { get; set; }
  string Staging { get; set; }
}

public class LetsEncryptConfiguration : ILetsEncryptConfiguration {
  public required string Production { get; set; }
  public required string Staging { get; set; }
}
