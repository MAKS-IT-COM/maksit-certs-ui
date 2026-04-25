namespace MaksIT.CertsUI.Authorization;

/// <summary>Auth header names. <see cref="ApiKeyHeaderName"/> values: new keys use <c>{guid:N}|{secret}</c> (salt + app pepper at rest); older keys are a single opaque blob.</summary>
public static class ApiAuthConstants {
  public const string ApiKeyHeaderName = "X-API-KEY";
}
