using System;
using System.Collections.Generic;

namespace MaksIT.LetsEncrypt.Models.Responses;
public class AuthorizationChallengeChallenge
{
    public Uri? Url { get; set; }

    public string? Type { get; set; }

    public string? Status { get; set; }

    public string? Token { get; set; }

    // New properties added to complete the model
    public DateTime? Validated { get; set; }

    public AuthorizationChallengeError? Error { get; set; }

    public List<AuthorizationChallengeValidationRecord>? ValidationRecord { get; set; }
}


