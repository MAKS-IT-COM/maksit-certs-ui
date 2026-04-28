using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.Agent.Requests;

public class ServiceReloadRequest : RequestModelBase {
  public required string ServiceName { get; set; }
}
