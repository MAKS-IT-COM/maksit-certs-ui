using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.LetsEncryptServer.CertsFlow.Requests;

public class ConfigureClientRequest : RequestModelBase {
  public bool IsStaging { get; set; }
}
