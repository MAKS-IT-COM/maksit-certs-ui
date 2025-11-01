using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.LetsEncrypt.Models.Responses;
public class AuthorizationChallengeError {
  public string Type { get; set; }

  public string Detail { get; set; }
}
