using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Cache.Responses {

  public class GetHostnamesResponse {
    public List<HostnameResponse> Hostnames { get; set; }
  }
}
