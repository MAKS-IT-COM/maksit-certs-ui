using MaksIT.Core.Abstractions.Webapi;


namespace MaksIT.CertsUI.Models.Identity.Login;

public class RefreshTokenRequest : RequestModelBase {
  public required string RefreshToken { get; set; } // The refresh token used for renewing access

  public bool? Force { get; set; } // Optional flag to force token refresh, bypassing certain checks
}