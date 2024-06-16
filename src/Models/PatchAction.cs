using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Models {
  public class PatchAction<T> {
    public PatchOperation Op { get; set; } // Enum for operation type
    public int? Index { get; set; } // Index for the operation (for arrays/lists)
    public T? Value { get; set; } // Value for the operation
  }
}
