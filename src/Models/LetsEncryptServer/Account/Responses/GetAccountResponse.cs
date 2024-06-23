using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Account.Responses {
  public class GetAccountResponse {
    public Guid AccountId { get; set; }

    public string? Description { get; set; }

    public required string [] Contacts { get; set; }

    public string? ChallengeType { get; set; }

    public HostnameResponse[]? Hostnames { get; set; }
  }
}
