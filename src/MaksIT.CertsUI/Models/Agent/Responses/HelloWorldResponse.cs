using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Agent.Responses;

public class HelloWorldResponse : ResponseModelBase {
  public required string Message { get; set; }
}
