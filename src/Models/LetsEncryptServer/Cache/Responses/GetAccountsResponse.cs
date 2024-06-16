using Models.LetsEncryptServer.Cache.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Cache.Responses {
  public class GetAccountsResponse {
    public GetAccountResponse[] Accounts { get; set; }
  }
}
