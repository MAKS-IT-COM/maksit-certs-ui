using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.LetsEncrypt.Models.Responses;


public class AuthorizationChallengeValidationRecord {
  public Uri? Url { get; set; }

  public string? Hostname { get; set; }

  public string? Port { get; set; }

  public List<string>? AddressesResolved { get; set; }

  public string? AddressUsed { get; set; }
}