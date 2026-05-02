using LinqToDB;
using LinqToDB.Data;
using MaksIT.CertsUI.Engine.Data;

namespace MaksIT.CertsUI.Engine.Infrastructure;

/// <summary>
/// Creates a Linq2Db DataConnection for the Certs database using the engine connection string and mapping schema.
/// Registered as scoped so each request gets its own connection.
/// </summary>
public interface ICertsUIDataConnectionFactory {
  DataConnection Create();
}

public class CertsUIDataConnectionFactory : ICertsUIDataConnectionFactory {
  private readonly ICertsEngineConfiguration _config;

  public CertsUIDataConnectionFactory(ICertsEngineConfiguration config) {
    _config = config;
  }

  public DataConnection Create() {
    var options = new DataOptions()
      .UseConnectionString(ProviderName.PostgreSQL, _config.ConnectionString)
      .UseMappingSchema(CertsUILinq2DbMapping.Schema);
    return new DataConnection(options);
  }
}
