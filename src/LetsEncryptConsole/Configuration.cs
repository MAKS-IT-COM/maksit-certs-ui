using System.Runtime.InteropServices;

namespace MaksIT.LetsEncryptConsole {
  public class Configuration {
    public LetsEncryptEnvironment[]? Environments { get; set; }
    public Customer[]? Customers { get; set; }
  }

  public class OsWindows {
    public string? Path { get; set; }
  }

  public class OsLinux {
    public string? Path { get; set; }

    public string? Owner { get; set; }

    public string? ChangeMode { get; set; }

  }

  public class OsDependant {
    public OsWindows? Windows { get; set; }
    public OsLinux? Linux { get; set; }
  }

  public class SSHClientSettings {
    public bool Active { get; set; }

    public string? Host { get; set; }

    public int Port { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }
  }



  public class LetsEncryptEnvironment {
    public bool Active { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }

    public OsDependant? ACME { get; set; }
    public OsDependant? SSL { get; set; }

    public SSHClientSettings? SSH { get; set; }
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
