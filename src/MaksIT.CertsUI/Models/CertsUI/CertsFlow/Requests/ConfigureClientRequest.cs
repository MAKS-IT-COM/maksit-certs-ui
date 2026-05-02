using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.CertsUI.CertsFlow.Requests;

public class ConfigureClientRequest : RequestModelBase {
  public bool IsStaging { get; set; }
}
