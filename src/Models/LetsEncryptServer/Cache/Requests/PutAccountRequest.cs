using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.Cache.Requests {
  public class PutAccountRequest {
    public string Description { get; set; }
    public string[] Contacts { get; set; }
  }
}
