using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.Models.LetsEncryptServer.Identity.Login;

public class RefreshTokenRequest : RequestModelBase {
  public required string RefreshToken { get; set; } // The refresh token used for renewing access
}
