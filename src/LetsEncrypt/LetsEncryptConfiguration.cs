using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.LetsEncrypt {


  public interface ILetsEncryptConfiguration {
    string Production { get; set; }
    string Staging { get; set; }
  }


  public class LetsEncryptConfiguration : ILetsEncryptConfiguration {
    public required string Production { get; set; }
    public required string Staging { get; set; }
  }


}
