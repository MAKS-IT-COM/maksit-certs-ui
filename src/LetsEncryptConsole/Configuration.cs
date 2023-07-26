using System.Runtime.InteropServices;

namespace MaksIT.LetsEncryptConsole {
  public class Configuration {
    public LetsEncryptEnvironment[]? Environments { get; set; }
    public Customer[]? Customers { get; set; }
  }

  public class OsDependant {
    public string? Windows { get; set; }
    public string? Linux { get; set; }
  }



  public class LetsEncryptEnvironment {
    public bool Active { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }

    private string? _cache;
    public string Cache {
      get => _cache ?? "";
      set => _cache = value;
    }

    public OsDependant? ACME { get; set; }
    public OsDependant? SSL { get; set; }


    public string? GetACME() {

      if (OperatingSystem.IsWindows())
        return ACME?.Windows;

      if (OperatingSystem.IsLinux())
        return ACME?.Linux;

      return default;
    }

    public string? GetSSL() {

      if (OperatingSystem.IsWindows())
        return SSL?.Windows;

      if (OperatingSystem.IsLinux())
        return SSL?.Linux;

      return default;
    }
  }

  public class Customer {
    private string? _id;
    public string Id {
      get => _id ?? string.Empty;
      set => _id = value;
    }
    
    public bool Active { get; set; }
    public string[]? Contacts { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public Site[]? Sites { get; set; }
  }

  public class Site {
    public bool Active { get; set; }
    public string? Name { get; set; }
    public string[]? Hosts { get; set; }
    public string? Challenge { get; set; }
  }
}
