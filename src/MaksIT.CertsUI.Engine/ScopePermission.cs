using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.CertsUI.Engine;

[Flags]
public enum ScopePermission : ushort {
  None = 0,

  Read = 1 << 0,
  Write = 1 << 1,
  Delete = 1 << 2,
  Create = 1 << 3,
}
