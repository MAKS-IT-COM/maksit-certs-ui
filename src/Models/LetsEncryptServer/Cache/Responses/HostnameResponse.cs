using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Cache.Responses {
  public class HostnameResponse {
    public string Hostname { get; set; }
    public DateTime Expires { get; set; }
    public bool IsUpcomingExpire { get; set; }
  }

}
