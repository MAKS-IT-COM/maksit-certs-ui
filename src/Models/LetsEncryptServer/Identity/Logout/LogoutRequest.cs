using MaksIT.Core.Abstractions.Webapi;


namespace Models.LetsEncryptServer.Identity.Logout;

public class LogoutRequest : RequestModelBase {
  public required string Token { get; set; }
  public bool LogoutFromAllDevices { get; set; }
}
