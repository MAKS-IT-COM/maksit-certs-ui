using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Agent.Requests;

public class ServiceReloadRequest : RequestModelBase {
  public required string ServiceName { get; set; }
}
