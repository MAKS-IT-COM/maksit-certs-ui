namespace LetsEncryptServer {

  public class Site {
    public required string Name { get; set; }
    public required string[] Hosts { get; set; }
    public required string Challenge { get; set; }
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

  public class Server { 
    public required string Address { get; set; }
    public required string PrivateKey { get; set; }
    public required string Path { get; set; }
  }

  public class Configuration {    
    public required string Production { get; set; }
    public required string Staging { get; set; }
    public required Server Server { get; set; }

    public Customer[]? Customers { get; set; }
  }
}
