using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.LetsEncrypt.Models.Responses {
  public class Problem {
    public string Type { get; set; }

    public string Detail { get; set; }

    public string RawJson { get; set; }
  }
}
