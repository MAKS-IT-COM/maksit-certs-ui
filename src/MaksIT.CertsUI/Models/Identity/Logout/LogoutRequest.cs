using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Identity.Logout;

public class LogoutRequest : RequestModelBase {
  public bool LogoutFromAllDevices { get; set; }
}
