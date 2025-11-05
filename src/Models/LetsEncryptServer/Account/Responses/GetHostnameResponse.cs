using MaksIT.Core.Abstractions.Webapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Account.Responses {
  public class GetHostnameResponse : ResponseModelBase {
    public required string Hostname { get; set; }
    public DateTime Expires { get; set; }
    public bool IsUpcomingExpire { get; set; }
    public bool IsDisabled { get; set; }
  }

}
