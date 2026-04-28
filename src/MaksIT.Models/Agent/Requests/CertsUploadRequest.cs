using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.Agent.Requests;

public class CertsUploadRequest : RequestModelBase {
  public required Dictionary<string, string> Certs { get; set; }
}
