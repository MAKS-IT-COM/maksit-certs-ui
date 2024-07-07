namespace MaksIT.LetsEncrypt.Models.Responses;

public class AcmeDirectory {
  public Uri KeyChange { get; set; }
  public AcmeDirectoryMeta Meta { get; set; }
  public Uri NewAccount { get; set; }
  public Uri NewNonce { get; set; }
  public Uri NewOrder { get; set; }
  public Uri RenewalInfo { get; set; }
  public Uri RevokeCert { get; set; }  
}

public class AcmeDirectoryMeta {
  public string[] CaaIdentities { get; set; }
  public string TermsOfService { get; set; }
  public string Website { get; set; }
}