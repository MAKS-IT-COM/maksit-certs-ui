using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Account.Requests {

  public class PatchContactsRequest {
    public List<PatchAction<string>> Contacts { get; set; }
  }
}
