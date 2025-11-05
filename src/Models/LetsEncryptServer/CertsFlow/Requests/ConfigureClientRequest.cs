using MaksIT.Core.Abstractions.Webapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models.LetsEncryptServer.CertsFlow.Requests {
  public class ConfigureClientRequest : RequestModelBase {
    public bool IsStaging { get; set; }
  }
}
