using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.LetsEncryptServer.Cache.Responses {

  public class HostnameResponse {
    public string Hostname { get; set; }
    public DateTime Expires { get; set; }
    public bool IsUpcomingExpire { get; set; }
  }


  public class GetHostnamesResponse {
    public List<HostnameResponse> Hostnames { get; set; }
  }
}
