using System;

namespace MaksIT.LetsEncrypt.Entities;
public class AuthorizationChallenge {
  public Uri? Url { get; set; }

  public string? Type { get; set; }

  public string? Status { get; set; }

  public string? Token { get; set; }
}
