using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.Agent.Responses;

public class HelloWorldResponse : ResponseModelBase {
  public string Message { get; set; }
}
