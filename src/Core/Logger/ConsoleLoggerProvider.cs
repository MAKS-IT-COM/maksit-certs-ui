using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaksIT.Core.Logger;

public class MyCustomLoggerProvider : ILoggerProvider {
  public ILogger CreateLogger(string categoryName) {
    throw new NotImplementedException();
  }

  public void Dispose() {
    throw new NotImplementedException();
  }
}