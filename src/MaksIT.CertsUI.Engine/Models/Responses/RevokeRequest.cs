using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.LetsEncrypt.Models.Responses {
  public class RevokeRequest {
    public string Certificate { get; set; } = string.Empty;
    public int Reason { get; set; }
  }
}
