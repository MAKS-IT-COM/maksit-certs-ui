using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.Agent.Requests;

public class ServiceReloadRequest : RequestModelBase {
  public string ServiceName { get; set; }
}
