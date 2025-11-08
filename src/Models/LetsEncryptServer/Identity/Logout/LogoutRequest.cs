using MaksIT.Core.Abstractions.Webapi;


namespace Models.LetsEncryptServer.Identity.Logout;

public class LogoutRequest : RequestModelBase {
  public bool LogoutFromAllDevices { get; set; }
}
