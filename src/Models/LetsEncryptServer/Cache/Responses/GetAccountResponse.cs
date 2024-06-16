using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.LetsEncryptServer.Cache.Responses {
  public class GetAccountResponse {
    public Guid AccountId { get; set; }

    public string? Description { get; set; }

    public string []? Contacts { get; set; }

    public string[]? Hostnames { get; set; }
  }
}
