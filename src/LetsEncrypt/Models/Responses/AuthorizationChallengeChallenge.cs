using System;

namespace MaksIT.LetsEncrypt.Models.Responses;
public class AuthorizationChallengeChallenge
{
    public Uri? Url { get; set; }

    public string? Type { get; set; }

    public string? Status { get; set; }

    public string? Token { get; set; }
}
