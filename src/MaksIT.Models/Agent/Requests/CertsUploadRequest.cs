using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.Agent.Requests;

public class CertsUploadRequest : RequestModelBase {
  public Dictionary<string, string> Certs { get; set; }
}
